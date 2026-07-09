using System.Collections.Generic;
using UnityEngine;

public class CablePlacer : MonoBehaviour
{
    private Grid startGrid = null;
    private bool isDragging = false;
    void Update()
    {
        if (!GameManager.Instance.CableActivated) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new(mouseWorld.x, mouseWorld.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.TryGetComponent<GridInstance>(out var gi))
                {
                    startGrid = gi.GridData;
                    isDragging = true;
                }
            }
        }

        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                
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

    private HashSet<Cable> cables;
    private HashSet<CableGroup> groups;

    private bool CanPlaceCable(GridManager gm, Vector2Int a, Vector2Int b)
    {
        if (!gm.IsValidPos(new(a, b)))
        {
            // 필요하다면 여기에 디버그 로그 출력
            return false;
        }
        return true;
    }
    public bool PlaceCable(GridManager gm, Vector2Int a, Vector2Int b)
    {
        if (!IsAdjacent(a, b)) return false;
        if (!CanPlaceCable(gm, a, b)) return false;

        // Cable 처리
        Cable cable = new(a, b);
        cables.Add(cable);
        gm.PlaceCable(cable);

        // CableGroup 처리
        CableGroup ga = FindGroup(a);
        CableGroup gb = FindGroup(b);
        if (ga == null && gb == null)
        {
            CableGroup group = new();
            group.Add(cable);
            groups.Add(group);
            PlaceCableGroup(gm, group);
        }
        else if (ga != null && gb == null) 
        {
            RemoveCableGroup(gm, ga);
            ga.Add(cable);
            PlaceCableGroup(gm, ga);
        }
        else if (ga == null && gb != null) 
        {
            RemoveCableGroup(gm, gb);
            gb.Add(cable);
            PlaceCableGroup(gm, gb);
        }
        else if (ga != gb)
        {
            RemoveCableGroup(gm, ga);
            RemoveCableGroup(gm, gb);
            CableGroup merged = CableGroup.Merge(ga, gb, cable);
            groups.Remove(ga);
            groups.Remove(gb);
            groups.Add(merged);
            PlaceCableGroup(gm, merged);
        }
        else 
        {
            RemoveCableGroup(gm, ga);
            ga.Add(cable);
            PlaceCableGroup(gm, ga);
        }

        // TODO: 그래픽 처리 및 연출

        return true;
    }

    public void PlaceCableGroup(GridManager gm, CableGroup group)
    {
        if (!gm.PlaceCableGroup(group))
        {
            group.SetValid(false);
            gm.AddInvalid(group);
            // TODO: 여러가지 연출 처리 (색상 등)
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

        groups.Remove(group);
        groups.UnionWith(split);

        RemoveCableGroup(gm, group);
        for (int i = 0; i < split.Count; i++) PlaceCableGroup(gm, split[i]);
        
        // TODO: 그래픽 처리 및 연출
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
}
