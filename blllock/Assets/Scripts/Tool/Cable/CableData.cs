using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum CableConnection
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3
}

public class Cable
{
    // Cable의 Node
    // Start와 End는 자동으로 Start < End로 정렬됨.
    public Vector2Int A { get; }
    public Vector2Int B { get; }

    // Nodes의 개수는 언제나 2개임.
    public List<Vector2Int> Nodes { get; }

    // TBD: Cable의 양 끝이 어떤 타입인지를
    // 이 Cable 데이터에 저장할 것인지 결정 필요

    public Cable(Vector2Int a, Vector2Int b)
    {
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
            throw new ArgumentException("Cable endpoints must be adjacent.");

        if (a.x < b.x || (a.x == b.x && a.y < b.y))
        {
            A = a;
            B = b;
        }
        else
        {
            A = b;
            B = a;
        }

        Nodes = new() { A, B }; 
    }

    public override bool Equals(object obj)
    {
        if (obj is not Cable other)
            return false;

        return A == other.A &&
               B == other.B;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B);
    }

    public Edge ToEdge()
    {
        if (A.x == B.x) // Horizontal
        {
            return new HEdge(new(A.x, A.y), EdgeType.Cable);   
        }
        else // Vertical 
        {
            return new VEdge(new(A.x, A.y), EdgeType.Cable);
        }
    }

    public bool IsHorizontal() => A.x == B.x;
}

public class CableGroup
{
    public HashSet<Cable> Cables { get; }
    private Dictionary<Vector2Int, HashSet<Cable>> connections;
    public CableConnection GetConnection(Vector2Int node)
    {
        CableConnection c = CableConnection.None;
        if (connections.TryGetValue(node, out HashSet<Cable> cables))
        {
            foreach (Cable cable in cables)
            {
                if (cable.IsHorizontal())
                {
                    if (cable.A == node) c |= CableConnection.Right;
                    else c |= CableConnection.Left;
                }
                else
                {
                    if (cable.A == node) c |= CableConnection.Down;
                    else c |= CableConnection.Up;
                }
            }
            return c;
        }
        else return c;
    }
    
    public HashSet<Vector2Int> Ends { get; private set; }
    public PortVar Port { get; private set; }
    public List<int> WireIds { get; private set; } = new();
    public bool Valid { get; private set; } = true;
    public void SetValid(bool valid) => Valid = valid;

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
            WireIds.Add(wires[i].ID);
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

        connections.TryAdd(cable.A, new HashSet<Cable>());
        connections.TryAdd(cable.B, new HashSet<Cable>());
        connections[cable.A].Add(cable);
        connections[cable.B].Add(cable);

        if (connections[cable.A].Count == 1) Ends.Add(cable.A);
        else Ends.Remove(cable.A);
        if (connections[cable.B].Count == 1) Ends.Add(cable.B);
        else Ends.Remove(cable.B);
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
        stack.Push(cable.A);
        stack.Push(cable.B);

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
                            split[i].Contains(c.A) ||
                            split[i].Contains(c.B)
                        )
                        {
                            split[i].Add(c);
                            if (!visited.Contains(c.A)) stack.Push(c.A);
                            if (!visited.Contains(c.B)) stack.Push(c.B);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        CableGroup newGroup = new();
                        newGroup.Add(c);
                        split.Add(newGroup);
                        if (!visited.Contains(c.A)) stack.Push(c.A);
                        if (!visited.Contains(c.B)) stack.Push(c.B);
                    }
                }
            }
        }
        return split;
    }
}   