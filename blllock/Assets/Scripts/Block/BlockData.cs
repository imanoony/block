using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WindowsMR.Input;

public enum Rotate
{
    None = 0,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270
}

public class BlockData
{
    public int ID, Width, Height;
    public Sprite BlockSprite;
    public List<Vector2Int> Tiles, Grids;
    public List<WireExpr> Ports;

    // 블록이 인스턴스화 될 때 각 Port를 Unique하게 만든다
    public List<WireExpr> Instantiate()
    {
        Dictionary<int, int> lookup = new();
        for (int i = 0; i < Ports.Count; i++) Ports[i] = Subst(Ports[i], lookup);
        return Ports;
    }
    private WireExpr Subst(WireExpr origin, Dictionary<int, int> lookup)
    {
        if (origin is Wire wire)
        {
            if (lookup.ContainsKey(wire.ID)) return GameManager.Instance.Wire.Wires[lookup[wire.ID]];

            Wire pos = new(GameManager.Instance.Wire.GenerateID()), neg = new(-pos.ID);
            if (wire.ID > 0) { lookup[wire.ID] = pos.ID; lookup[-wire.ID] = -pos.ID; }
            else { lookup[wire.ID] = -pos.ID; lookup[-wire.ID] = pos.ID; }
            GameManager.Instance.Wire.AddWire(pos, neg);

            return wire.ID > 0 ? pos : neg;
        }
        if (origin is WireNot wireNot) return new WireNot(Subst(wireNot.Inner, lookup)).Clean();
        if (origin is WireAnd wireAnd) return new WireAnd(Subst(wireAnd.Left, lookup), Subst(wireAnd.Right, lookup));
        if (origin is WireOr wireOr) return new WireOr(Subst(wireOr.Left, lookup), Subst(wireOr.Right, lookup));

        return null;
    }
}
