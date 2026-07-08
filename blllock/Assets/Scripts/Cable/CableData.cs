using System;
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

    public override bool Equals(object obj)
    {
        if (obj is not Cable other)
            return false;

        return Start == other.Start &&
               End == other.End;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, End);
    }
}

public class CableGroup
{
    public HashSet<Cable> Cables { get; }
    private Dictionary<Vector2Int, HashSet<Cable>> connections;
    
    public HashSet<Vector2Int> Ends { get; private set; }
    public PortVar Port { get; private set; }

    public CableGroup()
    {
        Cables = new();
        connections = new();
        Ends = new();
    
        Wire[] wires = new Wire[4];
        for (int i = 0; i < wires.Length; i++)
        {
            wires[i] = new Wire(GameManager.Instance.Wire.GenerateID());
            GameManager.Instance.Wire.AddWire(wires[i]);
        }
        Port = new PortVar
        (
            "",
            wires[0],
            wires[1],
            wires[2],
            wires[3]
        );
    }

    public bool Contains(Cable cable) => Cables.Contains(cable);
    public bool Contains(Vector2Int grid) => connections.ContainsKey(grid);

    public int Degree(Vector2Int grid)
    {
        if (connections.TryGetValue(grid, out HashSet<Cable> cables))
            return cables.Count;
        return 0;
    }

    // TODO: merge의 정합성 검사.
    public void Add(Cable cable)
    {
        Cables.Add(cable);

        connections.TryAdd(cable.Start, new HashSet<Cable>());
        connections.TryAdd(cable.End, new HashSet<Cable>());
        connections[cable.Start].Add(cable);
        connections[cable.End].Add(cable);

        if (connections[cable.Start].Count == 1) Ends.Add(cable.Start);
        else Ends.Remove(cable.Start);
        if (connections[cable.End].Count == 1) Ends.Add(cable.End);
        else Ends.Remove(cable.End);
    }

    // cable을 추가함으로써 merge함.
    public static CableGroup Merge(
        CableGroup g1,
        CableGroup g2,
        Cable cable
    )
    {
        // TODO: merge의 정합성 검사.
        // 만약 부적절한 merge라면 exception을 던지도록 함.
        // 일단 지금은 적절한 param만 들어온다고 가정함.

        CableGroup merged = new();
        foreach (Cable c in g1.Cables) merged.Add(c);
        foreach (Cable c in g2.Cables) merged.Add(c);
        merged.Add(cable);

        return merged;
    }

    // cable을 제거함으로써 split함.
    public static List<CableGroup> Split(
        CableGroup g,
        Cable cable
    )
    {
        // TODO: split의 정합성 검사.
        // 만약 부적절한 split이라면 exception을 던지도록 함.
        // 일단 지금은 적절한 param만 들어온다고 가정함.

        HashSet<Vector2Int> visited = new();
        List<CableGroup> split = new();
        
        Stack<Vector2Int> stack = new();
        stack.Push(cable.Start);
        stack.Push(cable.End);

        while (stack.Count > 0)
        {
            Vector2Int curr = stack.Pop();
            visited.Add(curr);

            if (g.connections.TryGetValue(curr, out HashSet<Cable> cables))
            {
                foreach (Cable c in cables)
                {
                    if (c.Equals(cable)) continue;

                    bool found = false;
                    for (int i = 0; i < split.Count; i++)
                    {
                        if (split[i].Contains(c))
                        {
                            found = true;
                            break;
                        }
                        else if (
                            split[i].Contains(c.Start) ||
                            split[i].Contains(c.End)
                        )
                        {
                            split[i].Add(c);
                            if (!visited.Contains(c.Start)) stack.Push(c.Start);
                            if (!visited.Contains(c.End)) stack.Push(c.End);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        CableGroup newGroup = new();
                        newGroup.Add(c);
                        split.Add(newGroup);
                        if (!visited.Contains(c.Start)) stack.Push(c.Start);
                        if (!visited.Contains(c.End)) stack.Push(c.End);
                        break;
                    }
                }
            }
        }
        return split;
    }
}   