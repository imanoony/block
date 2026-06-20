using System;
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
                }
                else
                {
                    GameManager.Instance.OutputCheck(new(x, y), false);

                    (CombExpr comb, List<Color> colors) = CompareOutput(expr, gridData.Expr);
                    SetColor(comb, colors);
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

    private (CombExpr, List<Color>) CompareOutput(LogicExpr expr, LogicExpr output)
    {
        CombExpr combExpr = expr.ToCombExpr();
        CombExpr combOutput = output.ToCombExpr();

        (VarExpr luExpr, Color luColor) = CompareOutput(combExpr.LeftUp, combOutput.LeftUp);
        (VarExpr ldExpr, Color ldColor) = CompareOutput(combExpr.LeftDown, combOutput.LeftDown);
        (VarExpr ruExpr, Color ruColor) = CompareOutput(combExpr.RightUp, combOutput.RightUp);
        (VarExpr rdExpr, Color rdColor) = CompareOutput(combExpr.RightDown, combOutput.RightDown);

        return (new CombExpr(luExpr, ldExpr, ruExpr, rdExpr), new List<Color>(){luColor, ldColor, ruColor, rdColor});
    }
    private (VarExpr, Color) CompareOutput(VarExpr expr, VarExpr output)
    {
        if (expr == null && output == null) return (null, Utils.CodeToColor(Utils.CLEAR));
        else if (expr == null) return (output, Utils.CodeToColor(Utils.GRAY));
        else if (output == null) return (expr, Utils.CodeToColor(Utils.RED));
        else if (expr.Equals(output)) return (output, Utils.CodeToColor(Utils.BLUE));
        else return (output, Utils.CodeToColor(Utils.RED));
    }

    private const string CombExpr = "_CombExpr", Colors = "_Colors", MaskIndex = "_MaskIndex";
    private Coroutine colorCoroutine = null;
    private void SetColor(LogicExpr expr, Color color)
    {
        SetColor(expr, new List<Color>(){color, color, color, color});
    }
    private void SetColor(LogicExpr expr, List<Color> colors)
    {
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);

        Vector4 v = Logic2Vector4(expr);
        Matrix4x4 m = new(
            (Vector4)colors[0].linear,
            (Vector4)colors[1].linear,
            (Vector4)colors[2].linear,
            (Vector4)colors[3].linear
        );
        m = m.transpose;

        animSr.gameObject.SetActive(true);
        animMpb.SetVector(CombExpr, v);
        animMpb.SetMatrix(Colors, m);
        animSr.SetPropertyBlock(animMpb);

        colorCoroutine = StartCoroutine(SetColorCo(
            () =>
            {
                mpb.SetVector(CombExpr, v);
                mpb.SetMatrix(Colors, m);
                mpb.SetInteger(MaskIndex, 0);
                sr.SetPropertyBlock(mpb);

                animMpb.SetInteger(MaskIndex, 0);
                animSr.SetPropertyBlock(animMpb);
                animSr.gameObject.SetActive(false);
            }
        ));
    }

    private IEnumerator SetColorCo(Action onComplete = null)
    {
        for (int i = 0; i < animFrameCnt; i++)
        {
            mpb.SetInteger(MaskIndex, i);
            animMpb.SetInteger(MaskIndex, i);
            sr.SetPropertyBlock(mpb);
            animSr.SetPropertyBlock(animMpb);

            yield return new WaitForSeconds(0.01f);
        }

        onComplete?.Invoke();
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