using Unity.VisualScripting;
using UnityEngine;

public class GridInstance : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    private Grid gridData;
    private SpriteRenderer sr;

    public void Initialize(int x, int y)
    {
        this.x = x; this.y = y;
        gridData = GameManager.Instance.Grid.Grids[x, y];
        gridData.OnPortsChanged += OnPortsChanged;
        sr = gameObject.GetComponent<SpriteRenderer>();

        SubscribePort();
        UpdateColor();
    }
    void OnMouseDown()
    {
        GameManager.Instance.UI.EnableGridHover(x, y);
    }

    private void SubscribePort()
    {
        foreach (WireExpr port in gridData.Ports)
            port.OnCacheChanged += OnPortCacheChanged;
    }
    private void UnsubscribePort()
    {
        foreach (WireExpr port in gridData.Ports)
            port.OnCacheChanged -= OnPortCacheChanged;
    }

    private void OnPortsChanged()
    {
        UnsubscribePort();
        SubscribePort();

        UpdateColor();
    }

    private void OnPortCacheChanged(WireExpr _) => UpdateColor();

    private void UpdateColor()
    {
        if (gridData.Type == GridType.Input) sr.color = Utils.CodeToColor(Utils.RED);
        else if (gridData.Type == GridType.Output) sr.color = Utils.CodeToColor(Utils.BLUE);
        else if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null) sr.color = Utils.CodeToColor(Utils.GRAY);
        else sr.color = Color.clear;
    }

    private void Oestroy()
    {
        gridData.OnPortsChanged -= OnPortsChanged;
        UnsubscribePort();
    }
}