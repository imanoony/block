#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public enum TileType { Empty, Occupied }
public enum GridType { Null, Input, Output }

public class Tile
{
    public Vector2Int Pos { get; private set; }
    public TileType Type { get; private set; }
    public Tile(Vector2Int pos, TileType type = TileType.Empty) { Pos = pos; Type = type; }
    public void SetType(TileType type) => Type = type;
}

public class Grid
{
    public Vector2Int Pos { get; private set; }
    public GridType Type { get; private set; }
    public LogicExpr? Expr { get; private set; } = null;
    public Grid(Vector2Int pos, GridType type = GridType.Null) { Pos = pos; Type = type; }
    public void SetType(GridType type) => Type = type;
    public void SetExpr(LogicExpr? expr) => Expr = expr;
}

public class GridManager : MonoBehaviour
{
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;
        // TODO
        initialized = true;
    }

    public Grid[,]? Grids { get; private set; } = null;
    public Tile[,]? Tiles { get; private set; } = null;
    private TilePlacer? _tilePlacer;
    public TilePlacer TilePlacer
    {
        get
        {
            if (_tilePlacer == null)
            {
                _tilePlacer = GetComponent<TilePlacer>();
                if (_tilePlacer == null)
                    _tilePlacer = gameObject.AddComponent<TilePlacer>();
            }
            return _tilePlacer;
        }
    }

    public void InitStage(StageData stage)
    {
        int width = stage.Width, height = stage.Height;

        Grids = new Grid[height + 1, width + 1];
        for (int x = 0; x < height + 1; x++)
            for (int y = 0; y < width + 1; y++)
                Grids[x, y] = new Grid(new(x, y));

        foreach (var (pos, expr) in stage.Inputs)
        {
            Grids[pos.x, pos.y].SetType(GridType.Input);
            Grids[pos.x, pos.y].SetExpr(expr);
        }

        foreach (var (pos, expr) in stage.Outputs)
        {
            Grids[pos.x, pos.y].SetType(GridType.Output);
            Grids[pos.x, pos.y].SetExpr(expr);
        }

        Tiles = new Tile[height, width];
        for (int x = 0; x < height; x++)
            for (int y = 0; y < width; y++)
                Tiles[x, y] = new Tile(new(x, y));

        // 타일 배치하는 GUI 로직
        TilePlacer.PlaceTiles(width, height); // TODO
    }

    private bool IsTileEmpty(int x, int y)
    {
        if (Tiles == null) return false;
        if (!IsInTileBounds(x, y)) return false;
        return Tiles[x, y].Type == TileType.Empty;
    }
    private bool IsInTileBounds(int x, int y)
    {
        if (Tiles == null) return false;
        return x >= 0 && y >= 0 && x < Tiles.GetLength(0) && y < Tiles.GetLength(1);
    }

    #region Block Placement
    // Base Tile은 블록이 놓이는 타일 중 우측 상단의 타일
    public bool CanPlaceBlock(BlockData block, Vector2Int baseTile)
    {
        // TODO
        return false;
    }

    public bool TryPlaceBlock(BlockData block, Vector2Int baseTile)
    {
        // TODO
        return false;
    }

    private bool IsValidPos(BlockData block, Vector2Int baseTile)
    {
        // TODO
        return false;
    }

    private bool IsValidPort(BlockData block, Vector2Int baseTile)
    {
        // TODO
        return false;
    }
    #endregion

    /*
    // base position is the top-left corner of the block
    public bool PlaceBlock(BlockData blockData, Vector2Int basePos, List<Vector2Int> tileOffsets)
    {
        TileType[,] backup = (TileType[,])Tiles.Clone(); // Backup current tile state

        foreach (var offset in tileOffsets)
        {
            Vector2Int pos = basePos + offset;
            if (IsInTileBounds(pos) && IsTileEmpty(pos))
                Tiles[pos.x, pos.y] = TileType.Occupied;
            else
            {
                Debug.LogWarning($"PlaceBlock: tilePos {pos} out of bounds");
                Tiles = backup; // Restore from backup if any tile is invalid
                return false;
            }
        }
        return true;
    }

    #region Position Conversion & Helpers

    // 타일 좌표 중 가장 가까운 위치 반환 (없으면 null)
    public Vector2Int? GetNearestGridPosition(Vector3 worldPos)
    {
        Debug.Log($"GetNearestTilePosition called with worldPos: ({worldPos.x}, {worldPos.y})");

        if (GridPoint == null) return null;

        float minDist = float.MaxValue;
        Vector2Int? nearest = null;

        int height = GridPoint.GetLength(0);
        int width = GridPoint.GetLength(1);

        Vector3 gridWorldPos = Vector3.zero;
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                gridWorldPos = GridToWorld(new Vector2Int(x, y));
                float dist = Vector3.Distance(worldPos, gridWorldPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = new Vector2Int(x, y);
                }
            }
        }

        Debug.Log($"min distance: {minDist}, nearest grid: {nearest}, gridWorldPos: ({gridWorldPos})");
        return minDist <= Utils.THRESHOLD ? nearest : null;
    }

    public Vector3 GridToWorld(int x, int y)
    {
        
    }

    #endregion*/
}