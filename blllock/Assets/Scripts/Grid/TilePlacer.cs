using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;
using System.Collections;

// Tilemap-based
public class TilePlacer : MonoBehaviour
{   
    #region Background Placement
    [Header("Background")]
    [SerializeField] private Tilemap bgTilemap;
    [SerializeField] private List<TileBase> bgTiles;

    private int bgWidth = -1, bgHeight = -1;
    private int bgOffset = 4;
    private int placedBgMinX, placedBgMaxX;
    private int placedBgMinY, placedBgMaxY;
    private bool hasBackground = false;
    public void PlaceBackground(int width, int height)
    {
        Debug.Log($"Placing background with width {width} and height {height}");

        bgWidth = width;
        bgHeight = height;

        List<TileBase> evenTiles = new();
        List<TileBase> oddTiles = new();

        for (int i = 0; i < bgTiles.Count; i++)
        {
            if (i % 2 == 0) evenTiles.Add(bgTiles[i]);
            else oddTiles.Add(bgTiles[i]);
        }

        int minX = -bgOffset;
        int maxX = width + bgOffset - 1;

        int minY = -bgOffset;
        int maxY = height + bgOffset - 1;

        // 첫 생성
        if (!hasBackground)
        {
            bgTilemap.ClearAllTiles();

            FillBackgroundRect(
                minX, maxX,
                minY, maxY,
                evenTiles, oddTiles
            );

            placedBgMinX = minX;
            placedBgMaxX = maxX;
            placedBgMinY = minY;
            placedBgMaxY = maxY;

            hasBackground = true;
            return;
        }

        // Left Expand
        if (minX < placedBgMinX)
        {
            FillBackgroundRect(
                minX, placedBgMinX - 1,
                minY, maxY,
                evenTiles, oddTiles
            );

            placedBgMinX = minX;
        }

        // Right Expand
        if (maxX > placedBgMaxX)
        {
            FillBackgroundRect(
                placedBgMaxX + 1, maxX,
                minY, maxY,
                evenTiles, oddTiles
            );

            placedBgMaxX = maxX;
        }

        // Top Expand
        if (minY < placedBgMinY)
        {
            FillBackgroundRect(
                placedBgMinX, placedBgMaxX,
                minY, placedBgMinY - 1,
                evenTiles, oddTiles
            );

            placedBgMinY = minY;
        }

        // Bottom Expand
        if (maxY > placedBgMaxY)
        {
            FillBackgroundRect(
                placedBgMinX, placedBgMaxX,
                placedBgMaxY + 1, maxY,
                evenTiles, oddTiles
            );

            placedBgMaxY = maxY;
        }
    }

