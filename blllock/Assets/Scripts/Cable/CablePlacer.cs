using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CablePlacer : MonoBehaviour
{
    [SerializeField] private GameObject cablePrefab;
    [SerializeField] private GameObject cableParent; // 얘는 나중에 처리
    [SerializeField] private Sprite[] cableNodeSprites;
    [SerializeField] private Sprite cableEdgeSprite;


    private GridManager gm;
    private Vector2Int startGrid = new(-1, -1);
    private bool isDragging = false;

    void Start()
    {
        gm = GameManager.Instance.Grid;
    }
    void Update()
    {
        if (!GameManager.Instance.CableActivated) return;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("CablePlace Start");
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new(mouseWorld.x, mouseWorld.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.TryGetComponent<GridInstance>(out var gi))
                {
                    startGrid = gi.GridData.Pos;
                    isDragging = true;
                }
            }
        }

        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                Debug.Log("CablePlace End");
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                List<Vector2Int?> nearGrids = gm.GetNearestGrids(mouseWorld, 4);

                for (int i = 0; i < nearGrids.Count; i++)
                {
                    try
                    {
                        Vector2Int? endGrid = nearGrids[i];
                        if (endGrid == null) break;

                        Cable cable = new(startGrid, (Vector2Int)endGrid);
                        if (cables.Contains(cable))
                        {
                            RemoveCable(gm, cable);
                        }
                        else if (PlaceCable(gm, startGrid, (Vector2Int)endGrid))
                        {
                            break;
                        }
                        else continue;
                    }
                    catch (ArgumentException)
                    {
                        Debug.Log("Invalid Cable");
                        continue;
                    }
                }
                isDragging = false;
                startGrid = new(-1, -1);
            }
        }
        // if 마우스 다운 감지:
        //   입력에 대응하는 그리드가 있는지 확인, 없으면 종료
        //   시작 지점 설정 (입력에 대응하는 그리드)
        //   드래그 중 true로 설정

        // if 마우스 업 감지:
        //   종료 지점 계산 및 그 정합성 log 출력
        //   종료 지점이 정합하다면 cable 설치
        //   드래그 중 false로 설정

        // if 드래그 중:
        //   현재 향하는 방향 계산 (구조 상 목적지는 자동으로 산출됨)
        //   현재 향하는 방향 및 그 정합성 log 출력
        //   만약 마우스가 다른 그리드 위에 있다면 
        //   그 그리드와의 연결의 정합성 log 출력
        //   만약 연결이 정합하다면 cable 설치 후 시작 지점 재설정
    }

    private HashSet<Cable> cables = new();
    private HashSet<CableGroup> groups = new();
    private Dictionary<Cable, CableInstance> cableEdgeInstances = new();
    private Dictionary<Vector2Int, CableInstance> cableNodeInstances = new();

    private bool CanPlaceCable(GridManager gm, Vector2Int a, Vector2Int b)
    {
        if (!gm.IsValidPos(new(a, b)))
        {
            // 필요하다면 여기에 디버그 로그 출력
            return false;
        }
        return true;
    }
    public bool PlaceCable(GridManager gm, Vector2Int start, Vector2Int end)
    {
        if (!IsAdjacent(start, end)) return false;
        if (!CanPlaceCable(gm, start, end)) return false;

        // Cable 처리
        Cable cable = new(start, end);
        cables.Add(cable);
        gm.PlaceCable(cable);

        // CableGroup 처리
        CableGroup gstart = FindGroup(start);
        CableGroup gend = FindGroup(end);
        CableConnection oldStartC = gstart == null ? CableConnection.None : gstart.GetConnection(start);
        CableConnection oldEndC = gend == null ? CableConnection.None : gend.GetConnection(end);
        CableGroup group;
        if (gstart == null && gend == null)
        {
            group = new();
            group.Add(cable);
            groups.Add(group);
        }
        else if (gstart != null && gend == null) 
        {
            RemoveCableGroup(gm, gstart);
            gstart.Add(cable);
            group = gstart;
        }
        else if (gstart == null && gend != null) 
        {
            RemoveCableGroup(gm, gend);
            gend.Add(cable);
            group = gend;
        }
        else if (gstart != gend)
        {
            RemoveCableGroup(gm, gstart);
            RemoveCableGroup(gm, gend);
            CableGroup merged = CableGroup.Merge(gstart, gend, cable);
            groups.Remove(gstart);
            groups.Remove(gend);
            groups.Add(merged);
            group = merged;
        }
        else 
        {
            RemoveCableGroup(gm, gstart);
            gstart.Add(cable);
            group = gstart;
        }
        group.SetValid(true);
        PlaceCableGroup(gm, group);

        GameObject edge = Instantiate(cablePrefab, cableParent.transform);
        CableInstance edgeCI = edge.GetComponent<CableInstance>();
        edgeCI.Initialize(
            CablePart.Edge,
            cable,
            cableEdgeSprite
        );
        cableEdgeInstances[cable] = edgeCI;

        for (int i = 0; i < cable.Nodes.Count; i++)
        {
            Vector2Int node = cable.Nodes[i];
            if (cableNodeInstances.TryGetValue(node, out CableInstance nodeCI))
            {
                nodeCI.Initialize(
                    CablePart.Node,
                    node,
                    Connection2Sprite(group.GetConnection(node)),
                    Connection2Rotate(group.GetConnection(node))
                );
            }
            else
            {
                GameObject nodeGo = Instantiate(cablePrefab, cableParent.transform);
                nodeCI = nodeGo.GetComponent<CableInstance>();
                nodeCI.Initialize(
                    CablePart.Node,
                    node,
                    Connection2Sprite(group.GetConnection(node)),
                    Connection2Rotate(group.GetConnection(node))
                );
                cableNodeInstances[node] = nodeCI;
            }
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(cableNodeInstances[start].GetPlaceNodeTween(
            oldStartC,
            group.GetConnection(start),
            false,
            0.05f,
            Ease.InQuad
        ));
        seq.Append(cableEdgeInstances[cable].GetPlaceEdgeTween(start, end, 0.1f, Ease.InQuad));
        seq.Append(cableNodeInstances[end].GetPlaceNodeTween(
            oldEndC,
            group.GetConnection(end),
            true,
            0.05f,
            Ease.OutQuad
        ));

        seq.Play();

        // TODO: 위에서 인스턴스 생성하며 애니메이션 출력 queue에
        // 출력할 순서대로 cable instances를 넣고,
        // 이를 tween의 sequence 기능으로 하나씩 출력함.

        // anim이 나오고 있는 동안에는 해당 cable과 상호작용이 불가능하며, 
        // 이 불가능은 전체 그룹 (노드, 엣지, 노드 3개) 이 완료될 때까지 유지된다.
        // 즉, cable과의 상호작용의 본질인 엣지와의 상호작용은,
        // 해당 edge와 결합된 두 노드의 anim이 모두 끝날 때까지 불가능하다.

        // cable anim의 구현은 cable instance에서 진행한다.
        // 이 케이블을 없앨 수 있는지 없는지 (지리적으로) 반환하는 함수도 cable instance에 위치한다. 

        return true;
    }

    public void PlaceCableGroup(GridManager gm, CableGroup group)
    {
        if (!gm.PlaceCableGroup(group))
        {
            group.SetValid(false);
            gm.AddInvalid(group);
            // TODO: 여러가지 연출 처리 (색상 등)
            Debug.Log("Invalid Cable Group");
        }
    }
    
    public void RemoveCable(GridManager gm, Vector2Int a, Vector2Int b)
    {
        Cable cable = new(a, b);
        RemoveCable(gm, cable);
    }

    public void RemoveCable(GridManager gm, Cable cable)
    {
        // Cable 처리
        cables.Remove(cable);
        gm.RemoveCable(cable);

        // CableGroup 처리
        CableGroup group = FindGroup(cable);
        List<CableGroup> split = CableGroup.Split(group, cable);

        Debug.Log($"splited count: {split.Count}");

        groups.Remove(group);
        groups.UnionWith(split);

        RemoveCableGroup(gm, group);
        for (int i = 0; i < split.Count; i++) PlaceCableGroup(gm, split[i]);
        
        Destroy(cableEdgeInstances[cable].gameObject);
        cableEdgeInstances.Remove(cable);

        for (int i = 0; i < cable.Nodes.Count; i++)
        {
            Vector2Int node = cable.Nodes[i];
            CableInstance nodeCI = cableNodeInstances[node];

            // 지워야 하는 애면 지우고, 아니면 모양만 바꾸기.
            CableGroup nodeGroup = FindGroup(node);
            if (nodeGroup == null)
            {
                Destroy(cableNodeInstances[node].gameObject);
                cableNodeInstances.Remove(node);
            }
            else
            {
                nodeCI.Initialize(
                    CablePart.Node,
                    node,
                    Connection2Sprite(nodeGroup.GetConnection(node)),
                    Connection2Rotate(nodeGroup.GetConnection(node))
                );
            }
        }
    }
    public void RemoveCableGroup(GridManager gm, CableGroup group)
    {
        gm.RemoveCableGroup(group, group.Valid);
        gm.RemoveInvalid(group);
    }

    public bool Check(GridManager gm, CableGroup group)
    {
        if (!gm.PlaceCableGroup(group)) return false;

        group.SetValid(true);
        // TODO: 여러가지 연출 처리 (색상 등)

        return true;
    }

    private CableGroup FindGroup(Vector2Int a)
    {
        foreach (CableGroup g in groups)
        {
            if (g.Contains(a)) return g;
        }

        return null;
    }
    private CableGroup FindGroup(Cable cable)
    {
        foreach (CableGroup g in groups)
        {
            if (g.Contains(cable)) return g;
        }

        return null;
    }
    private bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    // 일단은 하드하게
    private Rotate Connection2Rotate(CableConnection connection)
    {
        return (int)connection switch
        {
            0b0000 => throw new Exception("Invalid cable connection."),
            0b1000 or 0b1100 or 0b0110 or 0b1110 or 0b1111
                => Rotate.None,
            0b0010 or 0b0011 or 0b0101 or 0b0111
                => Rotate.Rotate90,
            0b0100 or 0b1001 or 0b1101
                => Rotate.Rotate180,
            0b0001 or 0b1010 or 0b1011
                => Rotate.Rotate270,
            _ => Rotate.Null
        };
    }
    // 얘도 하드하기
    private Sprite Connection2Sprite(CableConnection connection)
    {
        return (int)connection switch
        {
            0b0000 => throw new Exception("Invalid cable connection."),
            0b1000 or 0b0100 or 0b0010 or 0b0001 => cableNodeSprites[0],
            0b1100 or 0b0011 => cableNodeSprites[1],
            0b1010 or 0b1001 or 0b0110 or 0b0101 => cableNodeSprites[2],
            0b1110 or 0b1101 or 0b1011 or 0b0111 => cableNodeSprites[3],
            _ => cableNodeSprites[4]
        };
    }
}
