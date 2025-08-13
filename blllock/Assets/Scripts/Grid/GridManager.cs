using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

public enum TileType {
    Empty,
    Occupied
}

public class GridManager : MonoBehaviour
{
    // Contains stage grid data indexed by ID
    // Control each grid point information for a current stage

    #region Grid Data
    public string csvFileName = "Data.csv";
    public static Dictionary<int, DataParser.RowData> GridData { get; private set; } = new Dictionary<int, DataParser.RowData>();
    private void Awake()
    {
        LoadGridData();
    }
    private void LoadGridData()
    {
        string filePath = Path.Combine(Application.dataPath, "Data", csvFileName);
        List<DataParser.RowData> rows = DataParser.ParseCSV(filePath);

        foreach (var row in rows)
        {
            if (row.TryGetInt("ID", out int id))
            {
                GridData[id] = row;
            }
        }
    }
    #endregion

    #region Grid Points
    private static GridPoint[,] GridPoint; // 2D array container for grid point info

    // Initializes gridPointInfos with (width+1)*(height+1) GridPointInfo objects
    public static void InitGridPoint(int width, int height)
    {
        GridPoint = new GridPoint[height + 1, width + 1];
        for (int x = 0; x <= height; x++)
        {
            for (int y = 0; y <= width; y++)
            {
                GridPoint[x, y] = new GridPoint(new Vector2Int(x, y), GridPointType.None);
            }
        }
    }

    // Returns the GridPointInfo at (x, y), or null if not initialized
    // Changing the type and the expr of grid point (x, y) is done by accessing the GridPoint directly
    public static GridPoint GetGridPoint(int x, int y)
    {
        Assert.IsNotNull(GridPoint, "GridPoint array is not initialized.");
        Assert.IsTrue(x >= 0 && y >= 0, "GridPoint indices must be non-negative.");
        Assert.IsTrue(x < GridPoint.GetLength(0) && y < GridPoint.GetLength(1), "GridPoint indices must be within bounds.");

        return GridPoint[x, y];
    }

    private static void BlockGridPoint(int x, int y, LogicExpr expr, Vector2Int blockGrid)
    {
        Assert.IsNotNull(GridPoint, "GridPoint array is not initialized.");
        Assert.IsTrue(x >= 0 && y >= 0, "GridPoint indices must be non-negative.");
        Assert.IsTrue(x < GridPoint.GetLength(0) && y < GridPoint.GetLength(1), "GridPoint indices must be within bounds.");

        ////
    }
    #endregion

    #region Tiles
    private static TileType[,] Tiles; // 2D array to track tile occupancy
    public static void InitTiles(int width, int height)
    {
        Tiles = new TileType[height, width];
        for (int x = 0; x < height; x++)
            for (int y = 0; y < width; y++)
                Tiles[x, y] = TileType.Empty;
    }

    public static bool IsTileEmpty(Vector2Int tilePos)
    {
        if (!IsInTileBounds(tilePos)) return false;
        return Tiles[tilePos.x, tilePos.y] == TileType.Empty;
    }

    // base position is the top-left corner of the block
    public static bool PlaceBlock(Vector2Int basePos, List<Vector2Int> tileOffsets)
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
    #endregion

    #region Position Conversion & Helpers

    // 타일 좌표 중 가장 가까운 위치 반환 (없으면 null)
    public static Vector2Int? GetNearestGridPosition(Vector3 worldPos)
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

    public static Vector3 GridToWorld(Vector2Int tilePos)
    {
        float unit = Utils.TILE_SPACING / (float)Utils.DENOMINATOR; // 1.0f 등

        int width = GridPoint.GetLength(1);  // 가로 크기
        int height = GridPoint.GetLength(0); // 세로 크기

        float offsetX = width * unit / 2f;
        float offsetY = height * unit / 2f;

        // tilePos.x 가 세로 인덱스, tilePos.y 가 가로 인덱스라고 가정
        float posX = tilePos.y * unit + unit / 2f - offsetX;
        float posY = -(tilePos.x * unit + unit / 2f) + offsetY;

        return new Vector3(posX, posY, 0);
    }

    private static bool IsInTileBounds(Vector2Int pos)
    {
        if (Tiles == null) return false;
        return pos.x >= 0 && pos.y >= 0 && pos.x < Tiles.GetLength(0) && pos.y < Tiles.GetLength(1);
    }
    #endregion
}