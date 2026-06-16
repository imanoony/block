#nullable enable

using System;
using System.Runtime.CompilerServices;

// -------------------------------------------------------
// [ Port ]
// -------------------------------------------------------
public abstract class PortExpr
{
    public abstract void Dispose();
    private LogicExpr? _cache;
    public LogicExpr? Cache
    {
        get => _cache;
        set
        {
            if (_cache == value) return;
            _cache = value;
            OnCacheChanged?.Invoke(this);
        }
    }
    public event Action<PortExpr>? OnCacheChanged;
    protected void SubscribeWire(Wire wire)
    {
        wire.OnCacheChanged += OnWireCacheChanged;
    }
    protected void UnsubscribeWire(Wire wire)
    {
        wire.OnCacheChanged -= OnWireCacheChanged;
    }
    private void OnWireCacheChanged(Wire _)
    {
        Cache = GameManager.Instance.Wire.Eval(this);
    }

    public static PortExpr? Parse(string portString)
    {
        if (string.IsNullOrEmpty(portString)) return null;

        if (portString.Length == 1) return Parse(portString[0]);
        else
        {
            if (portString[1] == Utils.VERT) 
                return new PortVert(Parse(portString[0]), Parse(portString[2]));
            else if (portString[1] == Utils.HORZ)
                return new PortHorz(Parse(portString[0]), Parse(portString[2]));
            else return null;
        }
    }
    private static PortVar? Parse(char c)
    {
        if (c == 'n') return null;
        else return new(c.ToString());
    }

    // Wire
    public abstract Wire? LeftUp { get; }
    public abstract Wire? LeftDown { get; }
    public abstract Wire? RightUp { get; }
    public abstract Wire? RightDown { get; }

    // Rotate
    public abstract PortExpr RotateCW();
    public abstract PortExpr RotateCCW();

    // Flip
    public abstract PortExpr FlipX();
    public abstract PortExpr FlipY();
}
public class PortVar : PortExpr
{
    public int ID { get; private set; }
    public string Name { get; private set; }
    public PortVar(string name)
    {
        Name = name;
        _leftUp = new();
        _leftDown = new();
        _rightUp = new();
        _rightDown = new();
    }

    public PortVar(
        string name,
        Wire leftUp, 
        Wire leftDown, 
        Wire rightUp, 
        Wire rightDown
    )
    {
        Name = name;
        _leftUp = leftUp;
        _leftDown = leftDown;
        _rightUp = rightUp;
        _rightDown = rightDown;

        SubscribeWire(LeftUp);
        SubscribeWire(LeftDown);
        SubscribeWire(RightUp);
        SubscribeWire(RightDown);
    }

    private bool _disposed = false;
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnsubscribeWire(LeftUp);
        UnsubscribeWire(LeftDown);
        UnsubscribeWire(RightUp);
        UnsubscribeWire(RightDown);
    }

    // Wire
    private Wire _leftUp, _leftDown, _rightUp, _rightDown;
    public override Wire LeftUp => _leftUp;
    public override Wire LeftDown => _leftDown;
    public override Wire RightUp => _rightUp;
    public override Wire RightDown => _rightDown;

    // Rotate
    public override PortExpr RotateCW() => this;
    public override PortExpr RotateCCW() => this;

    // Flip
    public override PortExpr FlipX() => this;
    public override PortExpr FlipY() => this;
}
public class PortVert : PortExpr
{
    public PortVar? Up { get; private set; }
    public PortVar? Down { get; private set; }
    public PortVert(PortVar? up, PortVar? down)
    {
        Up = up;
        Down = down;

        if (Up != null) {
            SubscribeWire(LeftUp!);
            SubscribeWire(RightUp!);
        }
        if (Down != null)
        {
            SubscribeWire(LeftDown!);
            SubscribeWire(RightDown!);
        }
    }

    private bool _disposed = false;
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Up != null)
        {
            UnsubscribeWire(LeftUp!);
            UnsubscribeWire(RightUp!);
        }
        if (Down != null)
        {
            UnsubscribeWire(LeftDown!);
            UnsubscribeWire(RightDown!);
        }
    }

    // Wire
    public override Wire? LeftUp => Up?.LeftUp;
    public override Wire? LeftDown => Down?.LeftDown;
    public override Wire? RightUp => Up?.RightUp;
    public override Wire? RightDown => Down?.RightDown;

    // Rotate
    public override PortExpr RotateCW()
    {
        Dispose();
        return new PortHorz(Down, Up);
    }
    public override PortExpr RotateCCW()
    {
        Dispose();
        return new PortHorz(Up, Down);
    }

    // Flip
    public override PortExpr FlipX()
    {
        return this;
    }
    public override PortExpr FlipY()
    {
        Dispose();
        return new PortVert(Down, Up);
    }
}
public class PortHorz : PortExpr
{
    public PortVar? Left { get; private set; }
    public PortVar? Right { get; private set; }
    public PortHorz(PortVar? left, PortVar? right)
    {
        Left = left;
        Right = right;

        if (Left != null)
        {
            SubscribeWire(LeftUp!);
            SubscribeWire(LeftDown!);
        }
        if (Right != null)
        {
            SubscribeWire(RightUp!);
            SubscribeWire(RightDown!);    
        }
    }

    private bool _disposed = false;
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Left != null)
        {
            UnsubscribeWire(LeftUp!);
            UnsubscribeWire(LeftDown!);
        }
        if (Right != null)
        {
            UnsubscribeWire(RightUp!);
            UnsubscribeWire(RightDown!);
        }
    }

    // Wire
    public override Wire? LeftUp => Left?.LeftUp;
    public override Wire? LeftDown => Left?.LeftDown;
    public override Wire? RightUp => Right?.RightUp;
    public override Wire? RightDown => Right?.RightDown;

    // Rotate
    public override PortExpr RotateCW()
    {
        Dispose();
        return new PortVert(Left, Right);
    }
    public override PortExpr RotateCCW()
    {
        Dispose();
        return new PortVert(Right, Left);
    }

    // Flip
    public override PortExpr FlipX()
    {
        Dispose();
        return new PortHorz(Right, Left);
    }
    public override PortExpr FlipY()
    {
        return this;
    }
}


// -------------------------------------------------------
// [ Wire ]
// -------------------------------------------------------
public class Wire
{
    public int ID = -1;
    public string Name;
    public bool Updated = false;

    private VarExpr? _cache;
    public VarExpr? Cache
    {
        get => _cache;
        set
        {
            if (_cache == value) return;
            _cache = value;
            OnCacheChanged?.Invoke(this);
        }
    }
    public event Action<Wire>? OnCacheChanged;

    public Wire() // invalid dummy wire creation
    {
        ID = -1;
        Name = "";
        Updated = false;
        Cache = null;
    }
    public Wire(int id, int parent = 0)
    {
        ID = id;
        Name = "";
        Updated = false;
        Cache = null;
    }
    public Wire(Wire wire)
    {
        ID = wire.ID;
        Name = wire.Name;
        Updated = wire.Updated;
        Cache = null;
    }
    public void Init(Wire wire)
    {
        ID = wire.ID;
        Name = wire.Name;
        Updated = wire.Updated;
        Cache = null;
    }
    public override string ToString() => ID.ToString();
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}