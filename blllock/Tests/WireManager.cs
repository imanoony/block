#nullable enable

using System.Collections.Generic;
using System.Linq;

public class WireManager
{
    public Dictionary<int, Wire> Wires { get; private set; } = new Dictionary<int, Wire>(); // ID에 대한 Wire 매핑
    public Dictionary<int, HashSet<int>> WireDict { get; private set; } = new Dictionary<int, HashSet<int>>(); // Wire끼리의 관계
    public Dictionary<int, LogicExpr> WireLogic { get; private set; } = new Dictionary<int, LogicExpr>(); // Wire와 LogicExpr 매핑

    private int nextID = 1;
    public int GenerateID() => nextID++;

    // 우선 매번 동적으로 계산하되, 이후 복잡도 이슈 생기면 수정한다.

    #region Graph

    // neg flag를 통해 id에 연결이 적은 음수나 양수가 들어와도 모든 Equivalents를 반환하도록 함
    public HashSet<int> GetEquivalents(int id, HashSet<int>? visited = null, bool neg = true)
    {
        HashSet<int> result = new();
        visited ??= new();
        Stack<int> stack = new();
        stack.Push(id);

        while (stack.Count > 0)
        {
            int curr = stack.Pop();
            if (!visited.Add(curr)) continue;
            result.Add(curr);

            if (!WireDict.ContainsKey(curr)) continue;
            foreach (int eqID in WireDict[curr])
            {
                if (!visited.Contains(eqID)) stack.Push(eqID);
            }

            if (Wires[curr].P == 0) continue;
            HashSet<int> parentEq = GetEquivalents(Wires[curr].P, visited, true);
            foreach (int eqID in parentEq)
            {
                if (Wires[eqID].L == 0) continue;
                if (Wires[Wires[curr].P].L == curr && !visited.Contains(Wires[eqID].L)) stack.Push(Wires[eqID].L);
                if (Wires[Wires[curr].P].R == curr && !visited.Contains(Wires[eqID].R)) stack.Push(Wires[eqID].R);
            }
        }

        if (neg)
        {
            HashSet<int> negEq = GetEquivalents(-id, null, false);
            result.UnionWith(negEq.Select(x => -x).ToHashSet());
        }

        return result;
    }

    public WireExpr? GetSignature(int id, HashSet<int>? equivalents = null)
    {
        if (Wires[-id].Signature != null) return new WireNot(Wires[-id].Signature);

        HashSet<int> eq = equivalents == null ? GetEquivalents(id) : new(equivalents);

        foreach (int eqID in eq)
        {
            if (Wires[eqID].Signature == null) continue;
            return Wires[eqID].Signature;
        }

        return null;
    }

    // 두 노드의 호환 가능성을 반환
    public bool Compatible(int w1, int w2)
    {
        HashSet<int> eq1 = GetEquivalents(w1), eq2 = GetEquivalents(w2);
        WireExpr? s1 = GetSignature(w1, eq1), s2 = GetSignature(w2, eq2);
        if (s1 != null && s2 != null)
        {
            if (!CompareSig(s1, s2)) return false;
            LogicExpr? l1 = Eval(w1), l2 = Eval(w2);
            if (l1 != null && l2 != null) return l1.Equals(l2);
        }

        for (int i = 0; i < 2; i++)
        {
            int target = i == 0 ? w2 : w1;
            HashSet<int> eq = i == 0 ? eq1 : eq2;
            if (eq.Contains(target)) return true;
            if (eq.Contains(-target)) return false;

            HashSet<int> children = new();
            Stack<int> stack = new(eq);

            while (stack.Count > 0)
            {
                int curr = stack.Pop();
                if (Wires[curr].L == 0) continue;

                HashSet<int> child = GetEquivalents(Wires[curr].L);
                child.UnionWith(GetEquivalents(Wires[curr].R));
                children.UnionWith(child);

                stack = new(child);
            }

            if (children.Contains(target)) return false;
            if (children.Contains(-target)) return false;
        }

        return true;
    }
    #endregion

    #region Mapping
    public void AddWire(Wire pos, Wire neg) { Wires[pos.ID] = pos; Wires[-pos.ID] = neg; }
    public void RemoveWire(int id, bool neg = true)
    {
        List<int> remove = new();
        if (WireDict.TryGetValue(id, out HashSet<int> eq))
        {
            foreach (int eqID in eq)
            {
                WireDict[eqID].Remove(id);
                if (WireDict[eqID].Count == 0 && Wires[eqID].P != 0) remove.Add(Wires[eqID].P);
            }
        }
        if (WireLogic.ContainsKey(id)) WireLogic.Remove(id);

        foreach (int removeID in remove)
        {
            if (WireLogic.ContainsKey(Wires[removeID].L)) continue;
            RemoveSignature(removeID);
        }

        if (neg) RemoveWire(-id, false);
    }

