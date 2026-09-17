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
    private ToolManager tm;

    void Start()
    {
        gm = GameManager.Instance.Grid;
        tm = GameManager.Instance.Tool;
        tm.OnToolEdgePlaced += ToolEdgePlacedHandler;
    }
    void OnDestroy()
    {
        tm.OnToolEdgePlaced -= ToolEdgePlacedHandler;
    }
    private void ToolEdgePlacedHandler(
        Vector2Int startGrid,
        List<Vector2Int?> nearGrids
    )
    {
        if (tm.SelectedTool != ToolType.Cable) return;
        
        for (int i = 0; i < nearGrids.Count; i++)
        {
            try
            {
                Vector2Int? endGrid = nearGrids[i];
                if (endGrid == null) break;

                Cable cable = new(startGrid, (Vector2Int)endGrid);
                if (cables.Contains(cable))
                {
                    if (cableEdgeInstances[cable].IsInteractable())
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
    }

    private HashSet<Cable> cables = new();
    private HashSet<CableGroup> groups = new();
    private Dictionary<Cable, CableInstance> cableEdgeInstances = new();
    private Dictionary<Vector2Int, CableInstance> cableNodeInstances = new();

    private bool CanPlaceCable(GridManager gm, Vector2Int a, Vector2Int b)
    {
        if (!gm.IsValidPos(new Cable(a, b)))
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
        if (!tm.UseTool()) return false;

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
        seq.AppendCallback(
            () =>
            {
                cableNodeInstances[start].StartTweening();
                cableEdgeInstances[cable].StartTweening();
                cableNodeInstances[end].StartTweening();
            }
        );
        seq.Append(cableNodeInstances[start].GetPlaceNodeTween(
            oldStartC,
            group.GetConnection(start),
            0.3f,
            Ease.OutBack
        ));
        seq.Join(cableEdgeInstances[cable].GetPlaceEdgeTween(0.3f, Ease.OutBack));
        seq.Join(cableNodeInstances[end].GetPlaceNodeTween(
            oldEndC,
            group.GetConnection(end),
            0.3f,
            Ease.OutBack
        ));
        seq.AppendCallback(
            () =>
            {
                cableNodeInstances[start].EndTweening();
                cableEdgeInstances[cable].EndTweening();
                cableNodeInstances[end].EndTweening();
            }
        );
        
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

    public void RemoveCables()
    {
        if (cableEdgeInstances.Count != 0)
        {
            foreach (var kvp in cableEdgeInstances) 
                Destroy(kvp.Value.gameObject);
            cableEdgeInstances = new();
        }
        if (cableNodeInstances.Count != 0)
        {
            foreach (var kvp in cableNodeInstances)
                Destroy(kvp.Value.gameObject);
            cableNodeInstances = new();
        }
        cables.Clear();
        groups.Clear();
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
        tm.CancelTool(ToolType.Cable);

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
