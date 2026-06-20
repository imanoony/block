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

        SubscribeWires();
        mpb = new();
        animMpb = new();
        mpb.SetInteger("_IsOff", 1);
        animMpb.SetInteger("_IsOff", 0);
        sr.SetPropertyBlock(mpb);
        animSr.SetPropertyBlock(animMpb);
        
        if (gridData.Type == GridType.Input) SetColor(gridData.Expr, Utils.CodeToColor(Utils.BLUE));
        else if (gridData.Type == GridType.Output) SetColor(gridData.Expr, Utils.CodeToColor(Utils.GRAY));

    }

    private Wire leftUpRef = null; 
    private Wire leftDownRef = null; 
    private Wire rightUpRef = null; 
    private Wire rightDownRef = null;
    private void SubscribeWires()
    {
        Wire lu, ld, ru, rd;
        for (int i = 0; i < gridData.Ports.Count; i++)
        {
            lu = gridData.WiresLeftUp[i];
            ld = gridData.WiresLeftDown[i];
            ru = gridData.WiresRightUp[i];
            rd = gridData.WiresRightDown[i];

            if (leftUpRef == null && lu != null)
            {
                leftUpRef = lu;
                leftUpRef.OnCacheChanged += OnWireCacheChanged;
            }
            if (leftDownRef == null && ld != null)
            {
                leftDownRef = ld;
                leftDownRef.OnCacheChanged += OnWireCacheChanged;
            }
            if (rightUpRef == null && ru != null)
            {
                rightUpRef = ru;
                rightUpRef.OnCacheChanged += OnWireCacheChanged;
            }
            if (rightDownRef == null && rd != null)
            {
                rightDownRef = rd;
                rightDownRef.OnCacheChanged += OnWireCacheChanged;
            }
        }
    }
    private void UnsubscribeWires()
    {
        if (leftUpRef != null) leftUpRef.OnCacheChanged -= OnWireCacheChanged;
        if (leftDownRef != null) leftDownRef.OnCacheChanged -= OnWireCacheChanged;
        if (rightUpRef != null) rightUpRef.OnCacheChanged -= OnWireCacheChanged;
        if (rightDownRef != null) rightDownRef.OnCacheChanged -= OnWireCacheChanged;

        leftUpRef = null;
        leftDownRef = null;
        rightUpRef = null;
        rightDownRef = null;
    }

    private LogicExpr Wires2Logic()
    {
        CombExpr comb = new(
            leftUpRef?.Cache,
            leftDownRef?.Cache,
            rightUpRef?.Cache,
            rightDownRef?.Cache
        );
        return comb.Clean();
    }

    private void OnPortsChanged()
    {
        UnsubscribeWires();
        SubscribeWires();

        UpdateColor();
    }

    private void OnWireCacheChanged(Wire _) => UpdateColor();

    private void UpdateColor()
    {
        LogicExpr expr;

        if (gridData.Type == GridType.Input) return;
        else if (gridData.Type == GridType.Output)
        {
            // Output에 연결된 논리식이 있는 포트가 하나 이상일 때
            if ((expr = Wires2Logic()) != null)
            {
                if (expr.Equals(gridData.Expr))
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
        else if ((expr = Wires2Logic()) != null) {
            SetColor(expr, Utils.CodeToColor(Utils.BLUE));
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
        animMpb.SetColor("_Color", color);
        animSr.SetPropertyBlock(animMpb);

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
        mpb.SetColor("_Color", color);
        animMpb.SetInteger("_MaskIndex", 0);
        sr.SetPropertyBlock(mpb);
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
        UnsubscribeWires();
    }

    private void OnDisable()
    {
        if (gridData == null) return;

        // 모든 이벤트 해제
        gridData.OnPortsChanged -= OnPortsChanged;
        UnsubscribeWires();
    }

}