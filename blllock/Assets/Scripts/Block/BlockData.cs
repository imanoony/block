using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Rotate
{
    None = 0,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270
}

[System.Serializable]
public class BlockData
{
    public int ID;
    public Vector2Int Size; // (height, width)
    public List<Vector2Int> Tile; // 블록 형태 타일 위치 리스트
    public List<Vector2Int> Grid; // 포트 위치 리스트 (Ports와 인덱스 동기화)

    [SerializeReference, SubclassSelector]
    public List<LogicExpr> Port; // 포트 리스트 (Grid와 인덱스 동기화), 복합식 포함
    public Sprite Sprite; // 블록 스프라이트
    public bool IsFlipped; // 블록이 뒤집혔는지 여부
    public Rotate Rotation; // 블록의 회전 상태

    // Port <-> LogicExpr 매핑을 위한 변수
    public List<PortExpr> portExprs; // 단위 포트 변수만 있는 리스트
    private Dictionary<int, LogicExpr> portDict; // 포트 ID -> LogicExpr 매핑 (런타임 변경 가능)

    public void Init()
    {
        Tile ??= new List<Vector2Int>();
        Grid ??= new List<Vector2Int>();
        Port ??= new List<LogicExpr>();

        portExprs ??= new List<PortExpr>();
        portDict ??= new Dictionary<int, LogicExpr>();

        for (int i = 0; i < Port.Count; i++) InitPortExprsRecursive(Port[i]);
    }

    private void InitPortExprsRecursive(LogicExpr expr)
    {
        if (expr is PortExpr) portExprs.Add((PortExpr)expr);
        else if (expr is NotExpr not)
        {
            InitPortExprsRecursive(not.Inner);
        }
        else if (expr is AndExpr and)
        {
            foreach (var operand in and.Operands)
            {
                InitPortExprsRecursive(operand);
            }
        }
        else if (expr is OrExpr or)
        {
            foreach (var operand in or.Operands)
            {
                InitPortExprsRecursive(operand);
            }
        }
    }

    // 새 포트 매핑 시도 (같은 포트에 기존 매핑과 다르면 모순)
    public bool AddPortMapping(int portIndex, LogicExpr expr)
    {
        List<LogicExpr> backupPort = new List<LogicExpr>(Port); // 백업
        Dictionary<int, LogicExpr> backupDict = new Dictionary<int, LogicExpr>(portDict); // 백업

        if (portIndex < 0 || portIndex >= Port.Count)
        {
            Debug.LogWarning($"Invalid port index: {portIndex}");
            return false;
        }

        // Port 리스트 내 해당 LogicExpr
        var targetExpr = Port[portIndex];

        LogicExpr subst = ReplacePortExprRecursive(targetExpr, expr);
        if (subst == null)
        {
            Debug.LogWarning($"[BlockData] Failed to replace Port {portIndex} with {expr}");
            Port = backupPort; // 백업으로 복원
            portDict = backupDict; // 백업으로 복원
            return false; // 교체 실패
        }
        Port[portIndex] = subst; // 포트 업데이트

        for (int i = 0; i < Port.Count; i++)
        {
            Port[i] = SynchronizePort(Port[i]); // 포트 동기화
        }
        return true;
    }

