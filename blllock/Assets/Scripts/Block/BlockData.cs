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
    private List<List<int>> map; // portExprs -> 해당 포트 변수가 들어 있는 Port index 리스트
    private List<PortExpr> portExprs; // 단위 포트 변수만 있는 리스트
    private Dictionary<int, LogicExpr> portDict; // 포트 ID -> LogicExpr 매핑 (런타임 변경 가능)

    public void Init()
    {
        Tile ??= new List<Vector2Int>();
        Grid ??= new List<Vector2Int>();
        Port ??= new List<LogicExpr>();

        portExprs ??= new List<PortExpr>();
        portDict ??= new Dictionary<int, LogicExpr>();

        InitMap();
    }

    private void InitMap() {
        map = new List<List<int>>(portExprs.Count);
        for (int i = 0; i < portExprs.Count; i++)
        {
            for (int j = 0; j < Port.Count; j++)
            {
                if (ContainsPortExpr(Port[j], portExprs[i])) map[i].Add(j);
            }
        }
    }

    public bool IsCompleted(int portIndex)
    {
        for (int i = 0; i < map.Count; i++) // i is portId
        {
            if (map[i].Contains(portIndex))
            {
                if (portDict.ContainsKey(i)) continue;
                else return false;
            }
        }
        return true;
    }

    private bool ContainsPortExpr(LogicExpr expr, PortExpr portExpr)
    {
        if (expr is PortExpr port)
        {
            return port.Equals(portExpr);
        }
        else if (expr is NotExpr not)
        {
            return ContainsPortExpr(not.Inner, portExpr);
        }
        else if (expr is AndExpr and)
        {
            return and.Operands.Any(op => ContainsPortExpr(op, portExpr));
        }
        else if (expr is OrExpr or)
        {
            return or.Operands.Any(op => ContainsPortExpr(op, portExpr));
        }
        return false;
    }

    // 새 포트 매핑 시도 (같은 포트에 기존 매핑과 다르면 모순)
    public bool AddPortMapping(int portIndex, LogicExpr expr)
    {
        if (portIndex < 0 || portIndex >= Port.Count)
        {
            Debug.LogWarning($"Invalid port index: {portIndex}");
            return false;
        }

        // Port 리스트 내 해당 LogicExpr
        var targetExpr = Port[portIndex];

        // 해당 포트가 완전히 논리식으로 채워져 있는 경우
        if (IsCompleted(portIndex))
        {
            if (targetExpr.Equals(expr)) return true;
            else
            {
                Debug.LogWarning($"[BlockData] Contradiction: PortExpr {portIndex} already has a different expr.");
                return false;
            }
        }

        // 해당 포트에 포트식이 남아 있는 경우
        // 조건 분기로 처리
        Dictionary<int, LogicExpr> backup = new Dictionary<int, LogicExpr>(portDict); // 백업
        LogicExpr subst = ReplacePortExprRecursive(targetExpr, expr);
        if (subst == null)
        {
            Debug.LogWarning($"[BlockData] Failed to replace PortExpr {portIndex} with {expr}");
            portDict = backup; // 백업으로 복원
            return false; // 교체 실패
        }
        Port[portIndex] = subst; // 포트 업데이트
        

        if (!ReplacePortExprRecursive(targetExpr, expr))
        {
            Debug.LogWarning($"[BlockData] Failed to replace PortExpr {portIndex} with {expr}");
            return false; // 교체 실패
        }


        bool success = true;

        return success;
    }

    private LogicExpr ReplacePortExprRecursive(LogicExpr target, LogicExpr newExpr)
    {
        if (target is PortExpr port)
        {
            int index = portExprs.IndexOf(port);
            if (portDict.ContainsKey(index))
            {
                Debug.LogWarning($"[BlockData] PortExpr {port} already exists in portDict.");
                return null; // 이미 존재하는 포트는 교체할 수 없음
            }
            portDict[index] = newExpr; // 포트 딕셔너리 업데이트
            return port;
        }
        else if (target is NotExpr not)
        {
            NotExpr newNot = new(newExpr);
            return ReplacePortExprRecursive(not.Inner, newExpr);
        }
        else if (target is AndExpr and)
        {
            if (newExpr is not AndExpr)
            {
                Debug.LogWarning($"[BlockData] AndExpr는 AndExpr로만 교체할 수 있습니다: {and} -> {newExpr}");
                return null; // AndExpr가 아닌 경우 실패
            }
            AndExpr newAnd = (AndExpr)newExpr;
            
            LogicExpr left = ReplacePortExprRecursive(and.Operands[0], newAnd.Operands[0]);
            LogicExpr right = ReplacePortExprRecursive(and.Operands[1], newAnd.Operands[1]);
            if (left != null && right != null) return new AndExpr(new List<LogicExpr> { left, right });
            else
            {
                Debug.LogWarning($"[BlockData] AndExpr 내부 교체 실패: {and} -> {newExpr}");
                return null; // 내부 교체 실패
            }
        }
        else if (target is OrExpr or)
        {
            if (newExpr is not OrExpr)
            {
                Debug.LogWarning($"[BlockData] OrExpr는 OrExpr로만 교체할 수 있습니다: {or} -> {newExpr}");
                return null; // OrExpr가 아닌 경우 실패
            }
            OrExpr newOr = (OrExpr)newExpr;

            LogicExpr left = ReplacePortExprRecursive(or.Operands[0], newOr.Operands[0]);
            LogicExpr right = ReplacePortExprRecursive(or.Operands[1], newOr.Operands[1]);
            if (left != null && right != null) return new OrExpr(new List<LogicExpr> { left, right });
            else
            {
                Debug.LogWarning($"[BlockData] OrExpr 내부 교체 실패: {or} -> {newExpr}");
                return null; // 내부 교체 실패
            }
        }
        else if (target is VarExpr || target is ConstantExpr)
        {
            if (target.Equals(newExpr)) return target;
            else
            {
                Debug.LogWarning($"[BlockData] VarExpr 혹은 ConstantExpr는 동일한 표현식으로만 교체할 수 있습니다");
                return null; // VarExpr가 아닌 경우 실패
            }
        }

        // 그 외의 경우, 예외를 던지는 게 안전
        throw new System.InvalidOperationException($"지원하지 않는 LogicExpr 타입입니다: {target.GetType()}");
    }

    private void ReplaceLogicExprRecursive(LogicExpr target, int portId)
    {
        // target 내부에서 port를 찾아 subst로 대체한다
        // target 내부에는 반드시 port가 있어야 한다 (map으로 관리함)
        PortExpr port = portExprs[portId];
        LogicExpr subst = portDict[portId];

        if (target is PortExpr)
        {
            if (target.Equals(port))
        }
    }


    #region Grid to Port Conversion
    public int GridToIndex(Vector2Int gridPos) => Grid.IndexOf(gridPos);
    public LogicExpr IndexToPort(int index) => Port[index];
    #endregion
}
