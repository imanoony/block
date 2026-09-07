using System;
using System.Collections.Generic;
using UnityEngine;

public class Resistor
{
    // Resistor의 Node
    // A와 B는 자동으로 A < B로 정렬됨.
    public Vector2Int A { get; }
    public Vector2Int B { get; }

    // Nodes의 개수는 언제나 2개임.
    public List<Vector2Int> Nodes { get; }

    public Resistor(Vector2Int a, Vector2Int b)
    {
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
        {
            throw new ArgumentException(
                "Resistor endpoints must be adjacent."
            );
        }
        
    }
}