using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using UnityEngine.Rendering;

public class BlockInstance : MonoBehaviour
{
    private BlockData blockData;
    private bool isDragging = false;
    private bool isTweening = false;
    private GridManager gm;
    private SpriteRenderer sr;
    private SortingGroup sg;
    private Color color = Color.white;
    private GameObject shadow;
    [SerializeField] private GameObject ghostPrefab;
    private GameObject ghost;
    private Vector2 blockPos;
    private Vector2Int blockTilePos;

    private Transform unplacedRoot = null;
    private Transform placedRoot = null;
    public bool CanRotateCW { get; private set; } = false;
    public bool CanRotateCCW { get; private set; } = false;
    public bool CanFlipX { get; private set; } = false;
    public bool CanFlipY { get; private set; } = false;
    public bool HasSpike { get; private set; } = false;
    public void Initialize(
        BlockData blockData, 
        Sprite sprite, 
        Vector2Int initPos,
        Sprite ghostSprite,
        Transform placedRoot,
        bool canRotateCW = false, 
        bool canRotateCCW = false,
        bool canFlipX = false,
        bool canFlipY = false,
        bool hasSpike = false
    )
    {
        if (blockData == null) { Utils.PrintError("BlockData는 Null일 수 없음."); return; }
        this.blockData = blockData;
        this.placedRoot = placedRoot;
        this.unplacedRoot = gameObject.transform.parent;

        this.blockData.Instantiate();
        CanRotateCW = canRotateCW;
        CanRotateCCW = canRotateCCW;
        CanFlipX = canFlipX;
        CanFlipY = canFlipY;
        
        HasSpike = hasSpike;
        if (HasSpike) this.blockData.SetSpike();

        gm = GameManager.Instance.Grid;

        sr = gameObject.GetComponent<SpriteRenderer>();
        sg = gameObject.GetComponent<SortingGroup>();
        sr.sprite = sprite;
        sr.color = color;

        PolygonCollider2D poly = gameObject.GetComponent<PolygonCollider2D>();
        poly.pathCount = sprite.GetPhysicsShapeCount();
        for (int i = 0; i < poly.pathCount; i++)
        {
            List<Vector2> shape = new();
            sprite.GetPhysicsShape(i, shape);
            poly.SetPath(i, shape.ToArray());
        }

        // Shadow Sprite 설정
        shadow = transform.GetChild(0).gameObject;
        SpriteRenderer shadowSr = shadow.GetComponent<SpriteRenderer>();
        shadowSr.sprite = sprite;

        if (!Place(gm, initPos))
        {
            Debug.LogError("Failed to place block at initial position: " + initPos);
        }
        transform.position = gm.GetBlockCenterOnTile(initPos.x, initPos.y, blockData.Height, blockData.Width);
        transform.position = new Vector3(transform.position.x, transform.position.y, Utils.BLOCK_Z);
        blockPos = transform.position;
        blockTilePos = initPos;
        
        ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
        ghost.GetComponent<SpriteRenderer>().sprite = ghostSprite;
        ghost.SetActive(false);
    }

    private void OnDestroy()
    {
        Destroy(ghost);
    }

    #region Interaction
    private Vector2 dragOffset;
    private float dragSmoothTime = 0.07f;
    private float ghostSmoothTime = 0.1f;
    private Vector3 dragVelocity;
    private Vector3 ghostVelocity;
    private Vector3 targetBlockPos;
    private Vector3 targetGhostPos;
    private Vector2Int? currentGhostSnapPos = null;
    public void BeginDrag()
    {
        //if (currentCoroutine != null) return;
        if (GameManager.Instance.State != GameState.InGame) return;
        if (isTweening) return;

        GameManager.Instance.UI.BlockTooltipDisappear();

        isDragging = true;
        dragOffset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (isPlaced) Unplace(gm);

        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.SetActive(true);

        targetBlockPos = transform.position;
        targetGhostPos = ghost.transform.position;

        if (shadowCo != null) StopCoroutine(shadowCo);
        shadowCo = StartCoroutine(ShadowOffCo(0.3f));

        sg.sortingOrder += 10;
    }

