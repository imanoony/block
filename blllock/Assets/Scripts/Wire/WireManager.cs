using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Universal.ShaderGraph;
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

            if (!curr.Updated) Eval(curr);

            if (curr.Depend == null) continue;
            foreach (Wire w in curr.Depend)
            {
                if (!visited.Contains(w)) { prop.Add(w); visited.Add(w); }
            }
        }

        // 모두 새롭게 Evaluation 되었으면 패치한다.
        ResetWires();
        return true;
    }

    public void AddToDict() { }
    public void RemoveFromDict() { }

    // w1==w2이 현재 환경에 적용될 수 있는지 계산한다.
    // 계산 결과 적용될 수 있다면 true를, 적용될 수 없다면 false를 반환한다.
    // 도달 가능성이 아닌 호환 가능성을 확인한다. (가능한 모든 조합 모두 확인해야 함)
    public bool Compatible(Wire w1, WireExpr w2, HashSet<(WireExpr, WireExpr)> visited = null)
    {
        if (w1.Equals(w2)) return true;
        if (WireDict.ContainsKey(w1.ID) && WireDict[w1.ID].Contains(w2)) return true;
        if (WireLogic.ContainsKey(w1.ID)) return Compatible(w2, WireLogic[w1.ID]);
        if (w2.Contains(w1)) return false;

        if (w2 is Wire w)
        {
            // WireLogic과의 모순 확인
            if (WireLogic.ContainsKey(w.ID)) return Compatible(w1, WireLogic[w.ID]);

            // WireDict와의 모순 확인
            HashSet<WireExpr> subst1 = WireDict.ContainsKey(w1.ID) ? WireDict[w1.ID] : new();
            HashSet<WireExpr> subst2 = WireDict.ContainsKey(w.ID) ? WireDict[w.ID] : new();
            subst1.Add(w1); subst2.Add(w);

            HashSet<(WireExpr, WireExpr)> newvisited = (
                from a in subst1
                from b in subst2
                where visited == null || (!visited.Contains((a, b)) && !visited.Contains((b, a)))
                select (a, b)
            ).ToHashSet();
            List<(WireExpr, WireExpr)> prop = newvisited.ToList();
            if (visited != null) newvisited.UnionWith(visited);

            for (int i = 0; i < prop.Count; i++)
            {
                // 모순을 발견했다면 적용할 수 없다고 보고 바로 종료한다.
                if (!Compatible(prop[i].Item1, prop[i].Item2, newvisited)) return false;
            }

            // 모순을 찾지 못했다면 적용될 수 있는 것으로 본다.
            return true;
        }
        else if (w2 is WireNot wn)
        {
            if (wn.Inner is Wire inw)
            {

            }
            else // Dependency Check
            {

            }
        }
        else if (w2 is WireAnd wa)
        {
            // w1에서 WireAnd subst를 찾는다.
            List<WireExpr> prop = WireDict.ContainsKey(w1.ID) ? WireDict[w1.ID].ToList() : new();
            HashSet<WireExpr> newvisited = new HashSet<WireExpr>(prop);
            while (prop.Count > 0)
            {
                WireExpr curr = prop[0];
                prop.RemoveAt(0);

                if (curr is WireAnd currwa) return Compatible(currwa.Left, wa.Left) && Compatible(currwa.Right, wa.Right);
                else if (curr is WireOr) return false;
                else if (curr is Wire currw && WireDict.ContainsKey(currw.ID))
                {
                    foreach (WireExpr we in WireDict[currw.ID])
                    {
                        if (!newvisited.Contains(we)) { prop.Add(we); newvisited.Add(we); }
                    }
                }
                else if (curr is WireNot currwn && currwn.Inner is Wire currinw && WireDict.ContainsKey(currinw.ID))
                {
                    foreach (WireExpr we in WireDict[currinw.ID])
                    {
                        WireExpr newwe = new WireNot(we).Clean();
                        if (!newvisited.Contains(newwe)) { prop.Add(newwe); newvisited.Add(newwe); }
                    }
                }
                else if (curr is WireNot) return false;
            }

            // WireAnd subst가 존재하지 않는 경우
            // Dependency Check
            return !DependencyCheck(Decompose(wa), w1);
        }
        else if (w2 is WireOr wo)
        {
            // w1에서 WireAnd subst를 찾는다.
            List<WireExpr> prop = WireDict.ContainsKey(w1.ID) ? WireDict[w1.ID].ToList() : new();
            HashSet<WireExpr> newvisited = new HashSet<WireExpr>(prop);
            while (prop.Count > 0)
            {
                WireExpr curr = prop[0];
                prop.RemoveAt(0);

                if (curr is WireOr currwo) return Compatible(currwo.Left, wo.Left) && Compatible(currwo.Right, wo.Right);
                else if (curr is WireAnd) return false;
                else if (curr is Wire currw && WireDict.ContainsKey(currw.ID))
                {
                    foreach (WireExpr we in WireDict[currw.ID])
                    {
                        if (!newvisited.Contains(we)) { prop.Add(we); newvisited.Add(we); }
                    }
                }
                else if (curr is WireNot currwn && currwn.Inner is Wire currinw && WireDict.ContainsKey(currinw.ID))
                {
                    foreach (WireExpr we in WireDict[currinw.ID])
                    {
                        WireExpr newwe = new WireNot(we).Clean();
                        if (!newvisited.Contains(newwe)) { prop.Add(newwe); newvisited.Add(newwe); }
                    }
                }
                else if (curr is WireNot) return false;
            }

            // WireAnd subst가 존재하지 않는 경우
            // Dependency Check
            return !DependencyCheck(Decompose(wo), w1);
        }

        return false;
    }
    public bool Compatible(WireExpr w1, WireExpr w2, HashSet<(WireExpr, WireExpr)> visited = null)
    {
        return false;
    }

    // w==l이 현재 환경에 적용될 수 있는지 계산한다.
    // 계산 결과 적용될 수 있다면 true를, 적용될 수 없다면 false를 반환한다.
    public bool Compatible(Wire w, LogicExpr l, HashSet<WireExpr> visited = null)
    {
        if (WireLogic.ContainsKey(w.ID)) return WireLogic[w.ID].Equals(l);

        HashSet<WireExpr> newvisited = WireDict[w.ID];
        List<WireExpr> prop = newvisited.Where(expr => visited == null || !visited.Contains(expr)).ToList();
        newvisited.Add(w);
        if (visited != null) newvisited.UnionWith(visited);

        for (int i = 0; i < prop.Count; i++)
        {
            // 모순을 발견했다면 적용될 수 없다고 보고 바로 종료한다.
            if (!Compatible(prop[i], l, newvisited)) return false;
        }

        // 모순을 찾지 못했다면 적용될 수 있는 것으로 본다.
        return true;
    }
    public bool Compatible(WireExpr w, LogicExpr l, HashSet<WireExpr> visited = null)
    {
        if (w is Wire wire) return Compatible(wire, l, visited);
        else if (w is WireNot wn) return Compatible(wn.Inner, new NotExpr(l).Clean(), visited);
        else if (w is WireAnd wa)
        {
            if (l is AndExpr a) return Compatible(wa.Left, a.Left, visited) && Compatible(wa.Right, a.Right, visited);
            return false; // WireAnd와 호환 가능한 LogicExpr는 AndExpr뿐이다.
        }
        else if (w is WireOr wo)
        {
            if (l is OrExpr o) return Compatible(wo.Left, o.Left, visited) && Compatible(wo.Right, o.Right, visited);
            return false; // WireOr과 호환 가능한 LogicExpr는 OrExpr뿐이다. 
        }
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

        for (int i = 0; i < prop.Count; i++)
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
            if (expr != null) return new NotExpr(expr).Clean();
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
    public void ResetWires() { foreach (Wire wire in Wires.Values) wire.SetUpdated(false); }

    // WireExpr 속에 들어 있는 모든 Wire를 집합으로 반환한다.
    private HashSet<Wire> Decompose(WireExpr expr)
    {
        if (expr is Wire w) return new() { w };
        else if (expr is WireNot wn) return Decompose(wn.Inner);
        else if (expr is WireAnd wa)
        {
            HashSet<Wire> result = Decompose(wa.Left);
            result.UnionWith(Decompose(wa.Right));
            return result;
        }
        else if (expr is WireOr wo)
        {
            HashSet<Wire> result = Decompose(wo.Left);
            result.UnionWith(Decompose(wo.Right));
            return result;
        }
        return null;
    }

    // Dependency Check 용도의 함수.
    // wires의 depend 범위에 target이 포함되는지 여부를 반환한다.
    // 포함된다면 true, 포함되지 않는다면 false를 반환한다.
    private bool DependencyCheck(HashSet<Wire> wires, Wire target) // 생각: 함수를 받아서 depend에 대해 특정 기능을 수행하는 애로 만들어도 될 듯.
    {
        HashSet<Wire> visited = new HashSet<Wire>(wires);
        List<Wire> prop = visited.ToList();
        while (prop.Count > 0)
        {
            Wire curr = prop[0];
            prop.RemoveAt(0);

            if (curr.Equals(target)) return true;
            if (curr.Depend == null) continue;
            foreach (Wire w in curr.Depend)
            {
                if (!visited.Contains(w)) { prop.Add(w); visited.Add(w); }
            }
        }

        return true;
    }
}