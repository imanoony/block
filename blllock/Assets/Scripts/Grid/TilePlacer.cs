using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;


// Tilemap-based
public class TilePlacer : MonoBehaviour
{   
    [Header("Background")]
    [SerializeField] private Tilemap bgTilemap;
    [SerializeField] private List<TileBase> bgTiles;
    private int bgWidth = -1, bgHeight = -1;
    private int bgOffset = 4;
    public void PlaceBackground(int width, int height)
    {
        Debug.Log($"Placing background with width {width} and height {height}");
        bgTilemap.ClearAllTiles();
        List<TileBase> evenTiles = new();
        List<TileBase> oddTiles = new();
        bgWidth = width;
        bgHeight = height;

        for (int i = 0; i < bgTiles.Count; i++)
        {
            if (i % 2 == 0)
            {
                evenTiles.Add(bgTiles[i]);
            }    
            else
            {
                oddTiles.Add(bgTiles[i]);
            }
        }

        for (int y = -bgOffset; y < height + bgOffset; y++)
        {
            for (int x = -bgOffset; x < width + bgOffset; x++)
            {
                bool useEven = (x + y) % 2 == 0;
                List<TileBase> pool = useEven ? evenTiles : oddTiles;

                if (pool.Count == 0)
                {
                    continue;
                }

                TileBase tile = pool[Random.Range(0, pool.Count)];
                Vector3Int pos = new(x, y, 0);
                bgTilemap.SetTile(pos, tile);
            }
        }
        Debug.Log(bgTiles.Count);
    }
    public void RemoveBackground()
    {
        bgTilemap.ClearAllTiles();
        bgWidth = -1; bgHeight = -1;
    }

    [Header("Tiles")]
    
    [SerializeField] private Tilemap tileTilemap;
    [SerializeField] private TileBase tileCenter;
    [SerializeField] private TileBase tileLeft;
    [SerializeField] private TileBase tileRight;
    [SerializeField] private TileBase tileTop;
    [SerializeField] private TileBase tileTopLeft;
    [SerializeField] private TileBase tileTopRight;
    [SerializeField] private TileBase tileBottom;
    [SerializeField] private TileBase tileBottomLeft;
    [SerializeField] private TileBase tileBottomRight;
    private int tileWidth = -1, tileHeight = -1;
    private int tileStartX = -1, tileStartY = -1;
    private Tilemap tileShadow = null;

