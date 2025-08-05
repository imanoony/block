using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TilePlacer : MonoBehaviour
{
    public string csvFileName = "Data.csv"; // Resources 기준
    public GameObject tilePrefab;

    private Dictionary<int, DataParser.RowData> gridDataById;

    private void Awake()
    {
        LoadGridData();
    }

    void Start()
    {
        PlaceTilesForID(3); // 예시로 ID 3에 해당하는 타일을 배치
    }

    private void LoadGridData()
    {
        string filePath = Path.Combine(Application.dataPath, "Data", csvFileName);
        List<DataParser.RowData> rows = DataParser.ParseCSV(filePath);

        gridDataById = new Dictionary<int, DataParser.RowData>();

        foreach (var row in rows)
        {
            if (row.TryGetInt("ID", out int id))
            {
                gridDataById[id] = row;
            }
        }
    }

    public bool PlaceTilesForID(int id)
    {
        if (!gridDataById.ContainsKey(id))
        {
            Debug.LogWarning($"ID {id}에 해당하는 데이터가 없습니다.");
            return false;
        }

        var row = gridDataById[id];

        if (!row.TryGetInt("Width", out int width) || !row.TryGetInt("Height", out int height))
        {
            Debug.LogError($"ID {id}의 Width/Height 파싱 실패");
            return false;
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


        return true;
    }
}
