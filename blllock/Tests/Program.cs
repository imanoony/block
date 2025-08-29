using System;

class Program
{
    static WireManager wm = new WireManager();
    static void Main()
    {
        //Console.WriteLine("Hello Console!");  // printf랑 동일한 역할
        //Test_Decompose(); -> DONE
        //Test_Dict(); -> DONE
        //Test_Eval1(); -> DONE
        Test_Eval2();
    }

    static void Test_Eval1()
    {
        Wire a = new Wire(wm.GenerateID()), b = new Wire(wm.GenerateID()), c = new Wire(wm.GenerateID());
        wm.AddWire(a); wm.AddWire(b); wm.AddWire(c);

        wm.AddToDict(a.ID, b.ID);
        wm.AddToDict(b.ID, c.ID);

        Print_Wires();

        wm.AddToLogic(c.ID, new VarExpr("X"), true);

        Print_Wires();

        Print_LogicExpr(wm.Eval(a));
    }

    static void Test_Eval2()
    {
        Wire a = new Wire(1, "a"), b = new Wire(2, "b"), c = new Wire(3, "c");
        Wire d = new Wire(4, "d"), e = new Wire(5, "e"), f = new Wire(6, "f");
        wm.AddWire(a); wm.AddWire(b); wm.AddWire(c);
        wm.AddWire(d); wm.AddWire(e); wm.AddWire(f);

        wm.AddToDict(1, new WireOr(b, c));
        wm.AddToDict(2, d);
        wm.AddToLogic(4, new VarExpr("X"), true);
        Print_Wires();

        wm.AddToLogic(3, new VarExpr("Y"), true);
        wm.AddToDict(1, new WireOr(e, f), true);
        Print_Wires();

        Print_LogicExpr(wm.Eval(new WireOr(e, f)));
    }

    static void Test_Decompose()
    {
        Wire a = new Wire(1, "a"), b = new Wire(2, "b"), c = new Wire(3, "c");

        WireExpr expr1 = new WireAnd(a, b);
        WireExpr expr2 = new WireAnd(a, expr1);
        WireExpr expr3 = new WireNot(c);
        WireExpr expr4 = new WireOr(expr3, expr2);
        WireExpr expr5 = new WireNot(expr4);

        Print_HashSet(wm.Decompose(expr1));
        Print_HashSet(wm.Decompose(expr2));
        Print_HashSet(wm.Decompose(expr3));
        Print_HashSet(wm.Decompose(expr4));
        Print_HashSet(wm.Decompose(expr5));
    }

    static void Print_HashSet<T>(HashSet<T> set) => Console.WriteLine(string.Join(", ", set));

    static void Print_WireDict()
    {
        if (wm.WireDict.Count == 0) Console.WriteLine("Dictionary가 비었습니다.");
        foreach (var kvp in wm.WireDict)
        {
            Console.WriteLine($"({kvp.Key}: {string.Join(", ", kvp.Value)})");
        }
    }

    static void Print_Wires()
    {
        if (wm.Wires.Count == 0) Console.WriteLine("Wire가 없습니다.");
        foreach (var kvp in wm.Wires)
        {
            LogicExpr? expr = kvp.Value.Cache;
            string exprStr = expr == null ? "null" : expr.ToString();
            Console.WriteLine($"{kvp.Value} -> {exprStr}");
        }
        Print_Line();
    }

    static void Print_LogicExpr(LogicExpr? expr)
    {
        if (expr == null) Console.WriteLine("null");
        else Console.WriteLine(expr);
    }

    static void Print_Line() => Console.WriteLine("-----------------");
}
