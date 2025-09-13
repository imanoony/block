using System.Collections;
using UnityEngine;

public class GridInstance : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    private Grid gridData;
    private SpriteRenderer sr;
    private GameManager gm;
    private GameObject anim;
    private Animator animator;
    private string animStr = "GridAnim";

    public void Initialize(int x, int y)
    {
        this.x = x; this.y = y;
        gridData = GameManager.Instance.Grid.Grids[x, y];
        gridData.OnPortsChanged += OnPortsChanged;
        sr = gameObject.GetComponent<SpriteRenderer>();
        gm = GameManager.Instance;
        anim = gameObject.transform.GetChild(0).gameObject;
        animator = anim.GetComponent<Animator>();

        SubscribePort();
        //UpdateColor();

        sr.color = Color.clear;
        anim.SetActive(false);
        if (gridData.Type == GridType.Input) SetColor(Utils.CodeToColor(Utils.BLUE));
        else if (gridData.Type == GridType.Output) SetColor(Utils.CodeToColor(Utils.GRAY));
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

        if (gridData.Type == GridType.Input) return;
        else if (gridData.Type == GridType.Output)
        {
            if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null)
            {
                if (gridData.Ports[0].Cache.Equals(gridData.Expr))
                {
                    GameManager.Instance.OutputCheck(new(x, y), true);
                    SetColor(Utils.CodeToColor(Utils.BLUE));
                }
                else
                {
                    GameManager.Instance.OutputCheck(new(x, y), false);
                    SetColor(Utils.CodeToColor(Utils.RED));
                }
            }
            else SetColor(Utils.CodeToColor(Utils.GRAY));
        }
        else if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null) SetColor(Utils.CodeToColor(Utils.BLUE));
        else
        {
            SetColor(Color.clear);
            if (gm.UI.ChatEnablePos.ContainsKey(new(x, y))) gm.UI.DisableGridHover(x, y);
        }
    }

    private Coroutine colorCoroutine = null;
    private void SetColor(Color color)
    {
        if (sr.color == color && !anim.activeSelf) return;
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);

        if (color != Color.clear) colorCoroutine = StartCoroutine(SetColorCoroutine(color));
        else { sr.color = Color.clear; anim.SetActive(false); }
    }

    private IEnumerator SetColorCoroutine(Color color)
    {
        Debug.Log($"Color Coroutine | color = {color}");

        anim.SetActive(false);

        anim.SetActive(true);
        anim.GetComponent<SpriteRenderer>().color = color;
        animator.Play(animStr);

        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(clipLength);

        sr.color = color;
        anim.SetActive(false);
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