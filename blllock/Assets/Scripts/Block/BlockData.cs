using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class BlockData
{
    public int ID;
    public Vector2Int Size; // (height, width)
    public List<Vector2Int> Tile; // 블록 형태 타일 위치 리스트
    public Dictionary<Vector2Int, LogicExpr> Grid; // 각 타일 위치별 LogicExpr (포트 포함)
    public List<PortExpr> Ports; // 포트 리스트
    public Sprite Sprite; // 블록 스프라이트

    // 포트 ID -> LogicExpr 매핑 (런타임 변경 가능)
    public Dictionary<int, LogicExpr> PortDict;

    public BlockData()
    {
        Tile = new List<Vector2Int>();
        Grid = new Dictionary<Vector2Int, LogicExpr>();
        Ports = new List<PortExpr>();
        PortDict = new Dictionary<int, LogicExpr>();
    }

    // 새 포트 매핑 시도 (같은 포트에 기존 매핑과 다르면 모순)
    public bool AddPortMapping(int portId, LogicExpr expr)
    {
        if (PortDict.TryGetValue(portId, out var existingExpr))
        {
            if (!existingExpr.Equals(expr))
            {
                Debug.LogWarning($"[BlockData] Contradiction: Port {portId} has different existing expr.");
                return false;
            }
            return true; // 기존과 같으면 OK
        }

        PortDict[portId] = expr;

        // Grid 내 해당 포트 표현 치환 (포트 매핑 반영)
        var keys = new List<Vector2Int>(Grid.Keys);
        foreach (var pos in keys)
        {
            Grid[pos] = ReplacePortExprRecursive(Grid[pos], portId, expr);
        }

        return true;
    }

    private LogicExpr ReplacePortExprRecursive(LogicExpr target, int portId, LogicExpr newExpr)
    {
        if (target is PortExpr port)
        {
            int idx = Ports.IndexOf(port);
            if (idx == portId)
                return newExpr;
            return target;
        }
        else if (target is NotExpr not)
        {
            return new NotExpr(ReplacePortExprRecursive(not.Inner, portId, newExpr));
        }
        else if (target is AndExpr and)
        {
            var newOps = and.Operands.Select(op => ReplacePortExprRecursive(op, portId, newExpr)).ToList();
            return new AndExpr(newOps);
        }
        else if (target is OrExpr or)
        {
            var newOps = or.Operands.Select(op => ReplacePortExprRecursive(op, portId, newExpr)).ToList();
            return new OrExpr(newOps);
        }
        return target;
    }
}
