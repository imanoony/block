#nullable enable
using UnityEngine;

public enum GridPointType
{
    Input,
    Output,
    None // intermediate or undefined
}

public class GridPoint
{
    public Vector2Int Pos { get; private set; }
    public GridPointType Type { get; set; }
    public LogicExpr? Expr { get; set; }

    public GridPoint(Vector2Int position, GridPointType type, LogicExpr? expr = null)
    {
        Pos = position;
        Type = type;
        Expr = expr;
    }
}