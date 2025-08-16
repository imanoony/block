using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WireManager : MonoBehaviour
{
    public static WireManager Instance { get; private set; }

    public Dictionary<int, Wire> Wires { get; private set; } = new Dictionary<int, Wire>(); // ID에 대한 Wire 매핑
    public Dictionary<int, HashSet<WireExpr>> WireDict { get; private set; } = new Dictionary<int, HashSet<WireExpr>>(); // Wire끼리의 관계
    public Dictionary<int, LogicExpr> WireLogic { get; private set; } = new Dictionary<int, LogicExpr>(); // Wire와 LogicExpr 매핑

    // Wires에 Wire를 추가한다.
    // 이미 추가되어 있다면 false를 반환한다.
    public bool AddWire(Wire wire)
    {
        string message;
        if (Wires.ContainsKey(wire.ID))
        {
            if (Wires[wire.ID] != wire)
            {
                message = $"Wire with ID {wire.ID} has multiple instances.";
                Utils.PrintError(message);
                return false;
            }
            message = $"Wire with ID {wire.ID} already exists.";
            Utils.PrintWarning(message);
            return false;
        }
        Wires[wire.ID] = wire;
        return true;
    }

    // Wires에서 ID를 사용하여 Wire를 제거한다. 
    // Wire가 존재하지 않는다면 false를 반환한다.
    // WireDict, WireLogic에서도 Wire를 제거한다. 
    // 삭제 이후 Wire와 연관된 다른 Wire들을 re-evaluation한다.
    public bool RemoveWire(int id)
    {
        if (!Wires.ContainsKey(id))
        {
            string message = $"Wire with ID {id} does not exist.";
            Utils.PrintError(message);
            return false;
        }

        Wire wire = Wires[id];
        Wires.Remove(id);
        if (WireDict.ContainsKey(id)) WireDict.Remove(id);
        if (WireLogic.ContainsKey(id)) WireLogic.Remove(id);

        // WireDict에서 wire를 참조하는 다른 id가 있을 수 있으므로
        // depend list를 바탕으로 WireDict에서 wire를 참조하는 id를 수정한다.
        // 참조하는 id의 depend list에서 @param id를 제거한다.
        if (wire.Depend == null) return true; // depend가 없으면 바로 true 반환.
        foreach (Wire depwire in wire.Depend)
        {
            int depid = depwire.ID;
            if (!WireDict.ContainsKey(depid) || WireDict[depid] == null) continue;
            WireDict[depid].RemoveWhere(expr => expr.Contains(wire));
            depwire.Depend.Remove(wire);

            // 더 이상 HashSet에 원소가 존재하지 않으면 참조하는 id를 제거한다.
            if (WireDict[depid].Count == 0) WireDict.Remove(depid);
        }

        // dependency-propagation 연쇄 re-evaluation
        List<Wire> prop = wire.Depend.ToList();
        HashSet<Wire> visited = new HashSet<Wire>(prop);
        while (prop.Count > 0)
        {
            Wire curr = prop[0];
            prop.RemoveAt(0);

            Eval(curr);

            if (curr.Depend == null) continue;
            foreach (Wire w in curr.Depend)
            {
                if (!visited.Contains(w)) { prop.Add(w); visited.Add(w); }
            }
        }

        return true;
    }

    // WireDict나 WireLogic의 Wire가 갱신된 경우 Evaluation 한다.
    // 갱신된 Wire와 연관된 다른 Wire들을 re-evaluation한다.
    public bool Equivalent(WireExpr w1, WireExpr w2)
    {
        return false;
    }
    public bool Equivalent(WireExpr w, LogicExpr l)
    {
        return false;
    }

    // Evaluation 성공 시 Wire 캐시에 LogicExpr를 저장하고 LogicExpr를 반환한다.
    // Evaluation 실패 시 Wire 캐시를 비우고 null을 반환한다.
    public LogicExpr Eval(Wire wire, HashSet<WireExpr> visited = null)
    {
        if (WireLogic.ContainsKey(wire.ID)) { wire.SetCache(WireLogic[wire.ID]); return WireLogic[wire.ID]; }
        if (!WireDict.ContainsKey(wire.ID) || WireDict[wire.ID] == null) { wire.SetCache(null); return null; }

        HashSet<WireExpr> newvisited = WireDict[wire.ID];
        List<WireExpr> prop = newvisited.Where(expr => visited == null || !visited.Contains(expr)).ToList();
        newvisited.Add(wire);
        if (visited != null) newvisited.UnionWith(visited);

        // 맨 처음 원소는 wire이므로 제외하고 iteration한다.
        for (int i = 1; i < prop.Count; i++)
        {
            LogicExpr curr = Eval(prop[i], newvisited);
            if (curr != null) { wire.SetCache(curr); return curr; }
        }

        return null;
    }

    public LogicExpr Eval(int id)
    {
        if (!Wires.ContainsKey(id))
        {
            string message = $"Wire with ID {id} does not exist.";
            Utils.PrintError(message);
            return null;
        }
        return Eval(Wires[id]);
    }

    // Evaluation 성공 시 WireExpr 속 Wire들 캐시에 LogicExpr를 저장하고 LogicExpr를 반환한다.
    // Evaluation 실패 시 WireExpr 속 Wire들 캐시를 비우고 null을 반환한다.
    public LogicExpr Eval(WireExpr wireExpr, HashSet<WireExpr> visited = null)
    {
        if (wireExpr is Wire w) return Eval(w, visited);

        LogicExpr expr, left, right;
        if (wireExpr is WireNot n)
        {
            expr = Eval(n.Inner, visited);
            if (expr != null) return new NotExpr(expr);
            return null;
        }
        else if (wireExpr is WireAnd a)
        {
            left = Eval(a.Left, visited);
            right = Eval(a.Right, visited);
            if (left != null && right != null) return new AndExpr(left, right);
            return null;
        }
        else if (wireExpr is WireOr o)
        {
            left = Eval(o.Left, visited);
            right = Eval(o.Right, visited);
            if (left != null && right != null) return new OrExpr(left, right);
            return null;
        }

        return null;
    }

    // 현재 존재하는 모든 Wire의 Updated 상태를 false로 바꾼다.
    // WireDict, WireLogic 변동에 따른 Evaluation들이
    // 모두 종료된 후 반드시 호출되어야 한다.
    public void FetchWires() { foreach (Wire wire in Wires.Values) wire.SetUpdated(false); }
}