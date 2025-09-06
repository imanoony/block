#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WireManager
{
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;
        ReserveWire(2);
        initialized = true;
    }
    public void RollBack(Dictionary<int, HashSet<int>> dict, Dictionary<int, LogicExpr> logic)
    {
        WireDict = dict;
        WireLogic = logic;
    }
    public string StringOfWireDict(Dictionary<int, HashSet<int>>? wireDict = null)
    {
        Dictionary<int, HashSet<int>> target = wireDict ?? WireDict;

        if (target == null || target.Count == 0) return "{}";

        // "키: {값1, 값2}" 형식으로 변환
        return string.Join("|", target.Select(kv =>$"{kv.Key}: {string.Join(", ", kv.Value)}"));
    }

    public Dictionary<int, Wire> Wires { get; private set; } = new Dictionary<int, Wire>(); // ID에 대한 Wire 매핑
    public Dictionary<int, HashSet<int>> WireDict { get; private set; } = new Dictionary<int, HashSet<int>>(); // Wire끼리의 관계
    public Dictionary<int, LogicExpr> WireLogic { get; private set; } = new Dictionary<int, LogicExpr>(); // Wire와 LogicExpr 매핑

    private int nextID = 1;
    private readonly SortedSet<int> freeIDs = new(); // 삭제된 ID를 작은 순으로 관리
    public int GenerateID()
    {
        if (nextID == int.MaxValue) throw new InvalidOperationException("Wire ID limit reached. Program will abort.");
        if (freeIDs.Count > 0)
        {
            int id = freeIDs.Min;
            freeIDs.Remove(id);
            return id;
        }
        return nextID++;
    }

    public bool AutoEval = false;

    // 우선 매번 동적으로 계산하되, 이후 복잡도 이슈 생기면 수정한다.

    #region Graph

    // neg flag를 통해 id에 연결이 적은 음수나 양수가 들어와도 모든 Equivalents를 반환하도록 함
    private HashSet<int> GetEquivalents(int id, HashSet<int>? visited = null, bool neg = true)
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

        Debug.Log($"[GetEquivalents:{id}] {string.Join(", ", result)}");
        return result;
    }

    // 두 노드의 호환 가능성을 반환
    private bool Compatible(int w1, int w2)
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

    // wire와 logic expr의 호환 가능성을 반환
    private bool Compatible(int w, LogicExpr l)
    {
        HashSet<int> eq = GetEquivalents(w);
        WireExpr? s = GetSignature(w, eq);
        if (s != null)
        {
            if (!CompareSig(s, LogicToSig(l))) return false;
            LogicExpr? wl = Eval(w);
            if (wl != null) return wl.Equals(l);
        }
        return true;
    }
    #endregion

    #region Mapping
    public void AddWire(Wire pos, Wire? neg = null)
    {
        Wires[pos.ID] = pos;
        if (neg != null) Wires[-pos.ID] = neg;
        else Wires[-pos.ID] = new Wire(-pos.ID);
    }

    // 일단 Wires에서는 삭제하지 않고 WireDict, WireLogic에서만 삭제하는 상태.
    public void RemoveWire(int id, bool neg = true)
    {
        Debug.Log($"[RemoveWire:{id}]");
        if (id < reservedCount && id > -reservedCount) return;

        List<int> sigRemove = new();
        if (WireDict.TryGetValue(id, out HashSet<int> eq))
        {
            foreach (int eqID in eq.ToList())
            {
                WireDict[eqID].Remove(id);
                if (WireDict[eqID].Count == 0)
                {
                    if (Wires[eqID].P != 0) removeIDs.Add(Wires[eqID].P);
                    else WireDict.Remove(eqID);
                }
            }
            WireDict.Remove(id);
        }
        if (WireLogic.ContainsKey(id)) WireLogic.Remove(id);
        if (Wires[id].L != 0) { RemoveWire(Wires[id].L); RemoveWire(Wires[id].R); }
        // if (id > 0) freeIDs.Add(id);
        // Wires.Remove(id);

        if (neg) RemoveWire(-id, false);
    }

    // NOTE: AddTo_ 계열의 함수들은 사용할 때, 결과가 false라면 rollback을 해줘야 한다.
    // 내부에 rollback 기능이 없어서 외부에서 반드시 해줘야 함!!!

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
            if (expr is WireNot n) return AddToDict(Wires[-wire.ID], n.Inner!);

            // 기존의 시그니처에 부합하는지 확인
            WireExpr? wsig = GetSignature(wire.ID), esig = GetSignature(expr);
            if (!CompareSig(wsig, esig)) return false;

            // expr is WireAnd or WireOr
            if (wire.L != 0)
            {
                if (expr is WireAnd wa) return AddToDict(Wires[wire.L], wa.Left!) && AddToDict(Wires[wire.R], wa.Right!);
                if (expr is WireOr wo) return AddToDict(Wires[wire.L], wo.Left!) && AddToDict(Wires[wire.R], wo.Right!);
                return false;
            }

            // 새로운 자식 생성하고 시그니처 등록
            Wire left = new Wire(GenerateID(), wire.ID), leftneg = new Wire(-left.ID);
            Wire right = new Wire(GenerateID(), wire.ID), rightneg = new Wire(-right.ID);
            AddWire(left, leftneg); AddWire(right, rightneg);

            if (expr is WireAnd _) wire.Composite(new WireAnd(left, right), left.ID, right.ID);
            if (expr is WireOr _) wire.Composite(new WireOr(left, right), left.ID, right.ID);

            if (expr is WireAnd a) return AddToDict(left, a.Left!) && AddToDict(right, a.Right!);
            if (expr is WireOr o) return AddToDict(left, o.Left!) && AddToDict(right, o.Right!);
            return false;
        }

        // 그렇지 않은 경우
        if (w1 is WireNot n1 && w2 is WireNot n2) return AddToDict(n1.Inner!, n2.Inner!);
        if (w1 is WireAnd a1 && w2 is WireAnd a2) return AddToDict(a1.Left!, a2.Left!) && AddToDict(a1.Right!, a2.Right!);
        if (w1 is WireOr o1 && w2 is WireOr o2) return AddToDict(o1.Left!, o2.Left!) && AddToDict(o1.Right!, o2.Right!);
        return false;
    }

    public bool AddToLogic(int w, LogicExpr l)
    {
        if (!Compatible(w, l)) return false;

        if (l is VarExpr _ || l is ConstantExpr _) { WireLogic[w] = l; return true; }
        if (l is NotExpr n) return AddToLogic(-w, n.Inner);

        Wire wire = Wires[w];

        // l is AndExpr or OrExpr
        if (Wires[w].L != 0)
        {
            if (l is AndExpr la) return AddToLogic(wire.L, la.Left) && AddToLogic(wire.R, la.Right);
            if (l is OrExpr lo) return AddToLogic(wire.L, lo.Left) && AddToLogic(wire.R, lo.Right);
            return false;
        }

        // 새로운 자식 생성하고 시그니처 등록
        Wire left = new Wire(GenerateID(), wire.ID), leftneg = new Wire(-left.ID);
        Wire right = new Wire(GenerateID(), wire.ID), rightneg = new Wire(-right.ID);
        AddWire(left, leftneg); AddWire(right, rightneg);

        if (l is AndExpr _) wire.Composite(new WireAnd(left, right), left.ID, right.ID);
        if (l is OrExpr _) wire.Composite(new WireOr(left, right), left.ID, right.ID);

        if (l is AndExpr a) return AddToLogic(left.ID, a.Left) && AddToLogic(right.ID, a.Right);
        if (l is OrExpr o) return AddToLogic(left.ID, o.Left) && AddToLogic(right.ID, o.Right);
        return false;
    }

    public bool AddToLogic(WireExpr w, LogicExpr l)
    {
        if (w is Wire _) return AddToLogic(((Wire)w).ID, l);
        if (w is WireNot n) return AddToLogic(n.Inner!, new NotExpr(l).Clean());
        if (w is WireAnd aw && l is AndExpr al) return AddToLogic(aw.Left!, al.Left) && AddToLogic(aw.Right!, al.Right);
        if (w is WireOr ow && l is OrExpr ol) return AddToLogic(ow.Left!, ol.Left) && AddToLogic(ow.Right!, ol.Right);
        return false;
    }

    public LogicExpr? Eval(int id, HashSet<int>? equivalents = null, bool neg = true)
    {
        HashSet<int> eq = equivalents == null ? GetEquivalents(id) : new(equivalents);

        LogicExpr? result = null;
        foreach (int eqID in eq)
        {
            if (WireLogic.ContainsKey(eqID))
            {
                result = WireLogic[eqID];
                if (AutoEval) EvalEquivalents(eq, result);
                return result;
            }
            if (Wires[eqID].L == 0) continue;

            WireExpr? sig = GetSignature(eqID); // 여기서부터 수정 필요
            bool not = false; WireExpr? newsig = sig;
            if (sig is WireNot signot) { not = true; newsig = signot.Inner; }
            if (newsig is WireAnd _ || newsig is WireOr _)
            {
                LogicExpr? left = Eval(Wires[eqID].L), right = Eval(Wires[eqID].R);
                if (left != null && right != null)
                {
                    if (newsig is WireAnd _) result = not ? new NotExpr(new AndExpr(left, right)) : new AndExpr(left, right);
                    else result = not ? new NotExpr(new OrExpr(left, right)) : new OrExpr(left, right);

                    if (AutoEval) EvalEquivalents(eq, result);
                    return result;
                }
            }

            if (AutoEval) EvalEquivalents(eq, null);
            return null;
        }

        if (neg)
        {
            LogicExpr? negEval = Eval(-id, eq.Select(x => -x).ToHashSet(), false);
            if (negEval != null)
            {
                result = new NotExpr(negEval).Clean();
                if (AutoEval) EvalEquivalents(eq, result);
                return result;
            }
        }

        if (AutoEval) EvalEquivalents(eq, null);
        return null;
    }

    public LogicExpr? Eval(WireExpr expr)
    {
        if (expr is Wire w) return Eval(w.ID);
        if (expr is WireNot n)
        {
            LogicExpr? inner = Eval(n.Inner!);
            if (inner == null) return null;
            return new NotExpr(inner).Clean();
        }
        if (expr is WireAnd a)
        {
            LogicExpr? left = Eval(a.Left!), right = Eval(a.Right!);
            if (left != null && right != null) return new AndExpr(left, right);
            return null;
        }
        if (expr is WireOr o)
        {
            LogicExpr? left = Eval(o.Left!), right = Eval(o.Right!);
            if (left != null && right != null) return new OrExpr(left, right);
            return null;
        }
        return null;
    }

    private void EvalEquivalents(HashSet<int> equivalents, LogicExpr? l)
    {
        foreach (int eqID in equivalents)
        {
            Wires[eqID].Cache = l;
            Wires[-eqID].Cache = l != null ? new NotExpr(l).Clean() : null;
            Wires[eqID].Updated = true;
            Wires[-eqID].Updated = true;
        }
    }

    public void ResetWires() { foreach (var kvp in Wires) kvp.Value.Updated = false; }

    public void EvalAll()
    {
        foreach (var kvp in Wires)
        {
            if (kvp.Value.Updated) continue;
            Eval(kvp.Value.ID);
        }
    }
    #endregion

    #region Signature
    public WireExpr? GetSignature(int id, HashSet<int>? equivalents = null, bool neg = true)
    {
        HashSet<int> eq = equivalents == null ? GetEquivalents(id) : new(equivalents);

        foreach (int eqID in eq)
        {
            if (Wires[eqID].Signature == null) continue;
            if (Wires[eqID].Signature is not WireAnd _ && Wires[eqID].Signature is not WireOr _) return null;

            WireExpr? left = GetSignature(Wires[eqID].L);
            WireExpr? right = GetSignature(Wires[eqID].R);
            if (Wires[eqID].Signature is WireAnd _) return new WireAnd(left, right);
            else return new WireOr(left, right);
        }

        if (neg)
        {
            WireExpr? negSig = GetSignature(-id, eq.Select(x => -x).ToHashSet(), false);
            if (negSig != null) return new WireNot(negSig);
        }

        return null;
    }

    public WireExpr? GetSignature(WireExpr expr)
    {
        if (expr is Wire w) return GetSignature(w.ID);
        if (expr is WireNot n)
        {
            WireExpr? inner = GetSignature(n.Inner!);
            if (inner == null) return new WireNot(null);
            else return new WireNot(inner).Clean();
        }

        WireExpr? left, right;
        if (expr is WireAnd a) { left = GetSignature(a.Left!); right = GetSignature(a.Right!); return new WireAnd(left, right); }
        if (expr is WireOr o) { left = GetSignature(o.Left!); right = GetSignature(o.Right!); return new WireOr(left, right); }

        return null;
    }
    // LogicExpr -> Wire Signature
    private WireExpr LogicToSig(LogicExpr expr)
    {
        if (expr is NotExpr n) return new WireNot(LogicToSig(n.Inner));
        else if (expr is AndExpr a) return new WireAnd(LogicToSig(a.Left), LogicToSig(a.Right));
        else if (expr is OrExpr o) return new WireOr(LogicToSig(o.Left), LogicToSig(o.Right));
        else return new Wire(0);
    }
    private bool CompareSig(WireExpr? w1, WireExpr? w2)
    {
        if (w1 is null || w2 is null) return true;
        if (w1 is Wire && w2 is Wire) return true;
        if (w1 is WireNot n1 && w2 is WireNot n2) return CompareSig(n1.Inner, n2.Inner);
        if (w1 is WireAnd a1 && w2 is WireAnd a2) return CompareSig(a1.Left, a2.Left) && CompareSig(a1.Right, a2.Right);
        if (w1 is WireOr o1 && w2 is WireOr o2) return CompareSig(o1.Left, o2.Left) && CompareSig(o1.Right, o2.Right);
        return false;
    }
    private HashSet<int> removeIDs = new();
    public void RemoveSignature()
    {
        foreach (int id in removeIDs) RemoveSignature(id);
        removeIDs = new();
    }
    private void RemoveSignature(int id)
    {
        if (WireLogic.ContainsKey(Wires[id].L) || WireLogic.ContainsKey(Wires[id].R)) return;
        if (WireLogic.ContainsKey(-Wires[id].L) || WireLogic.ContainsKey(-Wires[id].R)) return;

        Wire wire = Wires[id];
        wire.Signature = null;
        Wires.Remove(wire.L); Wires.Remove(wire.R); freeIDs.Add(wire.L); freeIDs.Add(wire.R);
        Wires.Remove(-wire.L); Wires.Remove(-wire.R); 

        if (WireDict.ContainsKey(wire.L)) WireDict.Remove(wire.L);
        if (WireDict.ContainsKey(-wire.L)) WireDict.Remove(-wire.L);
        if (WireDict.ContainsKey(wire.R)) WireDict.Remove(wire.R);
        if (WireDict.ContainsKey(-wire.R)) WireDict.Remove(-wire.R);

        wire.LeftChild = wire.RightChild = 0;
    }
    #endregion

    #region Reserved
    // unique 포트 생성을 위한 template 와이어
    // 게임 셋업 이후 블록 파싱 직전에 호출되어야 한다
    private int reservedCount = -1;
    public void ReserveWire(int count)
    {
        reservedCount = count;
        for (int i = 0; i < count; i++) AddWire(new Wire(GenerateID()));
    }
    public Wire? GetReservedWire(int id)
    {
        if (reservedCount == -1) throw new Exception("와이어 템플릿이 예약되지 않음.");

        if (id > reservedCount || id < -reservedCount) return null;
        return Wires[id];
    }
    public int NameToReservedID(char name) => name - 'a' + 1;
    #endregion
}