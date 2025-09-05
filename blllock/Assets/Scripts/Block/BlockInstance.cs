using Unity.VisualScripting;
using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    private BlockData blockData;
    private bool isDragging = false;
    private GridManager gm = GameManager.Instance.Grid;

    public bool CanRotate { get; private set; } = false;
    public bool CanFlip { get; private set; } = false;
    public void Initialize(BlockData blockData, bool canRotate = false, bool canFlip = false)
    {
        if (blockData == null) { Utils.PrintError("BlockData는 Null일 수 없음."); return; }
        this.blockData = blockData;

        this.blockData.Instantiate();
        GetComponent<SpriteRenderer>().sprite = this.blockData.BlockSprite;
        CanRotate = canRotate;
        CanFlip = canFlip;
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
            transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    private bool isPlaced = false;
    private Vector2Int baseTile = new(-1, -1);
    public bool Valid = true;

    // Invalid Position이면 아예 둘 수 없다.
    // Invalid Ports면 둘 수는 있으나 Block Instance의 Valid가 false가 된다.
    // Valid가 false인 block들은 타일에 변화가 있을 때마다 
    private bool Place(GridManager gm, Vector2Int baseTile)
    {
        if (isPlaced || !baseTile.Equals(new(-1, -1))) return false;
        if (!gm.IsValidPos(blockData, baseTile)) return false;

        isPlaced = true;
        this.baseTile = baseTile;

        if (!gm.PlaceBlock(blockData, baseTile))
        {
            Valid = false;
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

        Valid = true;
        gm.RemoveInvalid(this);
    }

    public void Check(GridManager gm)
    {
        if (!isPlaced || baseTile.Equals(new(-1, -1)) || Valid) return;
        if (!gm.PlaceBlock(blockData, baseTile)) return;

        Valid = true;
        gm.RemoveInvalid(this);
    }

    private Vector3 GetBaseTilePos() => transform.position + new Vector3(-blockData.Width / 2f, blockData.Height / 2f, 0);
}
