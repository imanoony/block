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
    public List<LogicExpr> 

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

        int portIndex = block.GridToIndex(blockGrid);
        Debug.Log($"portIndex: {portIndex}, blockGrid: {blockGrid}");
        LogicExpr port = block.IndexToPort(portIndex);

        // 타일 격자에 논리식이 존재하지 않는 경우
        if (Expr == null && block.CanRegisterPort(port, Expr))
        {
            Debug.Log($"Registering port {port} at {Pos} for block {block.ID}");
            Expr = port; // 블록의 논리식을 등록한다
            Synchronize(); // 다른 블록과 동기화한다
        }

        // 타일 격자에 이미 논리식이 존재하는 경우
        else if (Expr != null && Type != GridPointType.Output)
        {
            if (!port.Equals(Expr))
            {
                Debug.Log($"port: {port}, Expr: {Expr}");
                if (block.AddPortMapping(portIndex, Expr)) return;
                if (block.CanRegisterPort(port, Expr))
                {
                    Debug.Log($"Registering port {port} at {Pos} for block {block.ID}");
                    Expr = port; // 블록의 논리식을 등록한다
                    Synchronize(); // 다른 블록과 동기화한다
                }
                else
                {
                    Debug.LogError("INVALID BLOCK POSITION");
                }
            }
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
    public void Synchronize()
    {
        if (Expr == null) return;
        for (int i = 0; i < Blocks.Count; i++)
        {
            int portIndex = Blocks[i].GridToIndex(BlockGrids[i]);
            if (!Blocks[i].AddPortMapping(portIndex, Expr))
            {
                Debug.LogWarning($"Failed to synchronize Expr {Expr} to Block {Blocks[i].ID} at Grid {BlockGrids[i]}");
                return;
            }
            //Blocks[i].Instance.portPositioner.UpdateText(Blocks[i]); // Update port text
        }
    }

}