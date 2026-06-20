#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Burst.Intrinsics;

public abstract class LogicExpr
{
    public abstract override string ToString();
    public abstract string ToDataString();
    public abstract override bool Equals(object? obj);
    public abstract override int GetHashCode();
    public abstract CombExpr ToCombExpr();
    public static LogicExpr? Parse(string exprString)
    {
        if (exprString.Length > 1)
        {
            string[] exprs = exprString.Split(";");
            return new CombExpr(
                ParseVar(exprs[0]),
                ParseVar(exprs[1]),
                ParseVar(exprs[2]),
                ParseVar(exprs[3])
            );
        }
        else
        {
            return ParseVar(exprString);
        }
    }
    private static VarExpr? ParseVar(string varString)
    {
        if (varString.Length == 0) return null;
        else return new VarExpr(varString);
    }
}

public class VarExpr : LogicExpr
{
    public string Name { get; private set; }
    public VarExpr(string name) => Name = name;
    public bool IsResisted { get; private set; } = false;
    public void Resist() => IsResisted = true;

    public override string ToString() => Name;
    public override string ToDataString() => Name;
    public override bool Equals(object? obj) 
    {
        if (obj is CombExpr objC)
        {
            if (!Equals(objC.LeftUp)) return false;
            if (!Equals(objC.LeftDown)) return false;
            if (!Equals(objC.RightUp)) return false;
            if (!Equals(objC.RightDown)) return false;
            return true;
        }
        else if (obj is VarExpr objV)
        {
            if (objV.Name != Name) return false;
            if (objV.IsResisted != IsResisted) return false;
            return true;
        }
        else return false;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, IsResisted);
    }
    public override CombExpr ToCombExpr() => new(this, this, this, this);
}

public class CombExpr : LogicExpr
{
    public VarExpr? LeftUp { get; private set; }
    public VarExpr? LeftDown { get; private set; }
    public VarExpr? RightUp { get; private set; }
    public VarExpr? RightDown { get; private set; }
    public CombExpr(
        VarExpr? leftup,
        VarExpr? leftdown,
        VarExpr? rightup,
        VarExpr? rightdown
    )
    {
        LeftUp = leftup;
        LeftDown = leftdown;
        RightUp = rightup;
        RightDown = rightdown;
    }
    public LogicExpr? Clean()
    {
        if (
            LeftUp == null &&
            LeftDown == null &&
            RightUp == null &&
            RightDown == null
        )
        {
            return null;
        }
        else if (
            LeftUp != null &&
            LeftDown != null &&
            RightUp != null &&
            RightDown != null
        )
        {
            if (
                LeftUp.Equals(LeftDown) &&
                LeftUp.Equals(RightUp) &&
                LeftUp.Equals(RightDown)
            )
            {
                return LeftUp;
            }
            else return this;
        }
        
        else return this;
    }
    public override string ToString() => $"{LeftUp}|{RightUp}\n{LeftDown}|{RightDown}";
    public override string ToDataString() => $"{LeftUp};{LeftDown};{RightUp};{RightDown}";
    public override bool Equals(object? obj)
    {
        LogicExpr? cleaned = Clean();
        if (cleaned is VarExpr v) 
            return v.Equals(obj);
        else
        {
            if (obj is not CombExpr)
                return false;
            CombExpr objC = (CombExpr)obj;
            return (
                ((LeftUp == null && objC.LeftUp == null) || (LeftUp != null && LeftUp.Equals(objC.LeftUp))) &&
                ((LeftDown == null && objC.LeftDown == null) || (LeftDown != null && LeftDown.Equals(objC.LeftDown))) &&
                ((RightUp == null && objC.RightUp == null) || (RightUp != null && RightUp.Equals(objC.RightUp))) &&
                ((RightDown == null && objC.RightDown == null) || (RightDown != null && RightDown.Equals(objC.RightDown)))
            );
        }
    }
    public override int GetHashCode()
    {
        LogicExpr? cleaned = Clean();

        if (cleaned is VarExpr v)
            return v.GetHashCode();

        return HashCode.Combine(
            LeftUp,
            LeftDown,
            RightUp,
            RightDown
        );
    }
    public override CombExpr ToCombExpr() => this;
}