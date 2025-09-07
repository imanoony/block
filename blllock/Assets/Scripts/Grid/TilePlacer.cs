using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;


// Tilemap-based
public class TilePlacer : MonoBehaviour
{

    [SerializeField] private Tilemap tilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase tileCenter;
    [SerializeField] private TileBase tileLeft;
    [SerializeField] private TileBase tileRight;
    [SerializeField] private TileBase tileTop;
    [SerializeField] private TileBase tileTopLeft;
    [SerializeField] private TileBase tileTopRight;
    [SerializeField] private TileBase tileBottom;
    [SerializeField] private TileBase tileBottomLeft;
    [SerializeField] private TileBase tileBottomRight;

    #region Tile Placement
    public void RemoveTiles()
    {
        tilemap.ClearAllTiles();
        width = -1; height = -1;
    }
    private int width = -1, height = -1;
    public void PlaceTiles(int width, int height)
    {
        if (tilemap == null || tileCenter == null) { Utils.PrintError("Invalid tilemap or tiles"); return; }

        this.width = width;
        this.height = height;
        for (int x = 0; x < height; x++)
            for (int y = 0; y < width; y++)
                tilemap.SetTile(TileToCell(x, y), tileCenter);

        PlaceBoundaries();
        PlaceCamera();
        PlaceCameraBoundary();
        PlaceTileCollider();
        PlaceGrids();
    }
    private void PlaceBoundaries()
    {
        if (tilemap == null || tileCenter == null) { Utils.PrintError("Invalid tilemap or tiles"); return; }
        for (int x = 0; x < height; x++)
        {
            tilemap.SetTile(TileToCell(x, -1), tileLeft);
            tilemap.SetTile(TileToCell(x, width), tileRight);
        }
        for (int y = 0; y < width; y++)
        {
            tilemap.SetTile(TileToCell(-1, y), tileTop);
            tilemap.SetTile(TileToCell(height, y), tileBottom);
        }
        tilemap.SetTile(TileToCell(-1, -1), tileTopLeft);
        tilemap.SetTile(TileToCell(-1, width), tileTopRight);
        tilemap.SetTile(TileToCell(height, -1), tileBottomLeft);
        tilemap.SetTile(TileToCell(height, width), tileBottomRight);
    }
    private void PlaceTileCollider()
    {
        Vector2 size = new Vector2(tilemap.cellSize.x * (width + 2), tilemap.cellSize.y * (height + 2));
        Vector2 pos = (Vector2)GetTileTopLeftWorld(0, 0);
        pos = new Vector2(pos.x - tilemap.cellSize.x, pos.y + tilemap.cellSize.y);
        pos = new Vector2(pos.x + size.x / 2f, pos.y - size.y / 2f);
        size = new Vector2(size.x, Camera.main.orthographicSize * 3);

        GameObject wall = new GameObject("TileWall");
        wall.transform.position = pos;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static; // 움직이지 않는 벽
    }

