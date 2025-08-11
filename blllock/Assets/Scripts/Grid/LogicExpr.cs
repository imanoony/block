using System;
using System.Collections.Generic;

[Serializable]
public abstract class LogicExpr : IEquatable<LogicExpr>, IComparable<LogicExpr>
{
    public abstract override string ToString();

    public abstract bool Equals(LogicExpr other);

    public abstract int CompareTo(LogicExpr other);

    public override bool Equals(object obj)
    {
        if (obj is LogicExpr other)
            return Equals(other);
        return false;
    }

    public abstract override int GetHashCode();
}

// PortExpr
[Serializable]
public class PortExpr : LogicExpr
{
    public string Name;
    public PortExpr(string name) => Name = name;

    public override string ToString() => $"{Name.ToLower()}";

    public override bool Equals(LogicExpr other)
    {
        return other is PortExpr p && Name == p.Name;
    }

    public override int CompareTo(LogicExpr other)
    {
        if (other is PortExpr p)
            return string.Compare(Name, p.Name, StringComparison.Ordinal);
        return GetType().Name.CompareTo(other.GetType().Name);
    }

    public override int GetHashCode() => Name.GetHashCode();
}

// ConstantExpr
[Serializable]
public class ConstantExpr : LogicExpr
{
    public bool Value;
    public ConstantExpr(bool value) => Value = value;

    public override string ToString() => Value ? "1" : "0";

    public override bool Equals(LogicExpr other)
    {
        return other is ConstantExpr c && Value == c.Value;
    }

    public override int CompareTo(LogicExpr other)
    {
        if (other is ConstantExpr c)
            return Value.CompareTo(c.Value);
        return GetType().Name.CompareTo(other.GetType().Name);
    }

    public override int GetHashCode() => Value.GetHashCode();
}

// VarExpr
[Serializable]
public class VarExpr : LogicExpr
{
    public string Name;
    public VarExpr(string name) => Name = name;

    public override string ToString() => Name;

    public override bool Equals(LogicExpr other)
    {
        return other is VarExpr v && Name == v.Name;
    }

    public override int CompareTo(LogicExpr other)
    {
        if (other is VarExpr v)
            return string.Compare(Name, v.Name, StringComparison.Ordinal);
        return GetType().Name.CompareTo(other.GetType().Name);
    }

    public override int GetHashCode() => Name.GetHashCode();
}

// NotExpr
[Serializable]
public class NotExpr : LogicExpr
{
    public LogicExpr Inner;
    public NotExpr(LogicExpr inner) => Inner = inner;

    public override string ToString()
    {
        if (Inner is VarExpr || Inner is ConstantExpr)
            return $"~{Inner}";
        return $"~({Inner})";
    }

    public override bool Equals(LogicExpr other)
    {
        return other is NotExpr n && Inner.Equals(n.Inner);
    }

    public override int CompareTo(LogicExpr other)
    {
        if (other is NotExpr n)
            return Inner.CompareTo(n.Inner);
        return GetType().Name.CompareTo(other.GetType().Name);
    }

    public override int GetHashCode() => Inner.GetHashCode() * 17;
}

// AndExpr (순서 무시 비교)
[Serializable]
public class AndExpr : LogicExpr
{
    public List<LogicExpr> Operands;

    public AndExpr(List<LogicExpr> operands)
    {
        Operands = new List<LogicExpr>(operands);
        Operands.Sort(); // 항상 정렬 상태 유지 권장
    }

    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        )
        {
            return $"{Operands[0]}{Operands[1]}";
        }
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr)
        {
            return $"{Operands[0]}({Operands[1]})";
        }
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        {
            return $"({Operands[0]}){Operands[1]}";
        }
        else
        {
            return $"({Operands[0]})({Operands[1]})";
        }
    }

    public override bool Equals(LogicExpr other)
    {
        if (other is not AndExpr a) return false;
        if (Operands.Count != a.Operands.Count) return false;

        // 정렬 후 순서 비교
        for (int i = 0; i < Operands.Count; i++)
        {
            if (!Operands[i].Equals(a.Operands[i])) return false;
        }
        return true;
    }

    public override int CompareTo(LogicExpr other)
    {
        if (other is not AndExpr a)
            return GetType().Name.CompareTo(other.GetType().Name);

        int minCount = Math.Min(Operands.Count, a.Operands.Count);
        for (int i = 0; i < minCount; i++)
        {
            int cmp = Operands[i].CompareTo(a.Operands[i]);
            if (cmp != 0) return cmp;
        }
        return Operands.Count.CompareTo(a.Operands.Count);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var op in Operands)
            hash = hash * 31 + op.GetHashCode();
        return hash;
    }
}

// OrExpr (순서 무시 비교)
[Serializable]
public class OrExpr : LogicExpr
{
    public List<LogicExpr> Operands;

    public OrExpr(List<LogicExpr> operands)
    {
        Operands = new List<LogicExpr>(operands);
        Operands.Sort(); // 항상 정렬 상태 유지 권장
    }

    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        )
        {
            return $"{Operands[0]}+{Operands[1]}";
        }
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr)
        {
            return $"{Operands[0]}+({Operands[1]})";
        }
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        {
            return $"({Operands[0]})+{Operands[1]}";
        }
        else
        {
            return $"({Operands[0]})+({Operands[1]})";
        }
    }

    public override bool Equals(LogicExpr other)
    {
        if (other is not OrExpr o) return false;
        if (Operands.Count != o.Operands.Count) return false;

        // 정렬 후 순서 비교
        for (int i = 0; i < Operands.Count; i++)
        {
            if (!Operands[i].Equals(o.Operands[i])) return false;
        }
        return true;
    }

    public override int CompareTo(LogicExpr other)
    {
        if (other is not OrExpr o)
            return GetType().Name.CompareTo(other.GetType().Name);

        int minCount = Math.Min(Operands.Count, o.Operands.Count);
        for (int i = 0; i < minCount; i++)
        {
            int cmp = Operands[i].CompareTo(o.Operands[i]);
            if (cmp != 0) return cmp;
        }
        return Operands.Count.CompareTo(o.Operands.Count);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var op in Operands)
            hash = hash * 31 + op.GetHashCode();
        return hash;
    }
}
