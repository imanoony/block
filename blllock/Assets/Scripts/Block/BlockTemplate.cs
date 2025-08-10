using UnityEngine;

public class BlockTemplate : MonoBehaviour
{
    public BlockData property;
    public GameObject blockInstancePrefab;

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
