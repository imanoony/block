using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    private BlockData blockData;
    private bool isDragging = false;
    private GridManager gm;
    private SpriteRenderer sr;
    private Color color;
    private GameObject shadow;

    public bool CanRotate { get; private set; } = false;
    public bool CanFlip { get; private set; } = false;
    public void Initialize(BlockData blockData, Sprite sprite, bool canRotate = false, bool canFlip = false)
    {
        if (blockData == null) { Utils.PrintError("BlockData는 Null일 수 없음."); return; }
        this.blockData = blockData;

        this.blockData.Instantiate();
        CanRotate = canRotate;
        CanFlip = canFlip;
        if (CanRotate) color = Utils.CodeToColor(Utils.GREEN);
        else if (CanFlip) color = Utils.CodeToColor(Utils.YELLOW);
        else color = Color.white;

        gm = GameManager.Instance.Grid;

        sr = gameObject.GetComponent<SpriteRenderer>();
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
    }

    #region Interaction
    private Vector2 dragOffset;
    public void BeginDrag()
    {
        if (currentCoroutine != null) return;
        if (GameManager.Instance.State != GameState.InGame) return;

        GameManager.Instance.UI.BlockTooltipDisappear();

        isDragging = true;
        dragOffset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (isPlaced) Unplace(gm);
    }

    public void EndDrag()
    {
        isDragging = false;

        Vector2Int? snapPos = gm.GetNearestTile(GetBaseTilePos());
        if (snapPos == null) return; // 여기에 블록 슬라이딩 로직

        if (!Place(gm, (Vector2Int)snapPos)) return; // 여기도 블록 슬라이딩 로직
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.InGame) return;

        if (isDragging)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 newPos = mousePos + dragOffset;
            transform.position = GetClampedPos(new Vector3(newPos.x, newPos.y, Utils.BLOCK_Z));
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (isPlaced || currentCoroutine != null) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (CanRotate) currentCoroutine = StartCoroutine(Rotate(isPlaced));
                else if (CanFlip) currentCoroutine = StartCoroutine(Flip(isPlaced));
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
        if (isPlaced || isDragging || isHovering || currentCoroutine != null) return;

        Vector3 tooltipPos = transform.position + new Vector3(0, (blockData.Height + 1.4f) / 2f * GameManager.Instance.Grid.GetTileSize().y, 0);
        GameManager.Instance.UI.BlockTooltipAppear(CanRotate, CanFlip, tooltipPos);

        Vector3 offset = Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);
        gameObject.transform.position += Utils.HOVER;
        shadow.transform.localPosition -= offset;
        isHovering = true;
    }
    private void OnMouseExit()
    {
        GameManager.Instance.UI.BlockTooltipDisappear();

        if (GameManager.Instance.State != GameState.InGame) return;
        if (isPlaced || isDragging || !isHovering || currentCoroutine != null) return;
        Vector3 offset = Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);
        gameObject.transform.position -= Utils.HOVER;
        shadow.transform.localPosition += offset;
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

    // Invalid Position이면 아예 둘 수 없다.
    // Invalid Ports면 둘 수는 있으나 Block Instance의 Valid가 false가 된다.
    // Valid가 false인 block들은 타일에 변화가 있을 때마다 
    private bool Place(GridManager gm, Vector2Int baseTile)
    {
        if (isPlaced || !this.baseTile.Equals(new(-1, -1))) return false;
        if (!gm.IsValidPos(blockData, baseTile))
        {
            Debug.Log("Invalid Pos");
            Debug.Log($"Block Size: {blockData.Height} x {blockData.Width}");
            Debug.Log($"Base Tile: {baseTile.x}, {baseTile.y}");
            Debug.Log($"Block Tiles: {string.Join(", ", blockData.Tiles)}");
            return false;
        }

        isPlaced = true;
        this.baseTile = baseTile;

        // 블록의 위치를 snap position (좌표) 에 동기화
        transform.position = gm.GetBlockCenterOnTile(baseTile.x, baseTile.y, blockData.Height, blockData.Width);
        transform.position = new(transform.position.x, transform.position.y, Utils.BLOCK_Z);
        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        if (!gm.PlaceBlock(blockData, baseTile))
        {
            Valid = false;
            sr.color = Utils.CodeToColor(Utils.RED);
            gm.AddInvalid(this);
        }

        gameObject.GetComponent<SpriteRenderer>().sortingOrder -= 2;
        shadow.SetActive(false);
        return true;
    }

    private void Unplace(GridManager gm)
    {
        if (!isPlaced || baseTile.Equals(new(-1, -1))) return;
        gm.RemoveBlock(blockData, baseTile, Valid);
        isPlaced = false;
        baseTile = new(-1, -1);
        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;

        Valid = true;
        sr.color = color;
        gm.RemoveInvalid(this);

        gameObject.GetComponent<SpriteRenderer>().sortingOrder += 2;
        shadow.SetActive(true);
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
    #endregion

    #region Rotate & Flip
    private Coroutine currentCoroutine = null;
    private float time = 0.2f;
    private IEnumerator Rotate(bool isPlaced)
    {
        if (!CanRotate) yield break;
        isHovering = true;
        GameManager.Instance.UI.BlockTooltipDisappear();

        Vector2Int baseTile = this.baseTile;
        if (isPlaced) Unplace(gm);

        yield return StartCoroutine(ShadowDisappear());

        blockData.Rotation();
        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        float startZ = transform.rotation.eulerAngles.z;
        float targetZ = -(int)blockData.BlockRotate;

        // 0~360 정규화
        startZ %= 360f; if (startZ < 0f) startZ += 360f;
        targetZ %= 360f; if (targetZ < 0f) targetZ += 360f;

        float deltaZ = targetZ - startZ;
        if (deltaZ > 0f) deltaZ -= 360f; // 항상 시계 방향

        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);

            // SmoothStep를 사용해서 휙 바뀌는 느낌
            t = Mathf.Pow(t, 0.3f); // 초반 빠르게 → 끝에 살짝 느리게

            float newZ = startZ + deltaZ * t;

            Vector3 euler = transform.rotation.eulerAngles;
            euler.z = newZ;
            transform.rotation = Quaternion.Euler(euler);

            yield return null;
        }

        // 마지막 보정
        Vector3 finalEuler = transform.rotation.eulerAngles;
        finalEuler.z = targetZ;
        transform.rotation = Quaternion.Euler(finalEuler);

        shadow.transform.localPosition = -Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);

        yield return StartCoroutine(ShadowAppear());

        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        if (isPlaced) Place(gm, baseTile);

        currentCoroutine = null;
        isHovering = false;
    }


    private IEnumerator Flip(bool isPlaced)
    {
        if (!CanFlip) yield break;
        isHovering = true;

        Vector2Int baseTile = this.baseTile;
        if (isPlaced) Unplace(gm);

        blockData.FlipX();
        sr.flipX = blockData.BlockFlipX;

        shadow.transform.localPosition = -Utils.GetHoverOffset(blockData.BlockRotate, blockData.BlockFlipX);
        shadow.GetComponent<SpriteRenderer>().flipX = blockData.BlockFlipX;

        if (isPlaced) Place(gm, baseTile);

        isHovering = false;
    }

    private float shadowTime = 0.1f;
    private IEnumerator ShadowAppear()
    {
        float elapsed = 0f;

        // 시작 알파(0 = 투명), 목표 알파(1 = 불투명)
        float startAlpha = 0f;
        float targetAlpha = Utils.SHADOW_ALPHA; // 약간 투명하게

        Color color = shadow.GetComponent<SpriteRenderer>().color;

        while (elapsed < shadowTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shadowTime;

            // 알파 보간
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            shadow.GetComponent<SpriteRenderer>().color = color;

            yield return null;
        }

        // 루프가 끝난 뒤 최종 알파 보정
        color.a = targetAlpha;
        shadow.GetComponent<SpriteRenderer>().color = color;
    }
    private IEnumerator ShadowDisappear()
    {
        float elapsed = 0f;

        // 시작 알파(0 = 투명), 목표 알파(1 = 불투명)
        float startAlpha = Utils.SHADOW_ALPHA;
        float targetAlpha = 0f; // 약간 투명하게

        Color color = shadow.GetComponent<SpriteRenderer>().color;

        while (elapsed < shadowTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shadowTime;

            // 알파 보간
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            shadow.GetComponent<SpriteRenderer>().color = color;

            yield return null;
        }

        // 루프가 끝난 뒤 최종 알파 보정
        color.a = targetAlpha;
        shadow.GetComponent<SpriteRenderer>().color = color;
    }
    #endregion


}
