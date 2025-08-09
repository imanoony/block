using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Assertions;

public class TilePlacer : MonoBehaviour
{

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject gridPrefab;


    void Start()
    {
        PlaceTilesForID(4); // 예시로 ID 3에 해당하는 타일을 배치
    }

    public bool PlaceTilesForID(int id)
    {
        if (!GridManager.GridData.ContainsKey(id))
        {
            Debug.LogWarning($"ID {id}에 해당하는 데이터가 없습니다.");
            return false;
        }

        var row = GridManager.GridData[id];

        if (!row.TryGetInt("Width", out int width) || !row.TryGetInt("Height", out int height))
        {
            Debug.LogError($"ID {id}의 Width/Height 파싱 실패");
            return false;
        }

        // Grid point initialization and setup
        GridManager.InitGridPoint(width, height);
        GridPoint gridPoint;

        foreach (var (pos, expr) in row.Inputs)
        {
            gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
            gridPoint.Type = GridPointType.Input;
            gridPoint.Expr = expr;
        }
        foreach (var (pos, expr) in row.Outputs)
        {
            gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
            gridPoint.Type = GridPointType.Output;
            gridPoint.Expr = expr;
        }

        float originX = - (width - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
        float originY = (height - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
        Vector3 origin = new Vector3(originX, originY, 0);

        for (int x = 0; x < height; x++)  // 세로 방향
        {
            for (int y = 0; y < width; y++)  // 가로 방향
            {
                float posX = y * Utils.TILE_SPACING / (float)Utils.DENOMINATOR;
                float posY = -x * Utils.TILE_SPACING / (float)Utils.DENOMINATOR;

                Vector3 tilePos = origin + new Vector3(posX, posY, 0);

                GameObject tile = Instantiate(tilePrefab, tilePos, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";
            }
        }


        // check if the grid points are set correctly
        foreach (var (pos, _) in row.Inputs)
        {
            gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
            Debug.Log($"Input Tile at ({pos.x}, {pos.y}): Type = {gridPoint.Type}, Expr = {gridPoint.Expr}");
            PlaceGrid(width, height, pos);
        }
        foreach (var (pos, _) in row.Outputs)
        {
            gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
            Debug.Log($"Output Tile at ({pos.x}, {pos.y}): Type = {gridPoint.Type}, Expr = {gridPoint.Expr}");
            PlaceGrid(width, height, pos);
        }

        return true;
    }

    public void PlaceGrid(int width, int height, Vector2Int pos)
    {
        var gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
        Assert.IsNotNull(gridPoint, $"GridPoint at {pos} is null");
        Assert.IsTrue(
            gridPoint.Type == GridPointType.Input || gridPoint.Type == GridPointType.Output,
            $"GridPoint at {pos} must be Input or Output, but was {gridPoint.Type}"
        );

        float originX = - (width - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
        float originY = (height - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
        Vector3 origin = new Vector3(originX, originY, 0);

        float posX = (pos.y - 0.5f) * Utils.TILE_SPACING / (float)Utils.DENOMINATOR;
        float posY = -(pos.x - 0.5f) * Utils.TILE_SPACING / (float)Utils.DENOMINATOR;

        Vector3 finalPos = origin + new Vector3(posX, posY, 0);

        GameObject gpObj = Instantiate(gridPrefab, finalPos, Quaternion.identity, transform);
        gpObj.name = $"GridPoint_{pos.x}_{pos.y}";

        var sr = gpObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = gridPoint.Type == GridPointType.Input ? Color.red : Color.blue;
        }

        var tmp = gpObj.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = gridPoint.Expr.ToString();
            tmp.color = gridPoint.Type == GridPointType.Input ? Color.red : Color.blue;
        }
    }
}
