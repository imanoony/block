using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public void EnableGridHover(int x, int y)
    {
        Debug.Log("[EnableGridHover] grid가 클릭됨");

        if (GameManager.Instance.Grid.Grids[x, y].Type != GridType.Null)
        {
            Debug.Log(GameManager.Instance.Grid.GetGridExpr(x, y));
        }
        else
        {
            Debug.Log(GameManager.Instance.Grid.GetGridCacheExpr(x, y));
        }
    }
    public void DisableGridHover(PointerEventData data)
    {
        
    }
}