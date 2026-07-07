using System.Collections.Generic;
using UnityEngine;

public class Cable
{
    // Cable의 Node
    // Start와 End는 자동으로 Start < End로 정렬됨.
    public Vector2Int Start { get; }
    public Vector2Int End { get; }

    // TBD: Cable의 양 끝이 어떤 타입인지를
    // 이 Cable 데이터에 저장할 것인지 결정 필요

    public Cable(Vector2Int a, Vector2Int b)
    {
        if (a.x < b.x || (a.x == b.x && a.y < b.y))
        {
            Start = a;
            End = b;
        }
        else
        {
            Start = b;
            End = a;
        }
    }


}

public class CableGroup
{
    public HashSet<Cable> Cables { get; }
    private Dictionary<Vector2Int, HashSet<Cable>> connections;
    
    public HashSet<Vector2Int> Ends { get; private set; }
    public PortVar Port { get; private set; }

    // TODO
    public void Add(Cable cable)
    {
        // TODO
        // Cables에 cable 추가
    }

    // cable을 추가함으로써 merge함.
    public static CableGroup Merge(
        CableGroup g1,
        CableGroup g2,
        Cable cable
    )
    {
        // TODO

        return null;
    }

    // cable을 제거함으로써 split함.
    public static List<CableGroup> Split(
        CableGroup g,
        Cable cable
    )
    {
        // TODO

        return null;
    }
}   