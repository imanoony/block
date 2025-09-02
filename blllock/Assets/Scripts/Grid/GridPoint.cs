#nullable enable
using System.Collections.Generic;
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

    public GridPoint(Vector2Int position, GridPointType type)
    {
        Pos = position;
        Type = type;
    }

}