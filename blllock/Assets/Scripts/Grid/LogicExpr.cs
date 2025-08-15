using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class LogicExpr : IEquatable<LogicExpr>
{
    public abstract override string ToString();
    public abstract bool Equals(LogicExpr other);
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
    public PortExpr() { }
    public PortExpr(string name) => Name = name;
    public override string ToString() => $"<color=#717171>{Name.ToLower()}</color>";
    public override bool Equals(LogicExpr other)
    {
        return other is PortExpr p && Name == p.Name;
    }
    public override int GetHashCode() => Name.GetHashCode();
}

// ConstantExpr
[Serializable]
public class ConstantExpr : LogicExpr
{
    public bool Value;
    public ConstantExpr() {}
    public ConstantExpr(bool value) => Value = value;
    public override string ToString() => Value ? "1" : "0";
    public override bool Equals(LogicExpr other)
    {
        if (other is NotExpr not && not.Inner is NotExpr innerNot)
        {
            return innerNot.Inner is ConstantExpr c && Value == c.Value;
        }
        return other is ConstantExpr oc && Value == oc.Value;
    }
    public override int GetHashCode() => Value.GetHashCode();
}

// VarExpr
[Serializable]
public class VarExpr : LogicExpr
{
    public string Name;
    public VarExpr() { }
    public VarExpr(string name) => Name = name;
    public override string ToString() => Name;
    public override bool Equals(LogicExpr other)
    {
        if (other is NotExpr not && not.Inner is NotExpr innerNot)
        {
            return innerNot.Inner is VarExpr v && Name == v.Name;
        }
        return other is VarExpr ov && Name == ov.Name;
    }
    public override int GetHashCode() => Name.GetHashCode();
}

// NotExpr
[Serializable]
public class NotExpr : LogicExpr
{
    [SerializeReference, SubclassSelector]
    public LogicExpr Inner;
    public NotExpr() => Inner = null;
    public NotExpr(LogicExpr inner) => Inner = inner;
    public override string ToString()
    {
        if (Inner is VarExpr || Inner is ConstantExpr || Inner is PortExpr)
            return $"~{Inner}";
        else if (Inner is NotExpr not)
            return not.Inner.ToString();
        return $"~({Inner})";
    }
    public override bool Equals(LogicExpr other)
    {
        if (other is NotExpr n) return Inner.Equals(n.Inner);
        if (Inner is NotExpr innerNot) return innerNot.Inner.Equals(other);
        return false;
    }
    public override int GetHashCode() => Inner.GetHashCode() * 17;
}

// AndExpr 
[Serializable]
public class AndExpr : LogicExpr
{
    [SerializeReference, SubclassSelector]
    public List<LogicExpr> Operands;
    public AndExpr() => Operands = new List<LogicExpr>();
    public AndExpr(List<LogicExpr> operands)
    {
        Operands = new List<LogicExpr>(operands);
        // 정렬하지 않음: 순서까지 비교
    }
    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr || Operands[0] is PortExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr || Operands[1] is PortExpr)
        )
        {
            return $"{Operands[0]}{Operands[1]}";
        }
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr || Operands[0] is PortExpr)
        {
            return $"{Operands[0]}({Operands[1]})";
        }
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr || Operands[1] is PortExpr)
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

        // 순서까지 비교
        for (int i = 0; i < Operands.Count; i++)
        {
            if (!Operands[i].Equals(a.Operands[i])) return false;
        }
        return true;
    }
    public override int GetHashCode()
    {
        int hash = 17;
        // 순서까지 반영
        foreach (var op in Operands)
            hash = hash * 31 + op.GetHashCode();
        return hash;
    }
}

// OrExpr (순서 무시 비교)
[Serializable]
public class OrExpr : LogicExpr
{
    [SerializeReference, SubclassSelector]
    public List<LogicExpr> Operands;
    public OrExpr() => Operands = new List<LogicExpr>();
    public OrExpr(List<LogicExpr> operands)
    {
        Operands = new List<LogicExpr>(operands);
        // 정렬하지 않음: 순서까지 비교
    }
    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr || Operands[0] is PortExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr || Operands[1] is PortExpr)
        )
        {
            return $"{Operands[0]}+{Operands[1]}";
        }
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr || Operands[0] is PortExpr)
        {
            return $"{Operands[0]}+({Operands[1]})";
        }
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr || Operands[1] is PortExpr)
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

        // 순서까지 비교
        for (int i = 0; i < Operands.Count; i++)
        {
            if (!Operands[i].Equals(o.Operands[i])) return false;
        }
        return true;
    }
    public override int GetHashCode()
    {
        int hash = 17;
        // 순서까지 반영
        foreach (var op in Operands)
            hash = hash * 31 + op.GetHashCode();
        return hash;
    }
}
