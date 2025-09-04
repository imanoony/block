using System.Collections.Generic;
using UnityEngine;

public enum Rotate { Null = -1, None = 0, Rotate90 = 90, Rotate180 = 180, Rotate270 = 270 }

public class BlockData
{
    #region ID & Shape
    public int ID { get; private set; }
    public void SetID(int id) => ID = id;

    private int _width;
    public int Width => GetWidth();
    public void SetWidth(int width) => _width = width;
    public int GetWidth(Rotate rotate = Rotate.Null)
    {
        Rotate refRotate = rotate == Rotate.Null ? BlockRotate : rotate;
        return (refRotate == Rotate.None || refRotate == Rotate.Rotate180) ? _width : _height;
    }

    private int _height;
    public int Height => GetHeight();
    public void SetHeight(int height) => _height = height;
    public int GetHeight(Rotate rotate = Rotate.Null)
    {
        Rotate refRotate = rotate == Rotate.Null ? BlockRotate : rotate;
        return (refRotate == Rotate.None || refRotate == Rotate.Rotate180) ? _height : _width;
    }

    public Sprite BlockSprite { get; private set; }
    public void SetSprite(Sprite sprite) => BlockSprite = sprite;
    public Rotate BlockRotate { get; private set; } = Rotate.None;
    public Rotate Rotation()
    {
        for (int i = 0; i < Tiles.Count; i++) Tiles[i] = new(Tiles[i].y, GetHeight() - 1 - Tiles[i].x);
        for (int i = 0; i < Grids.Count; i++) Grids[i] = new(Grids[i].y, GetHeight() - Grids[i].x);

        if (BlockRotate == Rotate.Rotate270) BlockRotate = Rotate.None;
        else BlockRotate += (int)Rotate.Rotate90;
        return BlockRotate;
    }
    public bool BlockFlipY { get; private set; } = false;
    public bool FlipY()
    {
        for (int i = 0; i < Tiles.Count; i++) Tiles[i] = new(Tiles[i].x, GetWidth() - 1 - Tiles[i].y);
        for (int i = 0; i < Grids.Count; i++) Grids[i] = new(Grids[i].x, GetWidth() - Grids[i].y);

        BlockFlipY = !BlockFlipY;
        return BlockFlipY;
    }
    #endregion

    #region Grid & Wire
    private List<Vector2Int> _tiles;
    public List<Vector2Int> Tiles;
    public void SetTiles(List<Vector2Int> tiles) => _tiles = tiles;

    private List<Vector2Int> _grids;
    public List<Vector2Int> Grids;
    public void SetGrids(List<Vector2Int> grids) => _grids = grids;

    public List<WireExpr> Ports { get; private set; }
    public void SetPorts(List<WireExpr> ports) => Ports = ports;
    public List<int> PortIds { get; private set; }
    #endregion

    // 블록이 인스턴스화 될 때 각 Port를 Unique하게 만든다
    public List<WireExpr> Instantiate()
    {
        Tiles = _tiles;
        Grids = _grids;
        PortIds = new();

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
            PortIds.Add(pos.ID);

            return wire.ID > 0 ? pos : neg;
        }
        if (origin is WireNot wireNot) return new WireNot(Subst(wireNot.Inner, lookup)).Clean();
        if (origin is WireAnd wireAnd) return new WireAnd(Subst(wireAnd.Left, lookup), Subst(wireAnd.Right, lookup));
        if (origin is WireOr wireOr) return new WireOr(Subst(wireOr.Left, lookup), Subst(wireOr.Right, lookup));

        return null;
    }
}
