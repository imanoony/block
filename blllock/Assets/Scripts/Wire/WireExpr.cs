#nullable enable

using System;
using System.Runtime.CompilerServices;

public abstract class WireExpr
{
    private LogicExpr? _cache;
    public LogicExpr? Cache
    {
        get => _cache;
        protected set
        {
            if (_cache == value) return;
            _cache = value;
            OnCacheChanged?.Invoke(this);
        }
    }
    public event Action<WireExpr>? OnCacheChanged;
    public abstract override string ToString();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();

    // WireExpr 내부에 wire가 포함되어 있는지 검사한다.
    // WireExpr 내부를 재귀적으로 탐색한다.
    public abstract bool Contains(Wire wire);

    // WireExpr 내부에 wireExpr가 포함되어 있는지 검사한다.
    // WireExpr 내부를 재귀적으로 탐색한다.
    public abstract bool Contains(WireExpr wireExpr);

    public static WireExpr? Parse(string exprString)
    {
        if (string.IsNullOrEmpty(exprString)) return null;

        if (exprString[0] == Utils.NOT) // 단항 not
        {
            string inner = exprString[1..];
            if (Utils.IsWrappedByParentheses(inner)) inner = inner[1..^1];
            return new WireNot(Parse(inner)).Clean();
        }

        if (Utils.IsWrappedByParentheses(exprString))
            return Parse(exprString[1..^1]);
        
        int depth = 0;
        for (int i = 0; i < exprString.Length; i++)
        {
            char c = exprString[i];
            if (c == Utils.PARENS[0]) depth++;
            else if (c == Utils.PARENS[1]) depth--;
            else if (depth == 0 && (c == Utils.AND || c == Utils.OR))
            {
                WireExpr? left = Parse(exprString[0..i]);
                WireExpr? right = Parse(exprString[(i + 1)..]);
                return c == Utils.AND ? new WireAnd(left, right) : new WireOr(left, right);
            }
        }

        if (exprString.Length == 1)
        {
            int reservedID = GameManager.Instance.Wire.NameToReservedID(exprString[0]);
            return GameManager.Instance.Wire.GetReservedWire(reservedID);
        }

        return null;
    }
}

public class Wire : WireExpr
{
    public int ID;
    public string Name;
    public bool Updated = false;
    public WireExpr? Signature;
    public new LogicExpr? Cache
    {
        get => base.Cache;
        set => base.Cache = value;
    }
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
    public Wire(Wire wire)
    {
        ID = wire.ID;
        Name = wire.Name;
        Updated = wire.Updated;
        Signature = wire.Signature;
        Cache = wire.Cache;
        Parent = wire.Parent;
        LeftChild = wire.LeftChild;
        RightChild = wire.RightChild;
    }
    public void Init(Wire wire)
    {
        ID = wire.ID;
        Name = wire.Name;
        Updated = wire.Updated;
        Signature = wire.Signature;
        Cache = wire.Cache;
        Parent = wire.Parent;
        LeftChild = wire.LeftChild;
        RightChild = wire.RightChild;
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
}

public class WireNot : WireExpr
{
    private WireExpr? _inner;
    public WireExpr? Inner
    {
        get => _inner;
        private set
        {
            if (_inner != null) _inner.OnCacheChanged -= InnerCacheChanged;
            _inner = value;
            if (_inner != null) _inner.OnCacheChanged += InnerCacheChanged;
            UpdateCache();
        }
    }
    public WireNot(WireExpr? inner) => Inner = inner;
    private void InnerCacheChanged(WireExpr _) => UpdateCache();
    private void UpdateCache() => Cache = Inner?.Cache != null ? new NotExpr(Inner.Cache).Clean() : null;
    public WireExpr? Clean() // 이중 부정을 제거하기 위함
    {
        if (Inner is WireNot innerNot) return innerNot.Inner;
        if (Inner is Wire wire) return GameManager.Instance.Wire.Wires[-wire.ID];
        return this;
    }
    public override string ToString() => Inner is Wire ? $"~{Inner}" : $"~({Inner})";
    public override bool Equals(object? obj)
    {
        if (obj is not WireNot wn) return false;
        return Inner!.Equals(wn.Inner);
    }
    public override int GetHashCode() => Inner!.GetHashCode() * 17;
    public override bool Contains(Wire wire) => Inner!.Contains(wire);
    public override bool Contains(WireExpr wireExpr) => this == wireExpr || Inner!.Contains(wireExpr);
}

public class WireAnd : WireExpr
{
    private WireExpr? _left, _right;
    public WireExpr? Left
    {
        get => _left;
        private set
        {
            if (_left != null) _left.OnCacheChanged -= ChildCacheChanged;
            _left = value;
            if (_left != null) _left.OnCacheChanged += ChildCacheChanged;
            UpdateCache();
        }
    }
    public WireExpr? Right
    {
        get => _right;
        private set
        {
            if (_right != null) _right.OnCacheChanged -= ChildCacheChanged;
            _right = value;
            if (_right != null) _right.OnCacheChanged += ChildCacheChanged;
            UpdateCache();
        }
    }
    public WireAnd(WireExpr? left, WireExpr? right) { Left = left; Right = right; }