    private LogicExpr ReplacePortExprRecursive(LogicExpr target, LogicExpr newExpr)
    {
        LogicExpr expr = newExpr;
        if (expr is NotExpr enot && enot.Inner is NotExpr einnerNot) expr = einnerNot.Inner; // 중첩된 NotExpr 제거
        
        if (target is PortExpr port)
            {
                int index = portExprs.IndexOf(port);
                if (portDict.ContainsKey(index))
                {
                    Debug.LogWarning($"[BlockData] PortExpr {port} already exists in portDict (value: {portDict[index]}).");
                    return null; // 이미 존재하는 포트는 교체할 수 없음
                }
                if (expr != null && !ContainsPort(expr)) portDict[index] = expr; // 포트 딕셔너리 업데이트
                return port;
            }
            else if (target is NotExpr not)
            {
                NotExpr newNot = new(expr);
                LogicExpr inner = ReplacePortExprRecursive(not.Inner, newNot);
                return new NotExpr(inner);
            }
            else if (target is AndExpr and)
            {
                if (expr is not AndExpr)
                {
                    Debug.LogWarning($"[BlockData] AndExpr는 AndExpr로만 교체할 수 있습니다: {and} -> {expr}");
                    return null; // AndExpr가 아닌 경우 실패
                }
                AndExpr newAnd = (AndExpr)expr;

                LogicExpr left = ReplacePortExprRecursive(and.Operands[0], newAnd.Operands[0]);
                LogicExpr right = ReplacePortExprRecursive(and.Operands[1], newAnd.Operands[1]);
                if (left != null && right != null) return new AndExpr(new List<LogicExpr> { left, right });
                else
                {
                    Debug.LogWarning($"[BlockData] AndExpr 내부 교체 실패: {and} -> {expr}");
                    return null; // 내부 교체 실패
                }
            }
            else if (target is OrExpr or)
            {
                if (expr is not OrExpr)
                {
                    Debug.LogWarning($"[BlockData] OrExpr는 OrExpr로만 교체할 수 있습니다: {or} -> {expr}");
                    return null; // OrExpr가 아닌 경우 실패
                }
                OrExpr newOr = (OrExpr)expr;

                LogicExpr left = ReplacePortExprRecursive(or.Operands[0], newOr.Operands[0]);
                LogicExpr right = ReplacePortExprRecursive(or.Operands[1], newOr.Operands[1]);
                if (left != null && right != null) return new OrExpr(new List<LogicExpr> { left, right });
                else
                {
                    Debug.LogWarning($"[BlockData] OrExpr 내부 교체 실패: {or} -> {expr}");
                    return null; // 내부 교체 실패
                }
            }
            else if (target is VarExpr || target is ConstantExpr)
            {
                if (target.Equals(expr)) return target;
                else
                {
                    Debug.LogWarning($"[BlockData] VarExpr 혹은 ConstantExpr는 동일한 표현식으로만 교체할 수 있습니다 (target: {target}, newExpr: {expr})");
                    return null; // VarExpr가 아닌 경우 실패
                }
            }

        // 그 외의 경우, 예외를 던지는 게 안전
        throw new System.InvalidOperationException($"지원하지 않는 LogicExpr 타입입니다: {target.GetType()}");
    }

    private bool ContainsPort(LogicExpr expr)
    {
        if (expr is PortExpr port) return true;
        else if (expr is NotExpr not) return ContainsPort(not.Inner);
        else if (expr is AndExpr and) return and.Operands.Any(ContainsPort);
        else if (expr is OrExpr or) return or.Operands.Any(ContainsPort);
        else return false; // VarExpr, ConstantExpr 등은 포트가 아님
    }

    private LogicExpr SynchronizePort(LogicExpr expr)
    {
        DebugDictionary(); // 디버그용: 현재 포트 딕셔너리 상태 출력
        if (expr is PortExpr port)
        {
            int index = portExprs.IndexOf(port);
            if (portDict.ContainsKey(index))
            {
                return portDict[index]; // 이미 존재하는 포트는 그대로 반환
            }
            return port;
        }
        else if (expr is NotExpr not)
        {
            LogicExpr inner = SynchronizePort(not.Inner);
            return new NotExpr(inner);
        }
        else if (expr is AndExpr and)
        {
            LogicExpr left = SynchronizePort(and.Operands[0]);
            LogicExpr right = SynchronizePort(and.Operands[1]);
            return new AndExpr(new List<LogicExpr> { left, right });
        }
        else if (expr is OrExpr or)
        {
            LogicExpr left = SynchronizePort(or.Operands[0]);
            LogicExpr right = SynchronizePort(or.Operands[1]);
            return new OrExpr(new List<LogicExpr> { left, right });
        }
        else
        {
            return expr; // 그대로 반환
        }
    }

    private void DebugDictionary()
    {
        foreach (var kvp in portDict)
        {
            Debug.Log($"Port: {portExprs[kvp.Key]}, LogicExpr: {kvp.Value}");
        }
    }

    #region Grid to Port Conversion
    public int GridToIndex(Vector2Int gridPos) => Grid.IndexOf(gridPos);
    public LogicExpr IndexToPort(int index) => Port[index];
    #endregion
}