    private void FillBackgroundRect(
        int minX,
        int maxX,
        int minY,
        int maxY,
        List<TileBase> evenTiles,
        List<TileBase> oddTiles
    )
    {
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                bool useEven = (x + y) % 2 == 0;
                List<TileBase> pool = useEven ? evenTiles : oddTiles;

                if (pool.Count == 0) continue;

                TileBase tile = pool[Random.Range(0, pool.Count)];
                Vector3Int pos = new(x, y, 0);

                bgTilemap.SetTile(pos, tile);
            }
        }
    }

    public void RemoveBackground()
    {
        bgTilemap.ClearAllTiles();

        bgWidth = -1;
        bgHeight = -1;

        hasBackground = false;
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
        if (circuitShadow != null) circuitShadow.ClearAllTiles();
        circuitWidth = -1; circuitHeight = -1;
        circuitStartX = -1; circuitStartY = -1;

        //RemoveTileCollider();
        //RemoveCameraBoundary();
        RemoveGrids();
        RemoveBarriers();
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

        circuitTilemap.gameObject.transform.position = Vector3.zero;
        
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
            new Vector3Int(bgWidth / 2, bgHeight / 2, 0)
        );

        if (bgHeight % 2 == 0) centerWorld -= new Vector3(0, bgTilemap.cellSize.y / 2f, 0);
        if (bgWidth % 2 == 0) centerWorld -= new Vector3(bgTilemap.cellSize.x / 2f, 0, 0);

        Vector3 targetPos = new Vector3(centerWorld.x, centerWorld.y, cam.transform.position.z);

        float worldWidth = bgWidth * bgTilemap.cellSize.x;
        float worldHeight = bgHeight * bgTilemap.cellSize.y;

        float aspect = Screen.width / (float)Screen.height;

        float cameraHalfHeight = worldHeight / 2f;
        float cameraHalfWidth = worldWidth / 2f;

        float targetOrthoSize = Mathf.Max(cameraHalfHeight, cameraHalfWidth / aspect);

        cam.orthographic = true;

        // 기존 트윈 있으면 제거
        cam.transform.DOKill();

        // 카메라 이동 + 줌 동시 트윈
        Sequence seq = DOTween.Sequence();

        seq.Join(
            cam.transform.DOMove(targetPos, 0.6f)
                .SetEase(Ease.InOutCubic)
        );

        seq.Join(
            DOTween.To(
                () => cam.orthographicSize,
                x => cam.orthographicSize = x,
                targetOrthoSize,
                0.6f
            ).SetEase(Ease.InOutCubic)
        );

        seq.OnComplete(() =>
        {
            // 바운더리 계산은 "최종 위치 기준"으로
            Vector3 camPos = cam.transform.position;

            float camHalfH = cam.orthographicSize;
            float camHalfW = camHalfH * aspect;

            Vector2 min = new Vector2(camPos.x - camHalfW, camPos.y - camHalfH);
            Vector2 max = new Vector2(camPos.x + camHalfW, camPos.y + camHalfH);

            Rect boundary = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            Utils.SetBoundary(boundary);
        });
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
        for (int x = circuitStartX; x < circuitStartX + circuitHeight + 1; x++)
        {
            for (int y = circuitStartY; y < circuitStartY + circuitWidth + 1; y++)
            {
                GameObject grid = Instantiate(gridPrefab, gridParent.transform);
                Vector3 topLeft = (Vector3)GetTileTopLeftWorld(x, y);
                grid.transform.position = new(topLeft.x, topLeft.y, grid.transform.position.z);

                int gridX = x - circuitStartX;
                int gridY = y - circuitStartY;
                
                grid.name = $"Grid_{gridX}_{gridY}";
                grid.GetComponent<GridInstance>().Initialize(gridX, gridY);

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
    [SerializeField] private GameObject barrierParent;
    [SerializeField] private GameObject hBarrierPrefab;
    [SerializeField] private GameObject vBarrierPrefab;
    public void PlaceHBarriers(List<Vector2Int> HBarriers)
    {
        foreach (Vector2Int pos in HBarriers)
        {
            GameObject barrier = Instantiate(hBarrierPrefab, barrierParent.transform);
            int x = pos.x + circuitStartX;
            int y = pos.y + circuitStartY;
            Vector2 topLeft = (Vector2)GetTileTopLeftWorld(x, y);
            barrier.transform.position = new Vector3(topLeft.x + GetTileSize().x / 2f, topLeft.y, barrier.transform.position.z);
        }
    }
    public void PlaceVBarriers(List<Vector2Int> VBarriers)
    {
        foreach (Vector2Int pos in VBarriers)
        {
            GameObject barrier = Instantiate(vBarrierPrefab, barrierParent.transform);
            int x = pos.x + circuitStartX;
            int y = pos.y + circuitStartY;
            Vector2 topLeft = (Vector2)GetTileTopLeftWorld(x, y);
            barrier.transform.position = new Vector3(topLeft.x, topLeft.y - GetTileSize().y / 2f, barrier.transform.position.z);
        }
    }
    public void RemoveBarriers()
    {
        foreach (Transform child in barrierParent.transform)
        {
            Destroy(child.gameObject);
        }
    }
    #endregion

    #region Transition
    [HideInInspector] public bool CircuitAppearTransDone = false;
    [HideInInspector] public bool CircuitDisappearTransDone = false;
    public void CircuitAppear()
    {
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(CircuitAppearCo());
    }
    public void CircuitDisappear()
    {
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(CircuitDisappearCo());
    }

    private Tween currentTween = null;
    private Coroutine currentCo = null;
    private IEnumerator CircuitAppearCo()
    {
        currentTween?.Kill();

        Camera cam = Camera.main;
        GameObject circuit = circuitTilemap.gameObject;
        Transform tr = circuit.transform;

        tr.DOKill();

        Vector3 targetPos = tr.position;
        Vector3 viewportPos = cam.WorldToViewportPoint(targetPos);
        viewportPos.y = 1.2f;

        Vector3 startPos = cam.ViewportToWorldPoint(viewportPos);
        startPos.z = targetPos.z;
        tr.position = startPos;

        Tween t = tr.DOMove(targetPos, 1.2f).SetEase(Ease.OutCubic);
        currentTween = t;

        yield return t.WaitForCompletion();

        if (currentTween == t) currentTween = null;
        currentCo = null;

        CircuitAppearTransDone = true;
    }

    private IEnumerator CircuitDisappearCo()
    {
        currentTween?.Kill();

        Camera cam = Camera.main;
        GameObject circuit = circuitTilemap.gameObject;
        Transform tr = circuit.transform;

        tr.DOKill();

        Vector3 startPos = tr.position;
        Vector3 viewportPos = cam.WorldToViewportPoint(startPos);
        viewportPos.y = 1.2f;

        Vector3 targetPos = cam.ViewportToWorldPoint(viewportPos);
        targetPos.z = startPos.z;

        Tween t = tr.DOMove(targetPos, 1.2f).SetEase(Ease.InCubic);
        currentTween = t;

        yield return t.WaitForCompletion();

        if (currentTween == t) currentTween = null;
        currentCo = null;

        CircuitDisappearTransDone = true;
    }
    #endregion
}