    [Header("Camera")]
    // Base Position, Offset, Scale 모두 포함해서 카메라 세팅
    // 카메라 세팅 이후 바운더리 수정
    [SerializeField] private PixelPerfectCamera PPCamera;
    public void PlaceCamera()
    {
        if (tilemap == null) { Utils.PrintError("Tilemap not set"); return; }

        Camera cam = Camera.main;
        if (cam == null) { Utils.PrintError("Main Camera not found"); return; }

        int cellX = width / 2, cellY = height / 2;
        Vector3 centerWorld = tilemap.GetCellCenterWorld(new(cellX, cellY));

        // height, width가 짝수라면 offset 추가한다
        if (height % 2 == 0) centerWorld -= new Vector3(0, tilemap.cellSize.y / 2f, 0);
        if (width % 2 == 0) centerWorld -= new Vector3(tilemap.cellSize.x / 2f, 0, 0);

        // 타일맵 성질에 따른 offset 추가한다
        centerWorld += new Vector3(tilemap.cellSize.x / 8f, 0f, 0f);

        cam.transform.position = new Vector3(centerWorld.x, centerWorld.y, cam.transform.position.z);

        // 타일맵 크기 (월드 단위)
        Vector3 cellSize = tilemap.cellSize;
        float worldWidth = width * cellSize.x;
        float worldHeight = height * cellSize.y;

        // 화면 비율에 맞게 카메라 크기 계산
        float aspect = Screen.width / (float)Screen.height;

        // 세로 기준 크기
        float cameraHalfHeight = worldHeight / Utils.TILE_FILL_PERCENT / 2f;

        // 가로 기준 크기 (aspect 보정)
        float cameraHalfWidth = worldWidth / Utils.TILE_FILL_PERCENT / 2f;

        // 가로/세로 중 더 큰 값을 사용해야 타일맵 전체가 화면 안에 들어옴
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(cameraHalfHeight, cameraHalfWidth / aspect);

        PPCamera.refResolutionY = Mathf.RoundToInt(cam.orthographicSize * 2f * Utils.PPU);
        PPCamera.refResolutionX = Mathf.RoundToInt(PPCamera.refResolutionY * aspect);

        // 카메라 월드 좌표 기준 최소/최대 좌표
        Vector3 camPos = cam.transform.position;
        float camHalfH = cam.orthographicSize;
        float camHalfW = cam.orthographicSize * aspect;

        Vector2 min = new Vector2(camPos.x - camHalfW, camPos.y - camHalfH);
        Vector2 max = new Vector2(camPos.x + camHalfW, camPos.y + camHalfH);
        Rect boundary = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

        // Utils에 바운더리 세팅
        Utils.SetBoundary(boundary);
    }
    private void PlaceCameraBoundary()
    {
        Camera cam = Camera.main;
        float camHalfH = cam.orthographicSize;
        float camHalfW = camHalfH * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float thickness = 1f; // 벽 두께

        // 상단 벽
        CreateWall(new Vector2(camPos.x, camPos.y + camHalfH + thickness / 2), new Vector2(camHalfW * 2, thickness));
        // 하단 벽
        CreateWall(new Vector2(camPos.x, camPos.y - camHalfH - thickness / 2), new Vector2(camHalfW * 2, thickness));
        // 왼쪽 벽
        CreateWall(new Vector2(camPos.x - camHalfW - thickness / 2, camPos.y), new Vector2(thickness, camHalfH * 2));
        // 오른쪽 벽
        CreateWall(new Vector2(camPos.x + camHalfW + thickness / 2, camPos.y), new Vector2(thickness, camHalfH * 2));
    }

    private void CreateWall(Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject("BoundaryWall");
        wall.transform.position = pos;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static; // 움직이지 않는 벽
    }


    [Header("Grid")]
    [SerializeField] private GameObject gridParent;
    [SerializeField] private GameObject gridPrefab;
    public void PlaceGrids()
    {
        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                GameObject grid = Instantiate(gridPrefab, gridParent.transform);
                grid.name = $"Grid_{x}_{y}";
                grid.transform.position = (Vector3)GetTileCenterWorld(x, y);
                grid.GetComponent<GridInstance>().Initialize(x, y);
            }
        }
    }
    #endregion

    #region Tile Position
    private Vector3Int TileToCell(int x, int y) => new(y, height - 1 - x, 0);
    public Vector3 GetTileSize() => tilemap.cellSize;
    public Vector3? GetTileCenterWorld(int x, int y)
    {
        if (x < 0 || x >= height || y < 0 || y >= width) return null;
        return tilemap.GetCellCenterWorld(TileToCell(x, y));
    }

    public Vector3? GetTileTopLeftWorld(int x, int y)
    {
        if (x < 0 || x >= height || y < 0 || y >= width) return null;
        Vector3 center = tilemap.GetCellCenterWorld(TileToCell(x, y));
        return center + new Vector3(-tilemap.cellSize.x / 2f, tilemap.cellSize.y / 2f, 0);
    }

    public Vector3 GetBlockCenterOnTile(int x, int y, int height, int width)
    {
        Vector3 topLeft = (Vector3)GetTileTopLeftWorld(x, y);
        topLeft += new Vector3(tilemap.cellSize.x * width / 2f, -tilemap.cellSize.y * height / 2f, 0);
        return topLeft += new Vector3(tilemap.cellSize.x / 8f, -tilemap.cellSize.y / 8f, 0);
    }
    #endregion
}
