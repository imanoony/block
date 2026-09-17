using System;
using System.Collections.Generic;
using UnityEngine;

public class Resistor
{
    // Resistor의 Node
    // A와 B는 자동으로 A < B로 정렬됨.
    public Vector2Int A { get; }
    public Vector2Int B { get; }
    public PortVar PortA { get; private set; }
    public PortVar PortB { get; private set; }

    // Nodes의 개수는 언제나 2개임.
    public List<Vector2Int> Nodes { get; }
    public List<int> WireIds { get; private set; } = new();
    public bool Valid { get; private set; } = true;
    public void SetValid(bool valid) => Valid = valid;

    public Resistor(Vector2Int a, Vector2Int b)
    {
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
        {
            throw new ArgumentException(
                "Resistor endpoints must be adjacent."
            );
        }
        
        (A, B) = Utils.SortPositions(a, b);

        Nodes = new() { A, B };

        Wire[] wires = new Wire[4];
        Wire[] reverseWires = new Wire[4];
        for (int i = 0; i < wires.Length; i++)
        {
            wires[i] = new Wire(GameManager.Instance.Wire.GenerateID());
            reverseWires[i] = new Wire(-wires[i].ID);

            GameManager.Instance.Wire.AddWire(wires[i]);
            GameManager.Instance.Wire.AddWire(reverseWires[i]);

            WireIds.Add(wires[i].ID);
            WireIds.Add(reverseWires[i].ID);
        }
        PortA = new PortVar
        (
            "",
            wires[0],
            wires[1],
            wires[2],
            wires[3]
        );
        PortB = new PortVar
        (
            "",
            reverseWires[0],
            reverseWires[1],
            reverseWires[2],
            reverseWires[3]
        );
    }


    public Edge ToEdge()
    {
        if (A.x == B.x) // Horizontal
        {
            return new HEdge(new(A.x, A.y), EdgeType.Resistor);
        }
        else // Vertical
        {
            return new VEdge(new(A.x, A.y), EdgeType.Resistor);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is not Resistor other) 
            return false;

        return A == other.A &&
               B == other.B;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, B);
    }
}