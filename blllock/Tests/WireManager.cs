using System.Collections.Generic;
using System.Linq;

public class WireManager
{
    public Dictionary<int, Wire> Wires { get; private set; } = new Dictionary<int, Wire>(); // ID에 대한 Wire 매핑
    public Dictionary<int, HashSet<Wire>> WireDict { get; private set; } = new Dictionary<int, HashSet<Wire>>(); // Wire끼리의 관계
    public Dictionary<int, LogicExpr> WireLogic { get; private set; } = new Dictionary<int, LogicExpr>(); // Wire와 LogicExpr 매핑

    // 우선 매번 동적으로 계산하되, 이후 복잡도 이슈 생기면 수정한다.

    #region Graph
    public WireExpr? GetSignature(int id)
    {
        if (!WireDict.ContainsKey(id)) return null;
        if (WireLogic.ContainsKey(id)) return LogicToWire(WireLogic[id]);
        if (Wires[id].Signature != null) return Wires[id].Signature;
        if (Wires[-id].Signature != null) return new WireNot(Wires[-id].Signature).Clean();

        List<Wire> prop = WireDict[id].ToList();
        HashSet<Wire> visited = new(prop);
        visited.Add(Wires[id]);

        while (prop.Count > 0)
        {
            Wire curr = prop[0];
            prop.RemoveAt(0);

            if (WireLogic.ContainsKey(curr.ID)) return LogicToWire(WireLogic[curr.ID]);
            if (curr.Signature != null) return curr.Signature;
            if (Wires[-curr.ID].Signature != null) return new WireNot(Wires[-curr.ID].Signature).Clean();

            if (!WireDict.ContainsKey(curr.ID)) continue;
            foreach (Wire w in WireDict[curr.ID])
            {
                if (!visited.Contains(w)) { prop.Add(w); visited.Add(w); }
            }
        }

        return null;
    }

    public HashSet<int> GetEquivalents(int id)
    {
        HashSet<int> result = new(), visited = new();
        Stack<int> stack = new();
        stack.Push(id);
    }

    // 두 노드의 호환 가능성을 반환
    public bool Compatible(int w1, int w2)
    {
        if (!WireDict.ContainsKey(w1) && !WireLogic.ContainsKey(w1)) return true;
        if (!WireDict.ContainsKey(w2) && !WireLogic.ContainsKey(w2)) return true;

        WireExpr? s1 = GetSignature(w1); s2 = GetSignature(w2);
        if (s1 != null && s2 != null)
        {
            if (!CompareSig(s1, s2)) return false;
            LogicExpr? l1 = Eval(w1); l2 = Eval(w2);
            if (l1 != null && l2 != null) return l1.Equals(l2);
        }

        // Level Check
    }
    #endregion

    #region Mapping
    public bool TryToAdd(WireExpr w1, WireExpr w2)
    {

    }

    public bool TryToAdd(WireExpr w, LogicExpr l)
    {

    }

    public LogicExpr? Eval(Wire w)
    {

    }

    public LogicExpr? Eval(WireExpr w)
    {

    }
    #endregion

    #region Signature
    // LogicExpr -> Wire Signature
    private WireExpr LogicToSig(LogicExpr expr)
    {
        if (expr is NotExpr n) return new WireNot(LogicToWire(n.Inner));
        else if (expr is AndExpr a) return new WireAnd(LogicToWire(a.Left), LogicToWire(a.Right));
        else if (expr is OrExpr o) return new WireOr(LogicToWire(o.Left), LogicToWire(o.Right));
        else return new Wire(0);
    }
    private bool CompareSig(WireExpr w1, WireExpr w2)
    {
        if (w1 is Wire && w2 is Wire) return true;
        if (w1 is WireNot n1 && w2 is WireNot n2) return CompareSig(n1.Inner, n2.Inner);
        if (w1 is WireAnd w1a)
    }
    #endregion
}