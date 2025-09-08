using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    private BlockData blockData;
    private bool isDragging = false;
    private GridManager gm;
    private SpriteRenderer sr;
    private Color color;

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
        else if (CanFlip) color = Utils.CodeToColor(Utils.ORANGE);
        else color = Color.white;

        gm = GameManager.Instance.Grid;
        sr = gameObject.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        gameObject.GetComponent<BoxCollider2D>().size = sr.sprite.bounds.size;
    }

    public void BeginDrag()
    {
        isDragging = true;

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
        if (isDragging)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = GetClampedPos(new Vector3(mousePos.x, mousePos.y, 0));
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);

            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (CanRotate) Rotate(isPlaced);
                else if (CanFlip) Flip();
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

        return clamped;
    }


    private bool isPlaced = false;
    private Vector2Int baseTile = new(-1, -1);
    public bool Valid = true;

    // Invalid Position이면 아예 둘 수 없다.
    // Invalid Ports면 둘 수는 있으나 Block Instance의 Valid가 false가 된다.
    // Valid가 false인 block들은 타일에 변화가 있을 때마다 
    private bool Place(GridManager gm, Vector2Int baseTile)
    {
        if (isPlaced || !this.baseTile.Equals(new(-1, -1))) return false;
        if (!gm.IsValidPos(blockData, baseTile)) { Debug.Log("Invalid Pos"); return false; }

        isPlaced = true;
        this.baseTile = baseTile;

        // 블록의 위치를 snap position (좌표) 에 동기화
        transform.position = gm.GetBlockCenterOnTile(baseTile.x, baseTile.y, blockData.Height, blockData.Width);
        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        if (!gm.PlaceBlock(blockData, baseTile))
        {
            Valid = false;
            sr.color = Utils.CodeToColor(Utils.RED);
            gm.AddInvalid(this);
        }
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
    }

    public void Check(GridManager gm)
    {
        if (!isPlaced || baseTile.Equals(new(-1, -1)) || Valid) return;
        if (!gm.PlaceBlock(blockData, baseTile)) return;

        Valid = true;
        sr.color = color;
        gm.RemoveInvalid(this);
    }

    private Vector3 GetBaseTilePos() => transform.position + new Vector3(-blockData.Width / 2f, blockData.Height / 2f, 0);

    #region Rotate & Flip
    private void Rotate(bool isPlaced)
    {
        if (!CanRotate) return;

        Vector2Int baseTile = this.baseTile;
        if (isPlaced) Unplace(gm);

        blockData.Rotation();
        Vector3 euler = transform.rotation.eulerAngles;
        euler.z = -(int)blockData.BlockRotate;  // 원하는 Z축 회전
        transform.rotation = Quaternion.Euler(euler);

        if (isPlaced) Place(gm, baseTile);
    }
    private void Flip()
    {
        if (!CanFlip) return;

        Vector2Int baseTile = this.baseTile;
        if (isPlaced) Unplace(gm);

        blockData.FlipX();
        sr.flipX = blockData.BlockFlipX;

        if (isPlaced) Place(gm, baseTile);
    }
    #endregion
}