    #region Tile Placement
    public void RemoveTiles()
    {
        tileTilemap.ClearAllTiles();
        tileWidth = -1; tileHeight = -1;
        tileStartX = -1; tileStartY = -1;

        RemoveTileCollider();
        RemoveCameraBoundary();
        RemoveGrids();
    }
    public void PlaceTiles(
        int startX,
        int startY,
        int width,
        int height
    )
    {
        if (tileTilemap == null || tileCenter == null) { Utils.PrintError("Invalid tilemap or tiles"); return; }

        this.tileStartX = startX;
        this.tileStartY = startY;   
        this.tileWidth = width;
        this.tileHeight = height;

        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                tileTilemap.SetTile(TileToCell(x, y), tileCenter);
            }
        }
            

        PlaceBoundaries();
        PlaceCamera();
        //PlaceCameraBoundary();
        //PlaceTileCollider();
        PlaceGrids();
    }
    private void PlaceBoundaries()
    {
        if (tileTilemap == null || tileCenter == null) { Utils.PrintError("Invalid tilemap or tiles"); return; }
        if (tileShadow == null)
        {
            tileShadow = tileTilemap.gameObject.transform.GetChild(0).GetComponent<Tilemap>();
        }

        for (int x = 0; x < tileHeight; x++)
        {
            tileTilemap.SetTile(TileToCell(x, -1), tileLeft);
            tileTilemap.SetTile(TileToCell(x, tileWidth), tileRight);
            tileShadow.SetTile(TileToCell(x, -1), tileLeft);
        }
        for (int y = 0; y < tileWidth; y++)
        {
            tileTilemap.SetTile(TileToCell(-1, y), tileTop);
            tileTilemap.SetTile(TileToCell(tileHeight, y), tileBottom);
            tileShadow.SetTile(TileToCell(tileHeight, y), tileBottom);
        }
        tileTilemap.SetTile(TileToCell(-1, -1), tileTopLeft);
        tileTilemap.SetTile(TileToCell(-1, tileWidth), tileTopRight);
        tileTilemap.SetTile(TileToCell(tileHeight, -1), tileBottomLeft);
        tileTilemap.SetTile(TileToCell(tileHeight, tileWidth), tileBottomRight);
        tileShadow.SetTile(TileToCell(-1, -1), tileTopLeft);
        tileShadow.SetTile(TileToCell(tileHeight, -1), tileBottomLeft);
        tileShadow.SetTile(TileToCell(tileHeight, tileWidth), tileBottomRight);
    }

    private GameObject tileWall = null;
    private void RemoveTileCollider()
    {
        if (tileWall != null)
        {
            Destroy(tileWall);
            tileWall = null;
        }
    }
    private void PlaceTileCollider()
    {
        Vector2 size = new Vector2(tileTilemap.cellSize.x * (tileWidth + 2), tileTilemap.cellSize.y * (tileHeight + 2));
        Vector2 pos = (Vector2)GetTileTopLeftWorld(0, 0);
        pos = new Vector2(pos.x - tileTilemap.cellSize.x, pos.y + tileTilemap.cellSize.y);
        pos = new Vector2(pos.x + size.x / 2f, pos.y - size.y / 2f);
        size = new Vector2(size.x, Camera.main.orthographicSize * 5);

        tileWall = new GameObject("TileWall");
        tileWall.transform.position = pos;

        var collider = tileWall.AddComponent<BoxCollider2D>();
        collider.size = size;

        var rb = tileWall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static; // 움직이지 않는 벽
    }

    [Header("Camera")]
    // Base Position, Offset, Scale 모두 포함해서 카메라 세팅
    // 카메라 세팅 이후 바운더리 수정
    [SerializeField] private PixelPerfectCamera PPCamera;
    public void PlaceCamera()
    {
        if (bgTilemap == null) { Utils.PrintError("Tilemap not set"); return; }

        Camera cam = Camera.main;
        if (cam == null) { Utils.PrintError("Main Camera not found"); return; }

        Vector3 centerWorld = bgTilemap.GetCellCenterWorld(
            new Vector3Int(
                bgWidth / 2,
                bgHeight / 2,
                0
            )
        );

        // height, width가 짝수라면 offset 추가한다
        if (bgHeight % 2 == 0) centerWorld -= new Vector3(0, bgTilemap.cellSize.y / 2f, 0);
        if (bgWidth % 2 == 0) centerWorld -= new Vector3(bgTilemap.cellSize.x / 2f, 0, 0);
        cam.transform.position = new Vector3(centerWorld.x, centerWorld.y, cam.transform.position.z);

        // 타일맵 크기 (월드 단위)
        Vector3 cellSize = bgTilemap.cellSize;
        float worldWidth = bgWidth * cellSize.x;
        float worldHeight = bgHeight * cellSize.y;

        // 화면 비율에 맞게 카메라 크기 계산
        float percent = 0.95f;
        float aspect = Screen.width / (float)Screen.height;
        float cameraHalfHeight = worldHeight / percent / 2f;
        float cameraHalfWidth = worldWidth / percent / 2f;

        // 가로/세로 중 더 큰 값을 사용해야 타일맵 전체가 화면 안에 들어옴
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(cameraHalfHeight, cameraHalfWidth / aspect);

        //PPCamera.refResolutionY = Mathf.RoundToInt(cam.orthographicSize * 2f * Utils.PPU);
        //PPCamera.refResolutionX = Mathf.RoundToInt(PPCamera.refResolutionY * aspect);

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

    private List<GameObject> boundaryWalls = null;
    private void RemoveCameraBoundary()
    {
        if (boundaryWalls != null)
        {
            foreach (var wall in boundaryWalls)
                if (wall != null) Destroy(wall);
        }
        boundaryWalls = null;
    }
    private void PlaceCameraBoundary()
    {
        boundaryWalls = new List<GameObject>();
        Camera cam = Camera.main;
        float camHalfH = cam.orthographicSize;
        float camHalfW = camHalfH * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float thickness = 5f; // 벽 두께

        // 상단 벽
        boundaryWalls.Add(CreateWall(new Vector2(camPos.x, camPos.y + camHalfH + thickness / 2), new Vector2(camHalfW * 2.5f, thickness)));
        // 하단 벽
        boundaryWalls.Add(CreateWall(new Vector2(camPos.x, camPos.y - camHalfH - thickness / 2), new Vector2(camHalfW * 2.5f, thickness)));
        // 왼쪽 벽
        boundaryWalls.Add(CreateWall(new Vector2(camPos.x - camHalfW - thickness / 2, camPos.y), new Vector2(thickness, camHalfH * 2.5f)));
        // 오른쪽 벽
        boundaryWalls.Add(CreateWall(new Vector2(camPos.x + camHalfW + thickness / 2, camPos.y), new Vector2(thickness, camHalfH * 2.5f)));
    }
    private GameObject CreateWall(Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject("BoundaryWall");
        wall.transform.position = pos;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static; // 움직이지 않는 벽

        return wall;
    }


    [Header("Grid")]
    [SerializeField] private GameObject gridParent;
    [SerializeField] private GameObject gridPrefab;
    private List<GameObject> grids = null;
    public void RemoveGrids()
    {
        if (grids != null)
        {
            foreach (var grid in grids)
            {
                Destroy(grid.GetComponent<GridInstance>());
                Destroy(grid);
            }
        }
        grids = null;
    }
    public void PlaceGrids()
    {
        grids = new List<GameObject>();
        for (int x = 0; x < tileHeight + 1; x++)
        {
            for (int y = 0; y < tileWidth + 1; y++)
            {
                GameObject grid = Instantiate(gridPrefab, gridParent.transform);
                grid.name = $"Grid_{x}_{y}";
                Vector3 center = (Vector3)GetTileCenterWorld(x, y);
                grid.transform.position = new(center.x, center.y, grid.transform.position.z);
                grid.GetComponent<GridInstance>().Initialize(x, y);

                grids.Add(grid);
            }
        }
    }
    #endregion

    #region Tile Position
    private Vector3Int TileToCell(int x, int y)
    {
        return new(
            tileStartY + y,
            tileStartX + (tileHeight - 1 - x),
            0
        );
    }
    public Vector3 GetTileSize() => tileTilemap.cellSize;
    public Vector3? GetTileCenterWorld(int x, int y)
    {
        // Grid 고려 height + 1, width + 1까지는 타일로 처리함
        if (x < 0 || x >= tileHeight + 1 || y < 0 || y >= tileWidth + 1) return null;
        return tileTilemap.GetCellCenterWorld(TileToCell(x, y));
    }

    public Vector3? GetTileTopLeftWorld(int x, int y)
    {
        // Grid 고려 height + 1, width + 1까지는 타일로 처리함
        if (x < 0 || x >= tileHeight + 1 || y < 0 || y >= tileWidth + 1) return null;
        Vector3 center = tileTilemap.GetCellCenterWorld(TileToCell(x, y));
        return center + new Vector3(-tileTilemap.cellSize.x / 2f, tileTilemap.cellSize.y / 2f, 0);
    }

    public Vector3 GetBlockCenterOnTile(int x, int y, int height, int width)
    {
        Vector3 topLeft = (Vector3)GetTileTopLeftWorld(x, y);
        topLeft += new Vector3(tileTilemap.cellSize.x * width / 2f, -tileTilemap.cellSize.y * height / 2f, 0);
        return topLeft;
    }
    #endregion

    #region Barrier Placement
    [Header("Barriers")]
    [SerializeField] private Tilemap HBarrierTilemap;
    [SerializeField] private Tilemap VBarrierTilemap;
    [SerializeField] private TileBase HB;
    [SerializeField] private TileBase VBstart;
    [SerializeField] private TileBase VBmiddle;
    [SerializeField] private TileBase VBend;
    public void PlaceHBarriers(HashSet<Vector2Int> HBarriers)
    {
        foreach (Vector2Int pos in HBarriers)
        {
            HBarrierTilemap.SetTile(TileToCell(pos.x, pos.y), HB);
        }
    }
    public void PlaceVBarriers(HashSet<Vector2Int> VBarriers)
    {
        foreach (Vector2Int pos in VBarriers)
        {
            VBarrierTilemap.SetTile(TileToCell(pos.x, pos.y), VBstart);
            if (VBarriers.Contains(new(pos.x - 1, pos.y)))
                VBarrierTilemap.SetTile(TileToCell(pos.x, pos.y), VBmiddle);
            if (!VBarriers.Contains(new(pos.x + 1, pos.y)))
                VBarrierTilemap.SetTile(TileToCell(pos.x + 1, pos.y), VBend);
        }
    }
    #endregion
}
