using System.Collections.Generic;

public abstract class LogicExpr
{
    public abstract override string ToString();
}

public class ConstantExpr : LogicExpr
{
    public bool Value;
    public ConstantExpr(bool value=false) => Value = value;
    public override string ToString() => Value ? "1" : "0";
}

public class VarExpr : LogicExpr
{
    public string Name;
    public VarExpr(string name = "") => Name = name;
    public override string ToString() => Name;
}

public class NotExpr : LogicExpr
{
    public LogicExpr Inner;
    public NotExpr(LogicExpr inner=null) => Inner = inner;
    public override string ToString()
    {
        if (Inner is VarExpr || Inner is ConstantExpr)
            return $"~{Inner}";
        else if (Inner is NotExpr not)
            return not.Inner.ToString();
        return $"~({Inner})";
    }
}

public class AndExpr : LogicExpr
{
    public List<LogicExpr> Operands;
    public AndExpr(List<LogicExpr> operands=null) => Operands = operands ?? new List<LogicExpr>();
    public AndExpr(LogicExpr left, LogicExpr right) => Operands = new List<LogicExpr> { left, right };
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
}

public class OrExpr : LogicExpr
{
    public List<LogicExpr> Operands;
    public OrExpr(List<LogicExpr> operands=null) => Operands = operands ?? new List<LogicExpr>();
    public OrExpr(LogicExpr left, LogicExpr right) => Operands = new List<LogicExpr> { left, right };
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
}