    public void EndDrag()
    {
        isDragging = false;
        ghost.SetActive(false);

        if (shadowCo != null) StopCoroutine(shadowCo);
        shadowCo = StartCoroutine(ShadowOnCo(0.3f));

        sg.sortingOrder -= 10;

        currentGhostSnapPos = null;

        List<Vector2Int?> snapPosList = gm.GetNearestTiles(GetBaseTilePos(), Utils.MAX_SNAP_COUNT);
        for (int i = 0; i < snapPosList.Count; i++)
        {
            Vector2Int? snapPos = snapPosList[i];
            if (snapPos == null)
            {
                transform.position = blockPos;
                Place(gm, blockTilePos);
                StartCoroutine(PlaceCo(blockTilePos, () => { isTweening = false; }));
                return;
            }
            if (Place(gm, (Vector2Int)snapPos))
            {
                StartCoroutine(PlaceCo(snapPos.Value, () => { isTweening = false; }));
                blockPos = transform.position;
                blockTilePos = snapPos.Value;
                return;
            }
        }

        transform.position = blockPos;
        Place(gm, blockTilePos);
        StartCoroutine(PlaceCo(blockTilePos, () => { isTweening = false; }));
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 newPos = mousePos + dragOffset;

            targetBlockPos = GetClampedPos(new Vector3(newPos.x, newPos.y, Utils.BLOCK_Z));
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetBlockPos,
                ref dragVelocity,
                dragSmoothTime
            );

