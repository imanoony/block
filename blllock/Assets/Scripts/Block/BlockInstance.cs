using UnityEngine;

public class BlockInstance : MonoBehaviour
{
    private BlockData blockData;
    private bool isDragging = false;

    public void Initialize(BlockData blockData)
    {
        if (blockData == null) { Utils.PrintError("BlockData는 Null일 수 없음."); return; }
        this.blockData = blockData;
        GetComponent<SpriteRenderer>().sprite = this.blockData.BlockSprite;
    }

    public void BeginDrag()
    {
        isDragging = true;
        // 드래그 시작 시 다른 UI 이벤트 차단용 처리도 가능
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
            //EndDrag();
        }
    }

    // 여기에 이제 회전이랑 반전 I/O도 추가한다
    /*
    private void EndDrag()
    {
        isDragging = false;

        int blockHeight = property.Size.x;
        int blockWidth = property.Size.y;
        float unit = Utils.TILE_SPACING / Utils.DENOMINATOR;

        Vector2Int? snapPos = GridManager.GetNearestGridPosition(
            transform.position - new Vector3(blockWidth / 2f * unit, (-1) * blockHeight / 2f * unit, 0));

        if (snapPos != null && GridManager.PlaceBlock(property, (Vector2Int)snapPos, property.Tile))
        {
            Vector3 topLeftPos = GridManager.GridToWorld(snapPos.Value);
            transform.position = topLeftPos + new Vector3(blockWidth / 2f * unit, (-1) * blockHeight / 2f * unit, 0);

            //portPositioner.UpdateText(property); // 포트 텍스트 업데이트
        }
        else
        {
            Debug.LogWarning($"블록을 배치할 수 없습니다. 위치: {snapPos?.x}, {snapPos?.y}");
            Destroy(gameObject);
        }
    }

    private void SlideBlock(Vector3 arrival)
    {
        
    }*/

}
