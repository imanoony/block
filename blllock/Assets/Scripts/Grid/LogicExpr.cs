using System.Collections.Generic;

public abstract class LogicExpr
{
    public abstract override string ToString();
}

public class ConstantExpr : LogicExpr
{
    public bool Value { get; private set; }
    public ConstantExpr(bool value) => Value = value;
    public override string ToString() => Value ? "1" : "0";
}


public class VarExpr : LogicExpr
{
    public string Name { get; private set; }
    public VarExpr(string name) => Name = name;
    public override string ToString() => $"{Name}";
}

public class NotExpr : LogicExpr
{
    public LogicExpr Inner { get; private set; }
    public NotExpr(LogicExpr inner) => Inner = inner;
    public override string ToString() {
        if (Inner is VarExpr || Inner is ConstantExpr)
            return $"~{Inner}";
        return $"~({Inner})";
    }
}

public class AndExpr : LogicExpr
{
    public List<LogicExpr> Operands { get; private set; }
    public AndExpr(List<LogicExpr> operands) => Operands = operands;
    public override string ToString() {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        ) {
            return $"{Operands[0]}{Operands[1]}";
        }
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr) {
            return $"{Operands[0]}({Operands[1]})";
        }
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr) {
            return $"({Operands[0]}){Operands[1]}";
        }
        else {
            return $"({Operands[0]})({Operands[1]})";
        }
    }
}

public class OrExpr : LogicExpr
{
    public List<LogicExpr> Operands { get; private set; }
    public OrExpr(List<LogicExpr> operands) => Operands = operands;
    public override string ToString()
    {
        if (
            (Operands[0] is VarExpr || Operands[0] is ConstantExpr) &&
            (Operands[1] is VarExpr || Operands[1] is ConstantExpr)
        ) {
            return $"{Operands[0]}+{Operands[1]}";
        }
        else if (Operands[0] is VarExpr || Operands[0] is ConstantExpr) {
            return $"{Operands[0]}+({Operands[1]})";
        }
        else if (Operands[1] is VarExpr || Operands[1] is ConstantExpr) {
            return $"({Operands[0]})+{Operands[1]}";
        }
        else {
            return $"({Operands[0]})+({Operands[1]})";
        }
    }
}