    private void ChildCacheChanged(WireExpr _) => UpdateCache();
    private void UpdateCache()
    {
        if (Left?.Cache != null && Right?.Cache != null)
            Cache = new AndExpr(Left.Cache, Right.Cache);
        else Cache = null;
    }
    public override string ToString()
    {
        string left = Left is Wire ? $"{Left}" : $"({Left})";
        string right = Right is Wire ? $"{Right}" : $"({Right})";
        return $"{left}{right}";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not WireAnd wa) return false;
        return Left!.Equals(wa.Left) && Right!.Equals(wa.Right);
    }
    public override int GetHashCode() => Left!.GetHashCode() * 31 + Right!.GetHashCode();
    public override bool Contains(Wire wire) => Left!.Contains(wire) || Right!.Contains(wire);
    public override bool Contains(WireExpr wireExpr)
    {
        if (this == wireExpr) return true;
        return Left!.Contains(wireExpr) || Right!.Contains(wireExpr);
    }
}

public class WireOr : WireExpr
{
        private WireExpr? _left, _right;
    public WireExpr? Left
    {
        get => _left;
        private set
        {
            if (_left != null) _left.OnCacheChanged -= ChildCacheChanged;
            _left = value;
            if (_left != null) _left.OnCacheChanged += ChildCacheChanged;
            UpdateCache();
        }
    }
    public WireExpr? Right
    {
        get => _right;
        private set
        {
            if (_right != null) _right.OnCacheChanged -= ChildCacheChanged;
            _right = value;
            if (_right != null) _right.OnCacheChanged += ChildCacheChanged;
            UpdateCache();
        }
    }
    public WireOr(WireExpr? left, WireExpr? right) { Left = left; Right = right; }

    private void ChildCacheChanged(WireExpr _) => UpdateCache();
    private void UpdateCache()
    {
        if (Left?.Cache != null && Right?.Cache != null)
            Cache = new OrExpr(Left.Cache, Right.Cache);
        else Cache = null;
    }
    
    public override string ToString()
    {
        string left = Left is Wire ? $"{Left}" : $"({Left})";
        string right = Right is Wire ? $"{Right}" : $"({Right})";
        return $"{left}+{right}";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not WireOr wo) return false;
        return Left!.Equals(wo.Left) && Right!.Equals(wo.Right);
    }
    public override int GetHashCode() => Left!.GetHashCode() * 31 + Right!.GetHashCode();
    public override bool Contains(Wire wire) => Left!.Contains(wire) || Right!.Contains(wire);
    public override bool Contains(WireExpr wireExpr)
    {
        if (this == wireExpr) return true;
        return Left!.Contains(wireExpr) || Right!.Contains(wireExpr);
    }
}