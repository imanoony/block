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
    public LogicExpr? Expr { get; set; }
    public List<BlockData> Blocks { get; private set; }
    public List<Vector2Int> BlockGrids { get; private set; }

    public GridPoint(Vector2Int position, GridPointType type, LogicExpr? expr = null)
    {
        Pos = position;
        Type = type;
        Expr = expr;

        Blocks = new List<BlockData>(4); // Initialize with a capacity of 4;
        BlockGrids = new List<Vector2Int>(4); // Initialize with a capacity of 4;
    }
    public void AddBlockData(BlockData block, Vector2Int blockGrid)
    {
        if (!Blocks.Contains(block))
        {
            Blocks.Add(block);
            BlockGrids.Add(blockGrid);
        }
        else
        {
            // 이미 존재하면 갱신한다
            int index = Blocks.IndexOf(block);
            BlockGrids[index] = blockGrid;
        }

        // 타일 격자에 논리식이 존재하지 않는 경우
        int portIndex = block.GridToIndex(blockGrid);
        if (Expr == null)
        {
            Expr = block.IndexToPort(portIndex); // 블록의 논리식을 등록한다
            Synchronize(); // 다른 블록과 동기화한다
        }

        // 타일 격자에 이미 논리식이 존재하는 경우
        else if (Expr != null && Type != GridPointType.Output)
        {
            block.AddPortMapping(portIndex, Expr); // 블록에 논리식을 등록한다
        }
    }
    public void SubBlockData(BlockData block)
    {
        int index = Blocks.IndexOf(block);
        if (index >= 0)
        {
            Blocks.RemoveAt(index);
            BlockGrids.RemoveAt(index);
        }
    }
    private void Synchronize()
    {
        for (int i = 0; i < Blocks.Count; i++)
        {
            int portIndex = Blocks[i].GridToIndex(BlockGrids[i]);
            Blocks[i].AddPortMapping(portIndex, Expr!);
        }
    }
}