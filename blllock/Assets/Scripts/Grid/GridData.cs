#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType { Empty, Occupied }
public enum GridType { Null, Input, Output }
public enum EdgeType { Empty, Barrier, Cable }

public class Tile
{
    public Vector2Int Pos { get; private set; }
    public TileType Type { get; private set; }
    public bool IsCircuit { get; private set; } = false;
    public Tile(Vector2Int pos, TileType type = TileType.Empty) { Pos = pos; Type = type; }
    public void SetType(TileType type) => Type = type;
    public void SetIsCircuit(bool isCircuit) => IsCircuit = isCircuit;
}

public class Grid
{
    public Vector2Int Pos { get; private set; }
    public GridType Type { get; private set; }
    public LogicExpr? Expr { get; private set; } = null; // input, output과 관련된 상수 LogicExpr

    public List<PortExpr> Ports { get; private set; } = new(); // 인접한 Ports들 (최대 4개)    
    public List<Wire?> WiresLeftUp { get; private set; } = new();
    public List<Wire?> WiresLeftDown { get; private set; } = new();
    public List<Wire?> WiresRightUp { get; private set; } = new();
    public List<Wire?> WiresRightDown { get; private set; } = new();
    public event Action? OnPortsChanged;

    public Grid(Vector2Int pos, GridType type = GridType.Null) { Pos = pos; Type = type; }
    public void SetType(GridType type) => Type = type;
    public void SetExpr(LogicExpr? expr) => Expr = expr;
    public bool AddPort(PortExpr port)
    {
        if (Ports.Count >= Utils.MAX_PORT) return false;

        Ports.Add(port);
        WiresLeftUp.Add(port.LeftUp);
        WiresLeftDown.Add(port.LeftDown);
        WiresRightUp.Add(port.RightUp);
        WiresRightDown.Add(port.RightDown);

        OnPortsChanged?.Invoke();
        return true;
    }
    public void RemovePort(PortExpr port)
    {
        int index = Ports.IndexOf(port);
        if (index < 0) return;

        Ports.RemoveAt(index);

        WiresLeftUp.RemoveAt(index);
        WiresLeftDown.RemoveAt(index);
        WiresRightUp.RemoveAt(index);
        WiresRightDown.RemoveAt(index);

        OnPortsChanged?.Invoke();
    }
}

public abstract class Edge
{
    public Vector2Int Pos { get; protected set; }
    public EdgeType Type { get; protected set; }
    public void SetType(EdgeType type) => Type = type;
}

public class HEdge : Edge
{
    public HEdge(
        Vector2Int pos,
        EdgeType type = EdgeType.Empty
    )
    {
        Pos = pos;
        Type = type;
    }
}

public class VEdge : Edge
{
    public VEdge(
        Vector2Int pos,
        EdgeType type = EdgeType.Empty
    )
    {
        Pos = pos;
        Type = type;
    }
}