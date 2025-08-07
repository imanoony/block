using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TilePlacer : MonoBehaviour
{
    
    public GameObject tilePrefab;


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

        float xOffset = (width - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
        float yOffset = (height - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float posX = x * Utils.TILE_SPACING / (float)Utils.DENOMINATOR - xOffset;
                float posY = y * Utils.TILE_SPACING / (float)Utils.DENOMINATOR - yOffset;

                Vector3 position = new Vector3(posX, posY, 0);

                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, gameObject.transform);
                tile.name = $"Tile_{x}_{y}";
            }
        }


        // check if the grid points are set correctly
        foreach (var (pos, _) in row.Inputs)
        {
            gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
            Debug.Log($"Input Tile at ({pos.x}, {pos.y}): Type = {gridPoint.Type}, Expr = {gridPoint.Expr}");
        }
        foreach (var (pos, _) in row.Outputs)
        {
            gridPoint = GridManager.GetGridPoint(pos.x, pos.y);
            Debug.Log($"Output Tile at ({pos.x}, {pos.y}): Type = {gridPoint.Type}, Expr = {gridPoint.Expr}");
        }

        return true;
    }
}
