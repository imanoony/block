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
        instanceScript.Initialize(property);

        // 드래그 상태 시작
        instanceScript.BeginDrag();
    }
}
