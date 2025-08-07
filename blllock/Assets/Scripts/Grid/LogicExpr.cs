using System.Collections.Generic;

public abstract class LogicExpr
{
    public abstract override string ToString();
}

public class ConstantExpr : LogicExpr
{
    public bool Value { get; private set; }
    public ConstantExpr(bool value) => Value = value;
    public override string ToString() => $"({(Value ? "1" : "0")})";
}


public class VarExpr : LogicExpr
{
    public string Name { get; private set; }
    public VarExpr(string name) => Name = name;
    public override string ToString() => $"({Name})";
}

public class NotExpr : LogicExpr
{
    public LogicExpr Inner { get; private set; }
    public NotExpr(LogicExpr inner) => Inner = inner;
    public override string ToString() => $"(~{Inner})";
}

public class AndExpr : LogicExpr
{
    public List<LogicExpr> Operands { get; private set; }
    public AndExpr(List<LogicExpr> operands) => Operands = operands;
    public override string ToString() => $"({string.Join(" + ", Operands)})";
}

public class OrExpr : LogicExpr
{
    public List<LogicExpr> Operands { get; private set; }
    public OrExpr(List<LogicExpr> operands) => Operands = operands;
    public override string ToString() => $"({string.Join(" * ", Operands)})";
}
