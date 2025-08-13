using System.Collections.Generic;
using UnityEngine;

public class BlockTemplate : MonoBehaviour
{
    public BlockData property;
    public GameObject blockInstancePrefab;

    // 일단 임시로 Start 시 port 표시되게 함
    void Start()
    {
        property.Init();
        PortPositioner portPositioner = GetComponent<PortPositioner>();
        portPositioner.PositionPorts(property);
    }

    private void OnMouseDown()
    {
        // 드래그 시작 시 자식 인스턴스 생성
        CreateInstance();
    }

    private void CreateInstance()
    {
        var instanceObj = Instantiate(blockInstancePrefab);
        var instanceScript = instanceObj.GetComponent<BlockInstance>();
        BlockData blockData = new BlockData
        {
            ID = property.ID,
            Size = property.Size,
            Tile = new List<Vector2Int>(property.Tile),
            Grid = new List<Vector2Int>(property.Grid),
            Port = new List<LogicExpr>(property.Port),
            Sprite = property.Sprite,
            IsFlipped = property.IsFlipped,
            Rotation = property.Rotation,
            portExprs = new List<PortExpr>(property.portExprs),
        };
        blockData.Init(); // 초기화
        instanceScript.Initialize(blockData);

        // 드래그 상태 시작
        instanceScript.BeginDrag();
    }
}
