using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridInstance : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    private Grid gridData;
    private GameManager gm;

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private SpriteRenderer animSr;
    [SerializeField] private int animFrameCnt = 37;
    private MaterialPropertyBlock mpb;
    private MaterialPropertyBlock animMpb;

    public void Initialize(int x, int y)
    {
        this.x = x; this.y = y;
        gridData = GameManager.Instance.Grid.Grids[x, y];
        gridData.OnPortsChanged += OnPortsChanged;
        gm = GameManager.Instance;

        SubscribePort();
        mpb = new();
        animMpb = new();
        mpb.SetInteger("_IsOff", 1);
        animMpb.SetInteger("_IsOff", 0);
        sr.SetPropertyBlock(mpb);
        animSr.SetPropertyBlock(animMpb);
        
        if (gridData.Type == GridType.Input) SetColor(gridData.Expr, Utils.CodeToColor(Utils.BLUE));
        else if (gridData.Type == GridType.Output) SetColor(gridData.Expr, Utils.CodeToColor(Utils.GRAY));

    }

    //void OnMouseDown()
    //{
    //    if (gm.UI.IsChatEnabled(new(x, y))) gm.UI.DisableChat(x, y);
    //    else gm.UI.EnableChat(x, y);
    //}

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
        if (gridData.Type == GridType.Input) return;
        else if (gridData.Type == GridType.Output)
        {
            // Output에 연결된 논리식이 있는 포트가 하나 이상일 때
            if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null)
            {
                if (gridData.Ports[0].Cache.Equals(gridData.Expr))
                {
                    GameManager.Instance.OutputCheck(new(x, y), true);
                    SetColor(gridData.Expr, Utils.CodeToColor(Utils.BLUE));
                    //GameManager.Instance.UI.EnableChat(x, y);
                }
                else
                {
                    GameManager.Instance.OutputCheck(new(x, y), false);
                    SetColor(gridData.Expr, Utils.CodeToColor(Utils.RED));
                }
            }
            // Output에 연결된 포트가 없거나,
            // 연결된 포트에 흐르는 논리식이 없을 때
            else
            {
                GameManager.Instance.OutputCheck(new(x, y), false);
                SetColor(gridData.Expr, Utils.CodeToColor(Utils.GRAY));
            }
        }
        else if (gridData.Ports.Count > 0 && gridData.Ports[0].Cache != null) {
            SetColor(gridData.Ports[0].Cache, Utils.CodeToColor(Utils.BLUE));
            //GameManager.Instance.UI.EnableChat(x, y);
        }
        else
        {
            SetColor(null, Utils.CodeToColor(Utils.CLEAR));
            //if (gm.UI.IsChatEnabled(new(x, y))) gm.UI.DisableChat(x, y);
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
        animSr.gameObject.SetActive(true);

        Vector4 v = Logic2Vector4(expr);
        animMpb.SetVector("_CombExpr", v);
        animSr.SetPropertyBlock(animMpb);
        animSr.color = color;

        for (int i = 0; i < animFrameCnt; i++)
        {
            mpb.SetInteger("_MaskIndex", i);
            animMpb.SetInteger("_MaskIndex", i);
            sr.SetPropertyBlock(mpb);
            animSr.SetPropertyBlock(animMpb);

            yield return new WaitForSeconds(0.01f);
        }

        mpb.SetVector("_CombExpr", v);
        mpb.SetInteger("_MaskIndex", 0);
        animMpb.SetInteger("_MaskIndex", 0);
        sr.SetPropertyBlock(mpb);
        sr.color = color;
        animSr.SetPropertyBlock(animMpb);

        animSr.gameObject.SetActive(false);
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