            // Ghost 위치 표시
            Vector2Int? snapPos = gm.GetNearestTiles(GetBaseTilePos())[0];
            if (snapPos != null)
            {
                if (
                    currentGhostSnapPos == null ||
                    currentGhostSnapPos.Value != snapPos.Value
                )
                {
                    currentGhostSnapPos = snapPos;
                    targetGhostPos = gm.GetBlockCenterOnTile(
                        snapPos.Value.x, 
                        snapPos.Value.y, 
                        blockData.Height, 
                        blockData.Width
                    );
                }
                ghost.transform.position = Vector3.SmoothDamp(
                    ghost.transform.position,
                    targetGhostPos,
                    ref ghostVelocity,
                    ghostSmoothTime
                );

                if (CanPlace(gm, snapPos.Value))
                {
                    ghost.GetComponent<SpriteRenderer>().color = Color.white;
                }
                else
                {
                    ghost.GetComponent<SpriteRenderer>().color = Utils.CodeToColor("#FF0034");
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (GameManager.Instance.State != GameState.InGame) return;
            if (isDragging) return;
            if (isTweening) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (CanRotateCW) RotateCW();
                else if (CanRotateCCW) RotateCCW();
            }
        }
    }

    private void OnMouseDown() => BeginDrag();

    private Vector3 GetClampedPos(Vector3 pos)
    {
        Rect boundary = Utils.Boundary;
        Vector3 tileSize = GameManager.Instance.Grid.GetTileSize();
        Vector3 clamped = new();

        clamped.x = Mathf.Clamp(pos.x, boundary.xMin + tileSize.x * blockData.Width / 2f, boundary.xMax - tileSize.x * blockData.Width / 2f);
        clamped.y = Mathf.Clamp(pos.y, boundary.yMin + tileSize.y * blockData.Height / 2f, boundary.yMax - tileSize.y * blockData.Height / 2f);
        clamped.z = Utils.BLOCK_Z;

        return clamped;
    }

    // Block Instance Hover
    private bool isHovering = false;
    private void OnMouseEnter()
    {
        if (GameManager.Instance.State != GameState.InGame) return;
        if (isHovering) return;
        if (isDragging) return;
        if (isTweening) return;
        //if (isPlaced || isDragging || isHovering || currentCoroutine != null) return;

        //Vector3 tooltipPos = transform.position + new Vector3(0, (blockData.Height + 1.4f) / 2f * GameManager.Instance.Grid.GetTileSize().y, 0);
        //GameManager.Instance.UI.BlockTooltipAppear(CanRotate, CanFlip, tooltipPos);

        //Vector3 offset = Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);
        //gameObject.transform.position += Utils.HOVER;
        //shadow.transform.localPosition -= offset;

        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleUpCo(0.4f));

        isHovering = true;
    }
    private void OnMouseExit()
    {
        //GameManager.Instance.UI.BlockTooltipDisappear();

        if (GameManager.Instance.State != GameState.InGame) return;
        if (!isHovering) return;
        if (isDragging) return;
        if (isTweening) return;
        //if (isPlaced || isDragging || !isHovering || currentCoroutine != null) return;
        //Vector3 offset = Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);
        //gameObject.transform.position -= Utils.HOVER;
        //shadow.transform.localPosition += offset;

        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleDownCo(0.4f));

        isHovering = false;
    }
    private void OnMouseUp()
    {
        if (isHovering) OnMouseExit();
    }

    #endregion

    #region Placement
    private bool isPlaced = false;
    private Vector2Int baseTile = new(-1, -1);
    public bool Valid = true;

    private bool CanPlace(GridManager gm, Vector2Int baseTile)
    {
        if (!gm.IsValidPos(blockData, baseTile))
        {
            Debug.Log("Invalid Pos");
            Debug.Log($"Block Size: {blockData.Height} x {blockData.Width}");
            Debug.Log($"Base Tile: {baseTile.x}, {baseTile.y}");
            Debug.Log($"Block Tiles: {string.Join(", ", blockData.Tiles)}");
            return false;
        }

        return true;
    }

    // Invalid Position이면 아예 둘 수 없다.
    // Invalid Ports면 둘 수는 있으나 Block Instance의 Valid가 false가 된다.
    // Valid가 false인 block들은 타일에 변화가 있을 때마다 
    private bool Place(GridManager gm, Vector2Int baseTile)
    {
        if (isPlaced || !this.baseTile.Equals(new(-1, -1))) return false;
        if (!CanPlace(gm, baseTile)) return false;

        isPlaced = true;
        this.baseTile = baseTile;

        if (gm.IsInCircuit(baseTile.x, baseTile.y)) transform.SetParent(placedRoot);
        else transform.SetParent(unplacedRoot);

        if (!gm.PlaceBlock(blockData, baseTile))
        {
            Valid = false;
            sr.color = Utils.CodeToColor(Utils.RED);
            gm.AddInvalid(this);
        }
        //shadow.SetActive(false);
        return true;
    }

    private void Unplace(GridManager gm)
    {
        if (!isPlaced || baseTile.Equals(new(-1, -1))) return;
        gm.RemoveBlock(blockData, baseTile, Valid);
        isPlaced = false;
        baseTile = new(-1, -1);

        Valid = true;
        sr.color = color;
        gm.RemoveInvalid(this);

        //shadow.SetActive(true);
    }

    public bool Check(GridManager gm)
    {
        if (!isPlaced || baseTile.Equals(new(-1, -1)) || Valid) return false;
        if (!gm.PlaceBlock(blockData, baseTile)) return false;

        Valid = true;
        sr.color = color;
        return true;
    }

    private Vector3 GetBaseTilePos() => transform.position + new Vector3(-blockData.Width / 2f, blockData.Height / 2f, 0);
    private IEnumerator PlaceCo(Vector2Int baseTile, Action onComplete = null)
    {
        isTweening = true;

        Vector3 targetPos =
            gm.GetBlockCenterOnTile(
                baseTile.x,
                baseTile.y,
                blockData.Height,
                blockData.Width
            );
        targetPos.z = Utils.BLOCK_Z;

        Tween tween = transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutCubic);

        yield return tween.WaitForCompletion();

        onComplete?.Invoke();
        yield break;
    }
    #endregion

    #region Rotate & Flip
    private bool RotateCW()
    {
        if (!CanRotateCW) return false;

        if (isPlaced) Unplace(gm);
        blockData.RotateCW();

        List<Vector2Int?> snapPosList = gm.GetNearestTiles(GetBaseTilePos(), Utils.MAX_SNAP_COUNT);
        for (int i = 0; i < snapPosList.Count; i++)
        {
            Vector2Int? snapPos = snapPosList[i];
            if (snapPos == null)
            {
                blockData.RotateCCW();
                transform.position = blockPos;
                Place(gm, blockTilePos);

                StartCoroutine(RotateFailCo(isCW: true));
                return false;
            }
            if (CanPlace(gm, (Vector2Int)snapPos))
            {
                StartCoroutine(RotateCo(
                    isCW: true,
                    () =>
                    {
                        Place(gm, (Vector2Int)snapPos);
                        blockPos = transform.position;
                        blockTilePos = snapPos.Value;
                        StartCoroutine(PlaceCo(snapPos.Value, () => { isTweening = false; }));
                    }
                ));
                return true;
            }
        }
        
        blockData.RotateCCW();
        transform.position = blockPos;
        Place(gm, blockTilePos);

        StartCoroutine(RotateFailCo(isCW: true));
        return false;
    }
    private bool RotateCCW()
    {
        if (!CanRotateCCW) return false;
        
        if (isPlaced) Unplace(gm);
        blockData.RotateCCW();

        List<Vector2Int?> snapPosList = gm.GetNearestTiles(GetBaseTilePos(), Utils.MAX_SNAP_COUNT);
        for (int i = 0; i < snapPosList.Count; i++)
        {
            Vector2Int? snapPos = snapPosList[i];
            if (snapPos == null)
            {
                blockData.RotateCW();
                transform.position = blockPos;
                Place(gm, blockTilePos);

                StartCoroutine(RotateFailCo(isCW: false));
                return false;
            }
            if (CanPlace(gm, (Vector2Int)snapPos))
            {
                if (isPlaced) Unplace(gm);
                StartCoroutine(RotateCo(
                    isCW: false,
                    () =>
                    {
                        Place(gm, (Vector2Int)snapPos);
                        blockPos = transform.position;
                        blockTilePos = snapPos.Value;
                        StartCoroutine(PlaceCo(snapPos.Value, () => { isTweening = false; }));
                    }
                ));
                return true;
            }
        }
        
        blockData.RotateCW();
        transform.position = blockPos;
        Place(gm, blockTilePos);

        StartCoroutine(RotateFailCo(isCW: false));
        return false;
    }
    private IEnumerator RotateCo(bool isCW, Action onComplete = null)
    {
        isTweening = true;
        GameManager.Instance.ActionOn();

        if (shadowCo != null) StopCoroutine(shadowCo);
        shadowCo = StartCoroutine(ShadowOffCo(0.2f));
        yield return shadowCo;

        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleUpCo(0.2f));
        yield return scaleCo;

        Tween rotateT = transform.DORotate(
            transform.eulerAngles + new Vector3(0, 0, isCW ? -90f : 90f),
            0.12f,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad);

        yield return rotateT.WaitForCompletion();
        yield return StartCoroutine(ScaleDownCo(0.1f));

        shadow.transform.localPosition = -Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);
        yield return StartCoroutine(ShadowOnCo(0.2f));

        onComplete?.Invoke();
        GameManager.Instance.ActionOff();
        yield break;
    }
    private IEnumerator RotateFailCo(bool isCW)
    {
        // TODO

        yield break;
    }
    #endregion

    #region Transition
    // 연출 관련 세팅은 하드하게.

    private Coroutine shadowCo = null;
    private IEnumerator ShadowOnCo(float duration)
    {
        SpriteRenderer sr = shadow.GetComponent<SpriteRenderer>();
        shadow.SetActive(true);

        Color c = sr.color;
        c.a = 0f;
        sr.color = c;

        Tween tween = sr.DOFade(0.5f, duration);

        yield return tween.WaitForCompletion();
        shadowCo = null;
    }
    private IEnumerator ShadowOffCo(float duration)
    {
        SpriteRenderer sr = shadow.GetComponent<SpriteRenderer>();

        Tween tween = sr.DOFade(0f, duration);

        yield return tween.WaitForCompletion();
        shadow.SetActive(false);
        shadowCo = null;
    }

    private Coroutine scaleCo = null;
    private IEnumerator ScaleUpCo(float duration, float scaleFactor = 1.05f)
    {
        Tween tween = transform.DOScale(scaleFactor, duration).SetEase(Ease.OutQuad);

        yield return tween.WaitForCompletion();
        scaleCo = null;
    }
    private IEnumerator ScaleDownCo(float duration)
    {
        Tween tween = transform.DOScale(1f, duration).SetEase(Ease.OutQuad);

        yield return tween.WaitForCompletion();
        scaleCo = null;
    }

    #endregion
}
