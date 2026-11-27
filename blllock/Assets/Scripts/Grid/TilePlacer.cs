using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;
using System.Collections;
using System.Linq;

// Tilemap-based
public class TilePlacer : MonoBehaviour
{   
    [Header("Tile Configs for Chapters")]
    [SerializeField] private TileConfig chapter1TileConfig;
    [SerializeField] private TileConfig chapter2TileConfig;
    [SerializeField] private TileConfig chapter3TileConfig;
    [SerializeField] private TileConfig chapter4TileConfig;

    #region Background Placement
    [Header("Background")]
    [SerializeField] private Tilemap bgTilemap;

    private int bgWidth = -1, bgHeight = -1;
    private int bgOffset = 4;
    private int placedBgMinX, placedBgMaxX;
    private int placedBgMinY, placedBgMaxY;
    private TileBase[] bgTiles;
    private int bgResourceWidth, bgResourceHeight;
    private bool hasBackground = false;
    public void PlaceBackground(
        int width, 
        int height,
        int chapter
    )
    {
        TileConfig config = chapter == 1 ? chapter1TileConfig :
                            chapter == 2 ? chapter2TileConfig :
                            chapter == 3 ? chapter3TileConfig :
                            chapter == 4 ? chapter4TileConfig :
                            throw new Exception("PlaceBackground: invalid chapter");

        if (config.bgTiles.Length != config.bgResourceWidth * config.bgResourceHeight)
            throw new Exception("PlaceBackground: invalid bg config setting");                    

        bgWidth = width;
        bgHeight = height;

        bgTiles = config.bgTiles;
        bgResourceWidth = config.bgResourceWidth;
        bgResourceHeight = config.bgResourceHeight;

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
            tileY = ((y % bgResourceHeight) + bgResourceHeight) % bgResourceHeight;
            for (int x = minX; x <= maxX; x++)
            {
                tileX = ((x % bgResourceWidth) + bgResourceWidth) % bgResourceWidth;

                index = tileY * bgResourceWidth + tileX;
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
    private Tilemap circuitShadow = null;
    public void RemoveCircuits()
    {
        circuitTilemap.ClearAllTiles();
        if (circuitShadow != null) circuitShadow.ClearAllTiles();
    }
    public void PlaceCircuits(
        List<(Vector2Int, int, int)> circuits,
        int chapter
    )
    {
        TileConfig config = chapter == 1 ? chapter1TileConfig :
                            chapter == 2 ? chapter2TileConfig :
                            chapter == 3 ? chapter3TileConfig :
                            chapter == 4 ? chapter4TileConfig :
                            throw new Exception("PlaceCircuits: invalid chapter");

        circuitTilemap.gameObject.transform.position = Vector3.zero;

        for (int i = 0; i < circuits.Count; i++)
        {
            (Vector2Int start, int width, int height) = circuits[i];
            PlaceCircuit(start.x, start.y, width, height, chapter, config);
            PlaceCircuitBoundary(start.x, start.y, width, height, chapter, config);
        }
    }
    private void PlaceCircuit(
        int startX,
        int startY,
        int width,
        int height,
        int chapter,
        TileConfig config
    ) // 일단은 하드하게 (임시)
    {
        if (circuitTilemap == null)
            throw new Exception("PlaceCircuit: circuit tilemap is null");

        if (config.circuitTiles.Length != config.circuitResourceHeight * config.circuitResourceWidth)
            throw new Exception("PlaceCircuit: invalid circuit config setting");

        circuitTilemap.gameObject.transform.position = Vector3.zero;

        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                int index = 0;
                if (chapter == 1)
                {
                    index = 4;
                }
                else if (chapter == 2)
                {
                    if (x < height / 2)
                    {
                        if (x == 0) index = 4;
                        else if (x == height / 2 - 1) index = 10;
                        else index = 7;
                    }
                    else
                    {
                        if (x == height - 1) index = 16;
                        else if (x == height / 2) index = 13;
                        else index = 7;
                    }
                }
                else if (chapter == 3)
                {
                    // TODO (later)
                }
                else if (chapter == 4)
                {
                    // TODO (later)
                }
                else throw new Exception("PlaceCircuit: invalid chapter");

                circuitTilemap.SetTile(
                    TileToCell(x + startX, y + startY), 
                    config.circuitTiles[index]
                );
            }
        }
    }
    private void PlaceCircuitBoundary(
        int startX,
        int startY,
        int width,
        int height,
        int chapter,
        TileConfig config
    ) // 얘도 일단은 하드하게
    {
        if (circuitTilemap == null)
            throw new Exception("PlaceCircuitBoundary: circuit tilemap is null");

        if (config.circuitTiles.Length != config.circuitResourceHeight * config.circuitResourceWidth)
            throw new Exception("PlaceCircuitBoundary: invalid circuit config setting");

        if (circuitShadow == null)
        {
            circuitShadow = circuitTilemap.gameObject.transform.GetChild(0).GetComponent<Tilemap>();
        }

        for (int x = 0; x < height; x++)
        {
            int leftIndex = 0, rightIndex = 0;
            if (chapter == 1)
            {
                leftIndex = 3;
                rightIndex = 5;
            }
            else if (chapter == 2)
            {
                if (x < height / 2)
                {
                    if (x == 0) { leftIndex = 3; rightIndex = 5; }
                    else if (x == height / 2 - 1) { leftIndex = 9; rightIndex = 11; }
                    else { leftIndex = 6; rightIndex = 8; }
                }
                else
                {
                    if (x == height - 1) { leftIndex = 15; rightIndex = 17; }
                    else if (x == height / 2) { leftIndex = 12; rightIndex = 14; }
                    else { leftIndex = 6; rightIndex = 8; }
                }
            }
            else if (chapter == 3)
            {
                // TODO (later)
            }
            else if (chapter == 4)
            {
                // TODO (later)
            }
            else throw new Exception("PlaceCircuitBoundary: invalid chapter");

            circuitTilemap.SetTile(
                TileToCell(x + startX, startY - 1), 
                config.circuitTiles[leftIndex]
            );
            circuitTilemap.SetTile(
                TileToCell(x + startX, startY + width),
                config.circuitTiles[rightIndex]
            );
            circuitShadow.SetTile(
                TileToCell(x + startX, startY - 1),
                config.circuitTiles[leftIndex]
            );
        }
        for (int y = startY; y < startY + width; y++)
        {
            int topIndex = 0, bottomIndex = 0;
            if (chapter == 1) { topIndex = 1; bottomIndex = 7; }
            else if (chapter == 2) { topIndex = 1; bottomIndex = 19; }
            else if (chapter == 3) {}
            else if (chapter == 4) {}
            else throw new Exception("PlaceCircuitBoundary: invalid chapter");

            circuitTilemap.SetTile(TileToCell(startX - 1, y), config.circuitTiles[topIndex]);
            circuitTilemap.SetTile(TileToCell(startX + height, y), config.circuitTiles[bottomIndex]);
            circuitShadow.SetTile(TileToCell(startX + height, y), config.circuitTiles[bottomIndex]);
        }

        int topLeftIndex = 0, topRightIndex = 0, bottomLeftIndex = 0, bottomRightIndex = 0;
        if (chapter == 1) { topLeftIndex = 0; topRightIndex = 2; bottomLeftIndex = 6; bottomRightIndex = 8; }
        else if (chapter == 2) { topLeftIndex = 0; topRightIndex = 2; bottomLeftIndex = 18; bottomRightIndex = 20; }
        else if (chapter == 3) {}
        else if (chapter == 4) {}
        else throw new Exception("PlaceCircuitBoundary: invalid chapter");

        circuitTilemap.SetTile(TileToCell(startX - 1, startY - 1), config.circuitTiles[topLeftIndex]);
        circuitTilemap.SetTile(TileToCell(startX - 1, startY + width), config.circuitTiles[topRightIndex]);
        circuitTilemap.SetTile(TileToCell(startX + height, startY - 1), config.circuitTiles[bottomLeftIndex]);
        circuitTilemap.SetTile(TileToCell(startX + height, startY + width), config.circuitTiles[bottomRightIndex]);
        circuitShadow.SetTile(TileToCell(startX - 1, startY - 1), config.circuitTiles[topLeftIndex]);
        circuitShadow.SetTile(TileToCell(startX + height, startY - 1), config.circuitTiles[bottomLeftIndex]);
        circuitShadow.SetTile(TileToCell(startX + height, startY + width), config.circuitTiles[bottomRightIndex]);
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
    [SerializeField] private GameObject tileBarrierPrefab;
    [SerializeField] private List<Sprite> tileBarrierSprites;
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
    public void PlaceTileBarriers(List<Vector2Int> tileBarriers)
    {
        int spriteCount = tileBarrierSprites.Count;
        foreach (Vector2Int pos in tileBarriers)
        {
            GameObject barrier = Instantiate(tileBarrierPrefab, barrierParent.transform);
            SpriteRenderer barrierSr = barrier.GetComponent<SpriteRenderer>();
            Sprite barrierSprite = tileBarrierSprites[UnityEngine.Random.Range(0, spriteCount)];
            Vector2 center = (Vector2)GetTileCenterWorld(pos.x, pos.y);

            barrierSr.sprite = barrierSprite;
            barrier.transform.position = new Vector3(center.x, center.y, barrier.transform.position.z);
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