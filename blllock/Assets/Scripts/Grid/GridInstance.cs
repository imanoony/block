using UnityEngine;

public class GridInstance : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }

    public void Initialize(int x, int y)
    {
        this.x = x; this.y = y;

        GridType type = GameManager.Instance.Grid.GetGridType(x, y);
        if (type == GridType.Input)
            gameObject.GetComponent<SpriteRenderer>().color = Color.red;
        else if (type == GridType.Output)
            gameObject.GetComponent<SpriteRenderer>().color = Color.blue;
    }
    void OnMouseDown()
    {
        GameManager.Instance.UI.EnableGridHover(x, y);
    }
}