using System.Collections.Generic;
using UnityEngine;

public class CablePlacer
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
    public bool PlaceCable(Vector2Int a, Vector2Int b)
    {
        // TODO: 두 grid가 인접한지 확인
        // TODO: 케이블 엣지 위치에 별다른 방해물이 없는지 확인

        Cable cable = new(a, b);
        cables.Add(cable);

        CableGroup ga = FindGroup(a);
        CableGroup gb = FindGroup(b);
        if (ga == null && gb == null)
        {
            CableGroup group = new();
            group.Add(cable);
            groups.Add(group);
        }
        else if (ga != null && gb == null) ga.Add(cable);
        else if (ga == null && gb != null) gb.Add(cable);
        else if (ga != gb)
        {
            CableGroup merged = CableGroup.Merge(ga, gb, cable);
            groups.Remove(ga);
            groups.Remove(gb);
            groups.Add(merged);
        }
        else ga.Add(cable);

        return false;
    }
    
    public void RemoveCable(Vector2Int a, Vector2Int b)
    {
        Cable cable = new(a, b);
        RemoveCable(cable);
    }

    public void RemoveCable(Cable cable)
    {
        cables.Remove(cable);

        CableGroup group = FindGroup(cable);
        List<CableGroup> splited = CableGroup.Split(group, cable);

        groups.Remove(group);
        groups.UnionWith(splited);
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
}
