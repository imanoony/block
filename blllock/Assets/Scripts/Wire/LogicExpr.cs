#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

public abstract class LogicExpr
{
    public abstract override string ToString();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
    public static LogicExpr Parse(string exprString)
    {
        if (string.IsNullOrEmpty(exprString)) 
            throw new FormatException("LogicExpr.Parse()");

        if (exprString[0] == Utils.NOT)
        {
            string inner = exprString[1..];
            if (Utils.IsWrappedByParentheses(inner)) inner = inner[1..^1];
            return new NotExpr(Parse(inner)).Clean();
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
                LogicExpr left = Parse(exprString[0..i]);
                LogicExpr right = Parse(exprString[(i + 1)..]);
                return c == Utils.AND ? new AndExpr(left, right) : new OrExpr(left, right);
            }
        }

        if (exprString.Length == 1)
        {
            if (char.IsDigit(exprString[0])) return new ConstantExpr(int.Parse(exprString));
            else return new VarExpr(exprString);
        }

        throw new InvalidDataException("LogicExpr.Parse()");
    }
}

public class ConstantExpr : LogicExpr
{
    public bool Value;
    public ConstantExpr(bool value = false) => Value = value;
    public ConstantExpr(int value) => Value = value != 0;
    public override string ToString() => Value ? "1" : "0";
    public override bool Equals(object? obj) => obj is ConstantExpr c && Value == c.Value;
    public override int GetHashCode() => Value.GetHashCode();
}

public class VarExpr : LogicExpr
{
    public string Name;
    public VarExpr(string name) => Name = name;
    public override string ToString() => Name;
    public override bool Equals(object? obj) => obj is VarExpr v && Name == v.Name;
    public override int GetHashCode() => Name.GetHashCode();
}

public class NotExpr : LogicExpr
{
    public LogicExpr Inner;
    public NotExpr(LogicExpr inner) => Inner = inner;
    public LogicExpr Clean() // 이중 부정을 제거하기 위함
    {
        if (Inner is NotExpr innerNot) return innerNot.Inner;
        return this;
    }
    public override string ToString()
    {
        if (Inner is VarExpr || Inner is ConstantExpr)
            return $"~{Inner}";
        else if (Inner is NotExpr not)
            return not.Inner.ToString();
        return $"~({Inner})";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not NotExpr n) return false;
        return Inner.Equals(n.Inner);
    }
    public override int GetHashCode() => Inner.GetHashCode() * 17;
}

public class AndExpr : LogicExpr
{
    public List<LogicExpr> Operands;
    public LogicExpr Left { get; private set; }
    public LogicExpr Right { get; private set; }
    public AndExpr(List<LogicExpr> operands)
    {
        Operands = operands;
        Left = Operands[0];
        Right = Operands[1];
    }
    public AndExpr(LogicExpr left, LogicExpr right)
    {
        Operands = new List<LogicExpr> { left, right };
        Left = left;
        Right = right;
    }
    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        ) return $"{Operands[0]}{Operands[1]}";
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr) return $"{Operands[0]}({Operands[1]})";
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr) return $"({Operands[0]}){Operands[1]}";
        else return $"({Operands[0]})({Operands[1]})";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not AndExpr a) return false;
        return Left.Equals(a.Left) && Right.Equals(a.Right);
    }
    public override int GetHashCode() => Left.GetHashCode() * 31 + Right.GetHashCode();
}

public class OrExpr : LogicExpr
{
    public List<LogicExpr> Operands;
    public LogicExpr Left { get; private set; }
    public LogicExpr Right { get; private set; }
    public OrExpr(List<LogicExpr> operands)
    {
        Operands = operands;
        Left = Operands[0];
        Right = Operands[1];
    }
    public OrExpr(LogicExpr left, LogicExpr right)
    {
        Operands = new List<LogicExpr> { left, right };
        Left = left;
        Right = right;
    }
    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        ) return $"{Operands[0]}+{Operands[1]}";
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr) return $"{Operands[0]}+({Operands[1]})";
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr) return $"({Operands[0]})+{Operands[1]}";
        else return $"({Operands[0]})+({Operands[1]})";
    }
    public override bool Equals(object? obj)
    {
        if (obj is not OrExpr o) return false;
        return Left.Equals(o.Left) && Right.Equals(o.Right);
    }
    public override int GetHashCode() => Left.GetHashCode() * 31 + Right.GetHashCode();
}
