using System.Collections.Generic;
using System.Runtime.CompilerServices;

public abstract class WireExpr
{
    public abstract override string ToString();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    // WireExpr 내부에 wire가 포함되어 있는지 검사한다.
    // WireExpr 내부를 재귀적으로 탐색한다.
    public abstract bool Contains(Wire wire);

    // WireExpr 내부에 wireExpr가 포함되어 있는지 검사한다.
    // WireExpr 내부를 재귀적으로 탐색한다.
    public abstract bool Contains(WireExpr wireExpr);
}

public class Wire : WireExpr
{
    public int ID;
    public string Name;
    public bool Updated { get; private set; }
    public WireExpr? Signature;
    public LogicExpr? Cache { get; private set; }
    public int Parent, LeftChild, RightChild; // 부모 Wire, 자식 Wire의 ID. 없으면 -1.
    public int P => Parent;
    public int L => LeftChild;
    public int R => RightChild;

    public Wire(int id, int parent = 0)
    {
        ID = id;
        Name = "";
        Updated = false;
        Signature = null;
        Cache = null;
        Parent = parent;
        LeftChild = RightChild = 0;
    }
    public void Composite(WireExpr source, int l, int r)
    {
        Signature = source;
        LeftChild = l;
        RightChild = r;
    }
    public override string ToString() => ID.ToString();
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    public override bool Contains(Wire wire) => this == wire;
    public override bool Contains(WireExpr wireExpr) => this == wireExpr;
    public void SetCache(LogicExpr? expr) { Cache = expr; SetUpdated(true); }
    public void SetUpdated(bool updated) => Updated = updated;
    public void SetParent(int parent) => Parent = parent;
}

public class WireNot : WireExpr
{
    public WireExpr Inner { get; private set; }
    public WireNot(WireExpr? inner) => Inner = inner;
    public WireExpr Clean() // 이중 부정을 제거하기 위함
    {
        if (Inner is WireNot innerNot) return innerNot.Inner;
        return this;
    }
    public override string ToString() => Inner is Wire ? $"~{Inner}" : $"~({Inner})";
    public override bool Equals(object? obj)
    {
        if (obj is not WireNot wn) return false;
        return Inner.Equals(wn.Inner);
    }
    public override int GetHashCode() => Inner.GetHashCode() * 17;
    public override bool Contains(Wire wire) => Inner.Contains(wire);
    public override bool Contains(WireExpr wireExpr) => this == wireExpr || Inner.Contains(wireExpr);
}

public class WireAnd : WireExpr
{
    public WireExpr Left { get; private set; }
    public WireExpr Right { get; private set; }
    public WireAnd(WireExpr? left, WireExpr? right) { Left = left; Right = right; }
    public override string ToString()
    {
        string left = Left is Wire ? $"{Left}" : $"({Left})";
        string right = Right is Wire ? $"{Right}" : $"({Right})";
        return $"{left}{right}";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not WireAnd wa) return false;
        return Left.Equals(wa.Left) && Right.Equals(wa.Right);
    }
    public override int GetHashCode() => Left.GetHashCode() * 31 + Right.GetHashCode();
    public override bool Contains(Wire wire) => Left.Contains(wire) || Right.Contains(wire);
    public override bool Contains(WireExpr wireExpr)
    {
        if (this == wireExpr) return true;
        return Left.Contains(wireExpr) || Right.Contains(wireExpr);
    }
}

public class WireOr : WireExpr
{
    public WireExpr Left { get; private set; }
    public WireExpr Right { get; private set; }
    public WireOr(WireExpr? left, WireExpr? right) { Left = left; Right = right; }
    public override string ToString()
    {
        string left = Left is Wire ? $"{Left}" : $"({Left})";
        string right = Right is Wire ? $"{Right}" : $"({Right})";
        return $"{left}+{right}";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not WireOr wo) return false;
        return Left.Equals(wo.Left) && Right.Equals(wo.Right);
    }
    public override int GetHashCode() => Left.GetHashCode() * 31 + Right.GetHashCode();
    public override bool Contains(Wire wire) => Left.Contains(wire) || Right.Contains(wire);
    public override bool Contains(WireExpr wireExpr)
    {
        if (this == wireExpr) return true;
        return Left.Contains(wireExpr) || Right.Contains(wireExpr);
    }
}