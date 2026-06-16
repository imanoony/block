#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WireManager
{
    public Dictionary<int, Wire> Wires { get; private set; } = new Dictionary<int, Wire>(); // ID에 대한 Wire 매핑
    public Dictionary<int, HashSet<int>> WireDict { get; private set; } = new Dictionary<int, HashSet<int>>(); // Wire끼리의 관계
    public Dictionary<int, VarExpr> WireLogic { get; private set; } = new Dictionary<int, VarExpr>(); // Wire와 LogicExpr 매핑
    public void Initialize(bool autoEval = true)
    {
        Wires = new();
        WireDict = new();
        WireLogic = new();

        nextID = 1;
        freeIDs.Clear(); 
        ReserveWire(2); // 1번, 2번 Wire를 블록 초기 포트값 (a,b) 을 위해 예약

        AutoEval = autoEval;
    }
    public void RollBack(Dictionary<int, Wire> wires, Dictionary<int, HashSet<int>> dict, Dictionary<int, VarExpr> logic)
    {
        List<int> removeKeys = new();
        foreach (var kvp in Wires)
        {
            if (wires.ContainsKey(kvp.Key)) Wires[kvp.Key].Init(wires[kvp.Key]);
            else removeKeys.Add(kvp.Key);
        }
        foreach (int key in removeKeys) Wires.Remove(key);
        WireDict = dict;
        WireLogic = logic;

        EvalAll();
    }
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

    #region Graph

    // Wire와 Equivalents한 모든 다른 Wire를 반환함
    private HashSet<int> GetEquivalents(int id, HashSet<int>? visited = null)
    {
        HashSet<int> result = new();
        if (id == 0) return result;
        visited ??= new();
        Stack<int> stack = new();
        stack.Push(id);

        while (stack.Count > 0)
        {
            int curr = stack.Pop();
            if (!visited.Add(curr)) continue;
            result.Add(curr);

            HashSet<int> eqs = new();
            if (WireDict.ContainsKey(curr)) eqs.UnionWith(WireDict[curr]);
            if (eqs.Count == 0) continue;

            foreach (int eqID in eqs)
            {
                if (!visited.Contains(eqID)) stack.Push(eqID);
            }
        }

        return result;
    }

    // 두 Wire의 호환 가능성을 반환
    // 즉, 두 Wire를 equivalent하게 만들 수 있는지 boolean으로 반환
    private bool Compatible(int w1, int w2)
    {
        HashSet<int> eq1 = GetEquivalents(w1), eq2 = GetEquivalents(w2);
        LogicExpr? l1 = Eval(w1, eq1), l2 = Eval(w2, eq2);

        if (l1 != null && l2 != null) return l1.Equals(l2);
        else return true;
    }

    // wire와 logic expr(var)의 호환 가능성을 반환
    private bool Compatible(int w, VarExpr var)
    {   
        HashSet<int> eq = GetEquivalents(w);
        LogicExpr? wl = Eval(w, eq);

        if (wl != null) return wl.Equals(var);
        return true;
    }
    #endregion

    #region Mapping
    public void AddWire(Wire wire)
    {
        Wires[wire.ID] = wire;
    }

    // Wires에서는 삭제하지 않고 WireDict, WireLogic에서만 삭제한다.
    // 한 스테이지가 끝날 때까지 모든 Wire는 계속 존재하기 때문.
    public void RemoveWire(int id)
    {
        //Debug.Log($"[RemoveWire:{id}]");
        if (id <= reservedCount) return;

        if (WireDict.TryGetValue(id, out HashSet<int> eq))
        {
            foreach (int eqID in eq.ToList())
            {
                WireDict[eqID].Remove(id);
                if (WireDict[eqID].Count == 0)
                {
                    WireDict.Remove(eqID);
                }
            }
            WireDict.Remove(id);
        }
        if (WireLogic.ContainsKey(id)) WireLogic.Remove(id);
    }

    // ---------------------------------------------------------------------------
    // [ Add To ]
    // > WireExpr과 WireExpr를 매핑하거나, 또는 WireExpr와 LogicExprs을 매핑한다.
    // > bool AddToDict(int, int): Wire와 Wire를 매핑한다.
    // > bool AddToDict(WireExpr, WireExpr): WireExpr과 WireExpr를 매핑한다.
    // ---------------------------------------------------------------------------
    // NOTE: AddTo_ 계열의 함수들은 사용할 때, 결과가 false라면 rollback을 해줘야 한다.
    // 내부에 rollback 기능이 없어서 외부에서 반드시 해줘야 함!!!
    // ---------------------------------------------------------------------------

    public bool AddToDict(int w1, int w2) // Wire와 Wire 매핑
    {
        if (!Compatible(w1, w2)) return false;

        if (WireDict.ContainsKey(w1)) WireDict[w1].Add(w2);
        else WireDict[w1] = new() { w2 };
        if (WireDict.ContainsKey(w2)) WireDict[w2].Add(w1);
        else WireDict[w2] = new() { w1 };

        return true;
    }

    public bool AddToDict(Wire? w1, Wire? w2)
    {
        if (w1 == null || w2 == null) return true;
        else return AddToDict(w1.ID, w2.ID);
    }
    public bool AddToDict(PortExpr p1, PortExpr p2) // Port와 Port 매핑
    {
        return (
            AddToDict(p1.LeftUp, p2.LeftUp) &&
            AddToDict(p1.LeftDown, p2.LeftDown) &&
            AddToDict(p1.RightUp, p2.RightUp) &&
            AddToDict(p1.RightDown, p2.RightDown)
        );
    }
    public bool AddToLogic(int w, VarExpr? var)
    {
        if (var == null) return true;
        if (!Compatible(w, var)) return false;
        WireLogic[w] = var;
        return true;
    }
    public bool AddToLogic(Wire? wire, VarExpr? var)
    {
        if (wire == null) return true;
        else return AddToLogic(wire.ID, var);
    }

    public bool AddToLogic(PortExpr p, LogicExpr l)
    {
        CombExpr comb = l is VarExpr v ? v.ToCombExpr() : (CombExpr)l;
        return (
            AddToLogic(p.LeftUp, comb.LeftUp) &&
            AddToLogic(p.LeftDown, comb.LeftDown) &&
            AddToLogic(p.RightUp, comb.RightUp) &&
            AddToLogic(p.RightDown, comb.RightDown)
        );
    }

    public VarExpr? EvalCache(int id) => Wires[id].Cache;
    public VarExpr? EvalCache(Wire? wire)
    {
        if (wire == null) return null;
        else return wire.Cache;
    }

    public LogicExpr? EvalCache(PortExpr port)
    {
        VarExpr? leftup = EvalCache(port.LeftUp);
        VarExpr? leftdown = EvalCache(port.LeftDown);
        VarExpr? rightup = EvalCache(port.RightUp);
        VarExpr? rightdown = EvalCache(port.RightDown);

        if (
            leftup == null && 
            leftdown == null && 
            rightup == null && 
            rightdown == null
        )
        {
            return null;
        }
        else
        {
            return new CombExpr(
                leftup,
                leftdown,
                rightup,
                rightdown
            ).Clean();
        }
    }

    public VarExpr? Eval(int id, HashSet<int>? equivalents = null)
    {
        HashSet<int> eq = equivalents == null ? GetEquivalents(id) : new(equivalents);

        VarExpr? result = null;
        foreach (int eqID in eq)
        {
            if (WireLogic.ContainsKey(eqID))
            {
                result = WireLogic[eqID];
                if (AutoEval) EvalEquivalents(eq, result);
                return result;
            }
        }

        if (AutoEval) EvalEquivalents(eq, null);
        return null;
    }

    public VarExpr? Eval(Wire? wire)
    {
        if (wire == null) return null;
        else return Eval(wire.ID);
    }

    public LogicExpr? Eval(PortExpr port)
    {
        VarExpr? leftup = Eval(port.LeftUp);
        VarExpr? leftdown = Eval(port.LeftDown);
        VarExpr? rightup = Eval(port.RightUp);
        VarExpr? rightdown = Eval(port.RightDown);

        if (
            leftup == null && 
            leftdown == null && 
            rightup == null && 
            rightdown == null
        )
        {
            return null;
        }
        else
        {
            return new CombExpr(
                leftup,
                leftdown,
                rightup,
                rightdown
            ).Clean();
        }
    }

    private void EvalEquivalents(HashSet<int> equivalents, VarExpr? l)
    {
        foreach (int eqID in equivalents)
        {
            Wires[eqID].Cache = l;
            Wires[eqID].Updated = true;
        }
    }

    public void ResetWires() { foreach (var kvp in Wires) kvp.Value.Updated = false; }

    public void EvalAll()
    {
        ResetWires();
        foreach (var kvp in Wires)
        {
            if (kvp.Value.Updated) continue;
            Eval(kvp.Value.ID);
        }
        ResetWires();
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

        if (id > reservedCount || id <= 0) return null;
        return Wires[id];
    }
    public int NameToReservedID(char name) => name - 'a' + 1;
    #endregion

    #region Debug
    public string StringOfWires() => $"Wires --- {string.Join("|", Wires.Select(x => $"{x}"))}";
    public string StringOfWireDict(Dictionary<int, HashSet<int>>? wireDict = null)
    {
        Dictionary<int, HashSet<int>> target = wireDict ?? WireDict;

        if (target == null || target.Count == 0) return "{}";

        // "키: {값1, 값2}" 형식으로 변환
        return string.Join("|", target.Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value)}"));
    }
    public string StringOfWireLogic(Dictionary<int, VarExpr>? wireLogic = null)
    {
        Dictionary<int, VarExpr> target = wireLogic ?? WireLogic;

        if (target == null || target.Count == 0)
            return "{}";

        return string.Join(
            "|",
            target.Select(kv => $"{kv.Key}: {kv.Value}")
        );
    }
    #endregion
}