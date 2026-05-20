using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;


// Tilemap-based
public class TilePlacer : MonoBehaviour
{   
    #region Background Placement
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
    #endregion

    #region Circuit Placement
    [Header("Circuit")]
    
    [SerializeField] private Tilemap circuitTilemap;
    [SerializeField] private TileBase circuitCenter;
    [SerializeField] private TileBase circuitLeft;
    [SerializeField] private TileBase circuitRight;
    [SerializeField] private TileBase circuitTop;
    [SerializeField] private TileBase circuitTopLeft;
    [SerializeField] private TileBase circuitTopRight;
    [SerializeField] private TileBase circuitBotton;
    [SerializeField] private TileBase circuitBottomLeft;
    [SerializeField] private TileBase circuitBottomRight;
    private int circuitWidth = -1, circuitHeight = -1;
    private int circuitStartX = -1, circuitStartY = -1;
    private Tilemap circuitShadow = null;
    public void RemoveCircuit()
    {
        circuitTilemap.ClearAllTiles();
        circuitWidth = -1; circuitHeight = -1;
        circuitStartX = -1; circuitStartY = -1;

        //RemoveTileCollider();
        //RemoveCameraBoundary();
        RemoveGrids();
    }
    public void PlaceCircuit(
        int startX,
        int startY,
        int width,
        int height
    )
    {
        if (circuitTilemap == null || circuitCenter == null) 
        { 
            Utils.PrintError("Invalid tilemap or tiles"); 
            return; 
        }
        
        this.circuitStartX = startX;
        this.circuitStartY = startY;
        this.circuitWidth = width;
        this.circuitHeight = height;

        for (int x = circuitStartX; x < circuitStartX + circuitHeight; x++)
        {
            for (int y = circuitStartY; y < circuitStartY + circuitWidth; y++)
            {
                circuitTilemap.SetTile(TileToCell(x, y), circuitCenter);
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
        if (circuitTilemap == null || circuitCenter == null) 
        { 
            Utils.PrintError("Invalid tilemap or tiles"); 
            return; 
        }
        if (circuitShadow == null)
        {
            circuitShadow = circuitTilemap.gameObject.transform.GetChild(0).GetComponent<Tilemap>();
        }

        for (int x = circuitStartX; x < circuitStartX + circuitHeight; x++)
        {
            circuitTilemap.SetTile(TileToCell(x, circuitStartY - 1), circuitLeft);
            circuitTilemap.SetTile(TileToCell(x, circuitStartY + circuitWidth), circuitRight);
            circuitShadow.SetTile(TileToCell(x, circuitStartY - 1), circuitLeft);
        }
        for (int y = circuitStartY; y < circuitStartY + circuitWidth; y++)
        {
            circuitTilemap.SetTile(TileToCell(circuitStartX - 1, y), circuitTop);
            circuitTilemap.SetTile(TileToCell(circuitStartX + circuitHeight, y), circuitBotton);
            circuitShadow.SetTile(TileToCell(circuitStartX + circuitHeight, y), circuitBotton);
        }
        circuitTilemap.SetTile(TileToCell(circuitStartX - 1, circuitStartY - 1), circuitTopLeft);
        circuitTilemap.SetTile(TileToCell(circuitStartX - 1, circuitStartY + circuitWidth), circuitTopRight);
        circuitTilemap.SetTile(TileToCell(circuitStartX + circuitHeight, circuitStartY - 1), circuitBottomLeft);
        circuitTilemap.SetTile(TileToCell(circuitStartX + circuitHeight, circuitStartY + circuitWidth), circuitBottomRight);
        circuitShadow.SetTile(TileToCell(circuitStartX - 1, circuitStartY - 1), circuitTopLeft);
        circuitShadow.SetTile(TileToCell(circuitStartX + circuitHeight, circuitStartY - 1), circuitBottomLeft);
        circuitShadow.SetTile(TileToCell(circuitStartX + circuitHeight, circuitStartY + circuitWidth), circuitBottomRight);
    }
    #endregion

    #region Camera Placement
    // Base Position, Offset, Scale 모두 포함해서 카메라 세팅
    // 카메라 세팅 이후 바운더리 수정
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
        float percent = 1f;
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
    #endregion

    #region Grid Placement
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
        for (int x = 0; x < circuitHeight + 1; x++)
        {
            for (int y = 0; y < circuitWidth + 1; y++)
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
    // Tile은 Background Tile과 Circuit Tile로 분류됨.
    // Background Tile과 Circuit Tile은 서로 다른 타일맵에 배치되지만,
    // 두 타일맵의 타일 크기와 좌표는 일치해야 함.
    private Vector3Int TileToCell(int x, int y) => new(y, bgHeight - 1 - x, 0);
    public Vector3 GetTileSize() => bgTilemap.cellSize;
    public Vector3? GetTileCenterWorld(int x, int y)
    {
        if (x < 0 || x >= bgHeight || y < 0 || y >= bgWidth) return null;
        return bgTilemap.GetCellCenterWorld(TileToCell(x, y));
    }

    public Vector3? GetTileTopLeftWorld(int x, int y)
    {
        if (x < 0 || x >= bgHeight || y < 0 || y >= bgWidth) return null;
        Vector3 center = bgTilemap.GetCellCenterWorld(TileToCell(x, y));
        return center + new Vector3(-GetTileSize().x / 2f, GetTileSize().y / 2f, 0);
    }

    public Vector3 GetBlockCenterOnTile(int x, int y, int height, int width)
    {
        Vector3 topLeft = (Vector3)GetTileTopLeftWorld(x, y);
        topLeft += new Vector3(GetTileSize().x * width / 2f, -GetTileSize().y * height / 2f, 0);
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
