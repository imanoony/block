using UnityEngine;

public class GridInstance : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    private Grid gridData;
    private SpriteRenderer sr;
    private GameManager gm;

    public void Initialize(int x, int y)
    {
        this.x = x; this.y = y;
        gridData = GameManager.Instance.Grid.Grids[x, y];
        gridData.OnPortsChanged += OnPortsChanged;
        sr = gameObject.GetComponent<SpriteRenderer>();
        gm = GameManager.Instance;

        SubscribePort();
        UpdateColor();
    }

    void OnMouseDown()
    {
        if (gm.UI.ChatEnablePos.ContainsKey(new(x, y))) gm.UI.DisableGridHover(x, y);
        else gm.UI.EnableGridHover(x, y);
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
        if (this == null || sr == null)
        {
            Utils.PrintWarning("GridInstance가 파괴되었거나 SpriteRenderer가 없음.");
            return;
        }

        if (gridData.Type == GridType.Input) sr.color = Utils.CodeToColor(Utils.BLUE);
        else if (gridData.Type == GridType.Output)
        {
            sr.color = Utils.CodeToColor(Utils.GRAY);

            if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null)
            {
                if (gridData.Ports[0].Cache.Equals(gridData.Expr))
                {
                    GameManager.Instance.OutputCheck(new(x, y), true);
                    sr.color = Utils.CodeToColor(Utils.BLUE);
                }
                else
                {
                    Utils.PrintWarning($"Output 불일치: [{gridData.Ports[0]}] {gridData.Ports[0].Cache} != {gridData.Expr}");
                    GameManager.Instance.OutputCheck(new(x, y), false);
                }
            }
        }
        else if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null) sr.color = Utils.CodeToColor(Utils.BLUE);
        else
        {
            sr.color = Color.clear;
            if (gm.UI.ChatEnablePos.ContainsKey(new(x, y))) gm.UI.DisableGridHover(x, y);
        }
    }

    private void OnDestroy()
    {
        gridData.OnPortsChanged -= OnPortsChanged;
        UnsubscribePort();
    }

    private void OnDisable()
    {
        if (gridData == null) return;

        // 모든 이벤트 해제
        gridData.OnPortsChanged -= OnPortsChanged;
        UnsubscribePort();
    }

}