using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

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
    #endregion
}