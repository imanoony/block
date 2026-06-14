using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridInstance : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_01 = new WaitForSeconds(0.01f);

    public int x { get; private set; }
    public int y { get; private set; }
    private Grid gridData;
    private GameManager gm;

    // Grid In
    [SerializeField] private SpriteRenderer gridInSr;
    [SerializeField] private SpriteRenderer gridInAnimOffSr;
    [SerializeField] private SpriteRenderer gridInAnimOnSr;
    private MaterialPropertyBlock gridInMpb;
    private MaterialPropertyBlock gridInAnimOffMpb;
    private MaterialPropertyBlock gridInAnimOnMpb;

    // Grid Out
    [SerializeField] private SpriteRenderer gridOutSr;
    [SerializeField] private SpriteRenderer gridOutAnimOffSr;
    [SerializeField] private SpriteRenderer gridOutAnimOnSr;
    private MaterialPropertyBlock gridOutMpb;
    private MaterialPropertyBlock gridOutAnimOffMpb;
    private MaterialPropertyBlock gridOutAnimOnMpb;

    public void Initialize(int x, int y)
    {
        this.x = x; this.y = y;
        gridData = GameManager.Instance.Grid.Grids[x, y];
        gridData.OnPortsChanged += OnPortsChanged;
        gm = GameManager.Instance;

        SubscribePort();
        //UpdateColor();
        if (gridData.Type == GridType.Input) SetColor(gridData.Expr, Utils.CodeToColor(Utils.BLUE));
        else if (gridData.Type == GridType.Output) SetColor(gridData.Expr, Utils.CodeToColor(Utils.GRAY));
    }

    void OnMouseDown()
    {
        if (gm.UI.IsChatEnabled(new(x, y))) gm.UI.DisableChat(x, y);
        else gm.UI.EnableChat(x, y);
    }

    private readonly HashSet<PortExpr> subscribedPorts = new(); // 이 GridInstance가 구독 중인 포트들
    private void SubscribePort()
    {
        foreach (PortExpr port in gridData.Ports)
        {
            port.OnCacheChanged += OnPortCacheChanged;
            subscribedPorts.Add(port);
        }
    }
    private void UnsubscribePort()
    {
        foreach (PortExpr port in subscribedPorts)
        {
            port.OnCacheChanged -= OnPortCacheChanged;
        }
        subscribedPorts.Clear();
    }

    private void OnPortsChanged()
    {
        UnsubscribePort();
        SubscribePort();

        UpdateColor();
    }

    private void OnPortCacheChanged(PortExpr _) => UpdateColor();

    private void UpdateColor()
    {
        if (this == null)
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
                    SetColor(gridData.Expr, Utils.CodeToColor(Utils.BLUE));
                    GameManager.Instance.UI.EnableChat(x, y);
                }
                else
                {
                    GameManager.Instance.OutputCheck(new(x, y), false);
                    SetColor(gridData.Expr, Utils.CodeToColor(Utils.RED));
                }
            }
            else
            {
                GameManager.Instance.OutputCheck(new(x, y), false);
                SetColor(gridData.Expr, Utils.CodeToColor(Utils.GRAY));
            }
        }
        else if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null) {
            SetColor(gridData.Ports[0].Cache, Utils.CodeToColor(Utils.BLUE));
            GameManager.Instance.UI.EnableChat(x, y);
        }
        else
        {
            SetColor(null, Utils.CodeToColor(Utils.CLEAR));
            if (gm.UI.IsChatEnabled(new(x, y))) gm.UI.DisableChat(x, y);
        }
    }

    private Coroutine colorCoroutine = null;
    private void SetColor(LogicExpr expr, Color color)
    {
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(SetColorCo(expr, color));
    }

    private IEnumerator SetColorCo(LogicExpr expr, Color color)
    {
        gridInMpb = new();
        gridInAnimOffMpb = new();
        gridInAnimOnMpb = new();
        gridOutMpb = new();
        gridOutAnimOffMpb = new();
        gridOutAnimOnMpb = new();

        
        gridOutAnimOffSr.gameObject.SetActive(true);
        gridOutAnimOnSr.gameObject.SetActive(true);

        Vector4 v = Logic2Vector4(expr);
        gridOutAnimOnMpb.SetVector("_CombExpr", v);
        gridOutAnimOnSr.SetPropertyBlock(gridOutAnimOnMpb);

        gridOutAnimOnSr.color = color;

        for (int i = 0; i < 37; i++)
        {
            gridOutAnimOffMpb.SetFloat("_AnimIndex", i);
            gridOutAnimOnMpb.SetFloat("_MaskIndex", i);
            
            gridOutAnimOffSr.SetPropertyBlock(gridOutAnimOffMpb);
            gridOutAnimOnSr.SetPropertyBlock(gridOutAnimOnMpb);

            yield return _waitForSeconds0_01;
        }
        
        gridInAnimOnMpb.SetVector("_CombExpr", v);
        gridInAnimOnSr.SetPropertyBlock(gridInAnimOnMpb);
        gridInAnimOffSr.gameObject.SetActive(true);
        gridInAnimOnSr.gameObject.SetActive(true);
        gridInAnimOnSr.color = color;

        for (int i = 0; i < 6; i++)
        {
            gridInAnimOffMpb.SetFloat("_AnimIndex", i);
            gridInAnimOnMpb.SetFloat("_MaskIndex", i);

            gridInAnimOffSr.SetPropertyBlock(gridInAnimOffMpb);
            gridInAnimOnSr.SetPropertyBlock(gridInAnimOnMpb);

            yield return new WaitForSeconds(0.03f);
        }

        gridInMpb.SetVector("_CombExpr", v);
        gridInMpb.SetFloat("_MaskIndex", 5);
        gridOutMpb.SetVector("_CombExpr", v);
        gridOutMpb.SetFloat("_MaskIndex", 36);
        gridInSr.SetPropertyBlock(gridInMpb);
        gridOutSr.SetPropertyBlock(gridOutMpb);
        gridInSr.color = color;
        gridOutSr.color = color;

        gridInAnimOffSr.gameObject.SetActive(false);
        gridInAnimOnSr.gameObject.SetActive(false);
        gridOutAnimOffSr.gameObject.SetActive(false);
        gridOutAnimOnSr.gameObject.SetActive(false);
    }

    private Vector4 Logic2Vector4(LogicExpr logic=null)
    {
        if (logic == null) return new(0, 0, 0, 0);
        else if (logic is VarExpr var)
        {
            int i = Var2Int(var);
            return new(i, i, i, i);
        }
        else if (logic is CombExpr comb)
        {
            int lu = Var2Int(comb.LeftUp);
            int ld = Var2Int(comb.LeftDown);
            int ru = Var2Int(comb.RightUp);
            int rd = Var2Int(comb.RightDown);
            return new(lu, ld, ru, rd);
        }
        else return new(0, 0, 0, 0);
    }

    private int Var2Int(VarExpr var=null)
    {
        if (var == null) return 0;
        else if (var.Name == "X") return 1;
        else if (var.Name == "Y") return 2;
        else if (var.Name == "Z") return 3;
        else return 0;
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