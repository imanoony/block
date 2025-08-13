using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PortPositioner : MonoBehaviour
{
    [SerializeField] private GameObject portPrefab;
    private List<GameObject> portInstances = new List<GameObject>();
    private BlockData blockData;
    private float tileSize = 0.16f;

    public void PositionPorts(BlockData bd)
    {
        blockData = bd;
        if (blockData == null || blockData.Port == null || blockData.Port.Count == 0)
        {
            Debug.LogWarning("포트 데이터가 없습니다.");
            return;
        }

        // shape = 블록의 타일 형태 좌표 집합
        HashSet<Vector2Int> shape = new(blockData.Tile);

        float offsetY = blockData.Size.y * tileSize / 2f;
        float offsetX = blockData.Size.x * tileSize / 2f;

        for (int i = 0; i < blockData.Port.Count; i++)
        {
            Vector2Int gridPos = blockData.Grid[i];
            LogicExpr expr = blockData.Port[i];

            // 포트 텍스트 생성 or 가져오기
            GameObject port = Instantiate(portPrefab, transform);
            port.name = $"Port_{gridPos.x}_{gridPos.y}";
            TextMeshPro text = port.GetComponent<TextMeshPro>();
            text.text = expr != null ? expr.ToString() : "";
            portInstances.Add(port);

            // 1. Base Position 
            Vector2 localPos = new Vector2(
                gridPos.y * tileSize - offsetY,
                -gridPos.x * tileSize + offsetX
            );

            // 2. 경계 판정 → offset 적용
            if (!shape.Contains(new Vector2Int(gridPos.x, gridPos.y)) && !shape.Contains(new Vector2Int(gridPos.x, gridPos.y - 1)))
            {
                localPos.y += Utils.PORT_OFFSET / Utils.DENOMINATOR; // 하단 경계
                text.rectTransform.pivot = new Vector2(text.rectTransform.pivot.x, 0);
                text.margin = new Vector4(text.margin.x, text.margin.w, text.margin.z, text.margin.y);
            }
            else if (!shape.Contains(new Vector2Int(gridPos.x - 1, gridPos.y)) && !shape.Contains(new Vector2Int(gridPos.x - 1, gridPos.y - 1)))
            {
                localPos.y -= Utils.PORT_OFFSET / Utils.DENOMINATOR; // 상단 경계
                text.rectTransform.pivot = new Vector2(text.rectTransform.pivot.x, 1);
            }
            if (!shape.Contains(new Vector2Int(gridPos.x, gridPos.y)) && !shape.Contains(new Vector2Int(gridPos.x - 1, gridPos.y)))
            {
                localPos.x -= Utils.PORT_OFFSET / Utils.DENOMINATOR; // 우측 경계
                text.rectTransform.pivot = new Vector2(1, text.rectTransform.pivot.y);
                text.alignment = TextAlignmentOptions.Right;
                text.margin = new Vector4(text.margin.z, text.margin.y, text.margin.x, text.margin.w);
            }
            else if (!shape.Contains(new Vector2Int(gridPos.x, gridPos.y - 1)) && !shape.Contains(new Vector2Int(gridPos.x - 1, gridPos.y - 1)))
            {
                localPos.x += Utils.PORT_OFFSET / Utils.DENOMINATOR; // 좌측 경계
                text.rectTransform.pivot = new Vector2(0, text.rectTransform.pivot.y);
            }


            // 3. 회전 적용
            localPos = RotatePoint(localPos, blockData.Rotation);

            // 4. Flip 보정
            if (blockData.IsFlipped)
            {
                localPos.x = -localPos.x;
                text.rectTransform.localRotation = Quaternion.Euler(0, 0, -(int)blockData.Rotation);
            }
            else
            {
                text.rectTransform.localRotation = Quaternion.Euler(0, 0, -(int)blockData.Rotation);
            }

            // 5. 최종 적용
            text.rectTransform.localPosition = localPos;
        }
    }

    private Vector2 RotatePoint(Vector2 point, Rotate angle)
    {
        float rad = (int)angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            point.x * cos - point.y * sin,
            point.x * sin + point.y * cos
        );
    }

    public void UpdateText(BlockData bd)
    {
        for (int i = 0; i < portInstances.Count; i++)
        {
            TextMeshPro text = portInstances[i].GetComponent<TextMeshPro>();
            LogicExpr expr = bd.Port[i];
            text.text = expr != null ? expr.ToString() : "";
        }
    }
}