    public bool AddToDict(int w1, int w2)
    {
        if (!Compatible(w1, w2)) return false;

        if (WireDict.ContainsKey(w1)) WireDict[w1].Add(w2);
        else WireDict[w1] = new() { w2 };
        if (WireDict.ContainsKey(w2)) WireDict[w2].Add(w1);
        else WireDict[w2] = new() { w1 };

        return true;
    }

    public bool AddToDict(WireExpr w1, WireExpr w2)
    {
        // 한쪽이 Wire인 경우
        if (w1 is Wire _ || w2 is Wire _)
        {
            Wire wire = w1 is Wire _ ? (Wire)w1 : (Wire)w2;
            WireExpr expr = w1 is Wire ? w2 : w1;

            if (expr is Wire w) return AddToDict(wire.ID, w.ID);
            if (expr is WireNot n) return AddToDict(-wire.ID, n.Inner);

            // expr is WireAnd or WireOr
            if (wire.L != 0)
            {
                if (expr is WireAnd a) return AddToDict(Wires[wire.L], a.Left) && AddToDict(Wires[wire.R], a.Right);
                if (expr is WireOr o) return AddToDict(Wires[wire.L], o.Left) && AddToDict(Wires[wire.R], o.Right);
            }

            // 새로운 자식 생성하고 시그니처 등록
            Wire left = new Wire(GenerateID(), wire.ID), leftneg = new Wire(-left.ID);
            Wire right = new Wire(GenerateID(), wire.ID), rightneg = new Wire(-right.ID);
            AddWire(left, leftneg); AddWire(right, rightneg);

            if (expr is WireAnd a) wire.Composite(new WireAnd(left, right), left.ID, right.ID);
            if (expr is WireOr o) wire.Composite(new WireOr(left, right), left.ID, right.ID);
            
            if (expr is WireAnd a) return AddToDict(left, a.Left) && AddToDict(right, a.Right);
            if (expr is WireOr o) return AddToDict(left, o.Left) && AddToDict(right, o.Right);
        }

        // 그렇지 않은 경우
        if (w1 is WireNot n1 && w2 is WireNot n2) return AddToDict(n1.Inner, n2.Inner);
        if (w1 is WireAnd a1 && w2 is WireAnd a2) return AddToDict(a1.Left, a2.Left) && AddToDict(a1.Right, a2.Right);
        if (w1 is WireOr o1 && w2 is WireOr o2) return AddToDict(o1.Left, o2.Left) && AddToDict(o1.Right, o2.Right);
        return false;
    }

    public LogicExpr? Eval(int id, HashSet<int>? equivalents = null)
    {
        HashSet<int> eq = equivalents == null ? GetEquivalents(id) : new(equivalents);

        foreach (int eqID in eq)
        {
            if (WireLogic.ContainsKey(eqID)) return WireLogic[eqID];
            if (Wires[eqID].L == 0) continue;

            WireExpr? sig = Wires[eqID].Signature; // 여기서부터 수정 필요
            bool not = false; WireExpr? newsig = sig;
            if (sig is WireNot signot) { not = true; newsig = signot.Inner; }
            if (newsig is WireAnd a && a.Left is Wire al && a.Right is Wire ar)
            {
                LogicExpr? left = Eval(al.ID), right = Eval(ar.ID);
                if (left != null && right != null) return not ? new NotExpr(new AndExpr(left, right)) : new AndExpr(left, right);
            }
            if (newsig is WireOr o && o.Left is Wire ol && o.Right is Wire or)
            {
                LogicExpr? left = Eval(ol.ID), right = Eval(or.ID);
                if (left != null && right != null) return not ? new NotExpr(new OrExpr(left, right)) : new OrExpr(left, right);
            }
            return null;
        }

        return null;
    }
    #endregion

    #region Signature
    // LogicExpr -> Wire Signature
    private WireExpr LogicToSig(LogicExpr expr)
    {
        if (expr is NotExpr n) return new WireNot(LogicToSig(n.Inner));
        else if (expr is AndExpr a) return new WireAnd(LogicToSig(a.Left), LogicToSig(a.Right));
        else if (expr is OrExpr o) return new WireOr(LogicToSig(o.Left), LogicToSig(o.Right));
        else return new Wire(0);
    }
    private bool CompareSig(WireExpr w1, WireExpr w2)
    {
        if (w1 is Wire && w2 is Wire) return true;
        if (w1 is WireNot n1 && w2 is WireNot n2) return CompareSig(n1.Inner, n2.Inner);
        if (w1 is WireAnd a1 && w2 is WireAnd a2) return CompareSig(a1.Left, a2.Left) && CompareSig(a1.Right, a2.Right);
        if (w1 is WireOr o1 && w2 is WireOr o2) return CompareSig(o1.Left, o2.Left) && CompareSig(o1.Right, o2.Right);
        return false;
    }
    private void RemoveSignature(int id)
    {
        Wire wire = Wires[id];
        wire.Signature = null;
        Wires.Remove(wire.L); Wires.Remove(wire.R);
        Wires.Remove(-wire.L); Wires.Remove(-wire.R);
        wire.LeftChild = wire.RightChild = 0;
    }
    #endregion
}