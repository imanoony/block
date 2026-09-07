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

    // seamless layout이 10(width) * 6(height)으로 구성되어 있으므로.
    private const int TileGridWidth = 10;
    private const int TileGridHeight = 6;

    private int bgWidth = -1, bgHeight = -1;
    private int bgOffset = 4;
    private int placedBgMinX, placedBgMaxX;
    private int placedBgMinY, placedBgMaxY;
    private bool hasBackground = false;
    public void PlaceBackground(int width, int height)
    {
        if (bgTiles.Count != TileGridWidth * TileGridHeight)
        {
            Utils.PrintError("Invalid background tile count");
            return;
        }

        bgWidth = width;
        bgHeight = height;

        // 카메라 위치 고려해서 수정해야 함
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
                minY, maxY
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
                minY, maxY
            );

            placedBgMinX = minX;
        }

        // Right Expand
        if (maxX > placedBgMaxX)
        {
            FillBackgroundRect(
                placedBgMaxX + 1, maxX,
                minY, maxY
            );

            placedBgMaxX = maxX;
        }

        // Top Expand
        if (minY < placedBgMinY)
        {
            FillBackgroundRect(
                placedBgMinX, placedBgMaxX,
                minY, placedBgMinY - 1
            );

            placedBgMinY = minY;
        }

        // Bottom Expand
        if (maxY > placedBgMaxY)
        {
            FillBackgroundRect(
                placedBgMinX, placedBgMaxX,
                placedBgMaxY + 1, maxY
            );

            placedBgMaxY = maxY;
        }
    }

    private void FillBackgroundRect(
        int minX,
        int maxX,
        int minY,
        int maxY
    )
    {
        int tileX, tileY, index;
        TileBase tile;

        for (int y = minY; y <= maxY; y++)
        {
            tileY = ((y % TileGridHeight) + TileGridHeight) % TileGridHeight;
            for (int x = minX; x <= maxX; x++)
            {
                tileX = ((x % TileGridWidth) + TileGridWidth) % TileGridWidth;

                index = tileY * TileGridWidth + tileX;
                tile = bgTiles[index];
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
    private Tilemap circuitShadow = null;
    public void RemoveCircuits()
    {
        circuitTilemap.ClearAllTiles();
        if (circuitShadow != null) circuitShadow.ClearAllTiles();
    }
    public void PlaceCircuits(
        List<(Vector2Int, int, int)> circuits
    )
    {
        if (circuitTilemap == null || circuitCenter == null) 
        { 
            Utils.PrintError("Invalid tilemap or tiles"); 
            return; 
        }

        circuitTilemap.gameObject.transform.position = Vector3.zero;

        for (int i = 0; i < circuits.Count; i++)
        {
            (Vector2Int start, int width, int height) = circuits[i];
            PlaceCircuit(start.x, start.y, width, height);
            PlaceCircuitBoundary(start.x, start.y, width, height);
        }
    }
    private void PlaceCircuit(
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

        for (int x = startX; x < startX + height; x++)
        {
            for (int y = startY; y < startY + width; y++)
            {
                circuitTilemap.SetTile(TileToCell(x, y), circuitCenter);
            }
        }
    }
    private void PlaceCircuitBoundary(
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
        if (circuitShadow == null)
        {
            circuitShadow = circuitTilemap.gameObject.transform.GetChild(0).GetComponent<Tilemap>();
        }

        for (int x = startX; x < startX + height; x++)
        {
            circuitTilemap.SetTile(TileToCell(x, startY - 1), circuitLeft);
            circuitTilemap.SetTile(TileToCell(x, startY + width), circuitRight);
            circuitShadow.SetTile(TileToCell(x, startY - 1), circuitLeft);
        }
        for (int y = startY; y < startY + width; y++)
        {
            circuitTilemap.SetTile(TileToCell(startX - 1, y), circuitTop);
            circuitTilemap.SetTile(TileToCell(startX + height, y), circuitBotton);
            circuitShadow.SetTile(TileToCell(startX + height, y), circuitBotton);
        }
        circuitTilemap.SetTile(TileToCell(startX - 1, startY - 1), circuitTopLeft);
        circuitTilemap.SetTile(TileToCell(startX - 1, startY + width), circuitTopRight);
        circuitTilemap.SetTile(TileToCell(startX + height, startY - 1), circuitBottomLeft);
        circuitTilemap.SetTile(TileToCell(startX + height, startY + width), circuitBottomRight);
        circuitShadow.SetTile(TileToCell(startX - 1, startY - 1), circuitTopLeft);
        circuitShadow.SetTile(TileToCell(startX + height, startY - 1), circuitBottomLeft);
        circuitShadow.SetTile(TileToCell(startX + height, startY + width), circuitBottomRight);
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

        float minWorldWidth = bgWidth * bgTilemap.cellSize.x + 1f;
        float minWorldHeight = bgHeight * bgTilemap.cellSize.y + 1f;

        float aspect = Screen.width / (float)Screen.height;

        float cameraHalfWidth = minWorldWidth / 2f;
        float cameraHalfHeight = minWorldHeight / 2f;

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
    public void PlaceGrids(Grid[,] grids)
    {
        this.grids = new List<GameObject>();
        int height = grids.GetLength(0);
        int width = grids.GetLength(1);

        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                if (grids[x, y].Type == GridType.Null) continue;

                GameObject grid = Instantiate(gridPrefab, gridParent.transform);
                Vector3 topLeft = (Vector3)GetTileTopLeftWorld(x, y);
                grid.transform.position = new(topLeft.x, topLeft.y, grid.transform.position.z);
                
                grid.name = $"Grid_{x}_{y}";
                grid.GetComponent<GridInstance>().Initialize(x, y);

                this.grids.Add(grid);
            }
        }
    }
    #endregion

    #region Tile Boundary Placement 
    [Header("Tile Boundary")]
    [SerializeField] private GameObject tileBoundary;
    private SpriteRenderer tileBoundarySr = null;
    public void RemoveTileBoundary()
    {
        tileBoundary.SetActive(false);
    }

    public void PlaceTileBoundary()
    {
        if (tileBoundarySr == null) tileBoundarySr = tileBoundary.GetComponent<SpriteRenderer>();

        tileBoundarySr.color = new(
            tileBoundarySr.color.r,
            tileBoundarySr.color.g,
            tileBoundarySr.color.b,
            0f
        );
        tileBoundarySr.size = new(
            bgWidth,
            bgHeight
        );
        tileBoundary.transform.position = new(tileBoundarySr.size.x / 2f, tileBoundarySr.size.y / 2f);

        tileBoundary.SetActive(true);
        
        Bounds b = tileBoundarySr.bounds;
        Rect r = new(b.min.x, b.min.y, b.size.x, b.size.y);
        Utils.SetBoundary(r);
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
            Vector2 topLeft = (Vector2)GetTileTopLeftWorld(pos.x, pos.y);
            barrier.transform.position = new Vector3(topLeft.x + GetTileSize().x / 2f, topLeft.y, barrier.transform.position.z);
        }
    }
    public void PlaceVBarriers(List<Vector2Int> VBarriers)
    {
        foreach (Vector2Int pos in VBarriers)
        {
            GameObject barrier = Instantiate(vBarrierPrefab, barrierParent.transform);
            Vector2 topLeft = (Vector2)GetTileTopLeftWorld(pos.x, pos.y);
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
        if (currentCircuitCo != null) StopCoroutine(currentCircuitCo);
        currentCircuitCo = StartCoroutine(CircuitAppearCo());
    }
    public void CircuitDisappear()
    {
        if (currentCircuitCo != null) StopCoroutine(currentCircuitCo);
        currentCircuitCo = StartCoroutine(CircuitDisappearCo());
    }

    private Tween currentCircuitTween = null;
    private Coroutine currentCircuitCo = null;
    private IEnumerator CircuitAppearCo()
    {
        currentCircuitTween?.Kill();

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
        currentCircuitTween = t;

        yield return t.WaitForCompletion();

        if (currentCircuitTween == t) currentCircuitTween = null;
        currentCircuitCo = null;

        CircuitAppearTransDone = true;
    }

    private IEnumerator CircuitDisappearCo()
    {
        currentCircuitTween?.Kill();

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
        currentCircuitTween = t;

        yield return t.WaitForCompletion();

        if (currentCircuitTween == t) currentCircuitTween = null;
        currentCircuitCo = null;

        CircuitDisappearTransDone = true;
    }

    [HideInInspector] public bool TileBoundaryAppearTransDone = false;
    [HideInInspector] public bool TileBoundaryDisappearTransDone = false;

    public void TileBoundaryAppear()
    {
        if (currentTileBoundaryCo != null) StopCoroutine(currentTileBoundaryCo);
        currentTileBoundaryCo = StartCoroutine(TileBoundaryAppearCo());
    }
    public void TileBoundaryDisappear()
    {
        if (currentTileBoundaryCo != null) StopCoroutine(currentTileBoundaryCo);
        currentTileBoundaryCo = StartCoroutine(TileBoundaryDisappearCo());
    }

    private Tween currentTileBoundaryTween = null;
    private Coroutine currentTileBoundaryCo = null;

    private IEnumerator TileBoundaryAppearCo()
    {
        currentTileBoundaryTween?.Kill();

        float targetA = 0.3f;

        Tween t = tileBoundarySr.DOFade(targetA, 0.6f).SetEase(Ease.OutCubic);
        currentTileBoundaryTween = t;

        yield return t.WaitForCompletion();

        if (currentTileBoundaryTween == t) currentTileBoundaryTween = null;
        currentTileBoundaryCo = null;

        TileBoundaryAppearTransDone = true;
    }
    private IEnumerator TileBoundaryDisappearCo()
    {
        currentTileBoundaryTween?.Kill();

        float targetA = 0f;

        Tween t = tileBoundarySr.DOFade(targetA, 0.6f).SetEase(Ease.Linear);
        currentTileBoundaryTween = t;

        yield return t.WaitForCompletion();

        if (currentTileBoundaryTween == t) currentTileBoundaryTween = null;
        currentTileBoundaryCo = null;

        TileBoundaryDisappearTransDone = true;
    }
    #endregion
}