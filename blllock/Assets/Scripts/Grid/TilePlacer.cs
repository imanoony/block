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
        PlaceTilesForID(5); // 예시로 ID 3에 해당하는 타일을 배치
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

        Color color = Color.white;
        string colorCode = gridPoint.Type == GridPointType.Input ? Utils.RED : Utils.BLUE;
        if (gridPoint.Expr is ConstantExpr) colorCode = Utils.BLACK;

        if (!ColorUtility.TryParseHtmlString(colorCode, out color))
        {
            Debug.LogWarning($"Invalid color code: {colorCode}");
            color = Color.white;
        }

        var sr = gpObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }

        var tmp = gpObj.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp != null)
        {
            tmp.text = gridPoint.Expr.ToString();
            tmp.color = color;

            // 중앙 위치 (world 좌표)
            float centerX = origin.x + (width - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
            float centerY = origin.y - (height - 1) * Utils.TILE_SPACING / (2f * Utils.DENOMINATOR);
            Vector3 centerPos = new Vector3(centerX, centerY, 0);

            // 방향 벡터 (중앙 → 원)
            Vector3 dir = (finalPos - centerPos).normalized;

            // 가장자리 여부 판단
            bool isEdge = pos.x == 0 || pos.x == height || pos.y == 0 || pos.y == width;
            if (isEdge)
            {
                dir = Vector3.zero;
                if (pos.x == 0) dir += Vector3.up;
                if (pos.x == height) dir += Vector3.down;
                if (pos.y == 0) dir += Vector3.left;
                if (pos.y == width) dir += Vector3.right;
                dir = dir.normalized;
            }

            // 텍스트 길이(폭)만큼 spacing 보정
            tmp.ForceMeshUpdate();
            float widthAdjust = tmp.preferredWidth * 0.5f * tmp.transform.lossyScale.x;
            float spacing = Utils.GRID_TEXT_SPACING / (float)Utils.DENOMINATOR;
            Vector3 textPos = finalPos + dir * (spacing + widthAdjust);

            // 텍스트 로컬 위치 = 텍스트 world 위치 - circle world 위치
            tmp.transform.localPosition = textPos - finalPos;
        }
    }
}
