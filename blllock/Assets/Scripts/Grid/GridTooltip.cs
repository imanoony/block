using UnityEngine;

public class GridTooltip : MonoBehaviour
{
    private float gridIdleTime = 0f;
    private bool tooltipOn = false;
    private bool isEnabled = false;
    private Vector2Int pos;

    public void Initialize(Vector2Int pos)
    {
        gridIdleTime = 0f;
        this.pos = pos;
    }

    void Update()
    {
        if (!isEnabled) return;
        if (GameManager.Instance.State != GameState.InGame) return;

        gridIdleTime += Time.deltaTime;
        if (gridIdleTime >= Utils.GRID_IDLE && !tooltipOn)
        {
            GameManager.Instance.UI.GridTooltipAppear(pos);
            tooltipOn = true;
        }
    }

    public void ResetGridIdleTime()
    {
        gridIdleTime = 0f;
        if (tooltipOn) GameManager.Instance.UI.GridTooltipDisappear();
        tooltipOn = false;
    }

    public void StartCheck() => isEnabled = true;
    public void StopCheck() => isEnabled = false;
}