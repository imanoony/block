using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Rotate 
{ 
    Null = -1, 
    None = 0, 
    Rotate90 = 90, 
    Rotate180 = 180, 
    Rotate270 = 270 
}

public class BlockData
{
    public BlockData() { }
    public BlockData(BlockData blockData)
    {
        ID = blockData.ID;
        _width = blockData._width;
        _height = blockData._height;
        _tiles = new(blockData._tiles);
        _grids = new(blockData._grids);
        Ports = new(blockData.Ports);
        TagPos = new(blockData.TagPos.x, blockData.TagPos.y);
        BlockRotate = Rotate.None;
        BlockFlipX = false;
        HasSpike = false;
    }

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
    
    public Vector2 TagPos { get; private set; } = new(0, 0);
    public void SetTagPos(Vector2 tagPos) => TagPos = tagPos;

    public Rotate BlockRotate { get; private set; } = Rotate.None;
    public Rotate RotateCW()
    {
        int i;
        for (i = 0; i < Tiles.Count; i++) 
        {
            Tiles[i] = new(Tiles[i].y, GetHeight() - 1 - Tiles[i].x);
        }
        for (i = 0; i < Grids.Count; i++) 
        {
            Grids[i] = new(Grids[i].y, GetHeight() - Grids[i].x);
        }

        if (HasSpike)
        {
            for (i = 0; i < SpikeTiles.Count; i++)
                SpikeTiles[i] = new(SpikeTiles[i].y, GetHeight() - 1 - SpikeTiles[i].x);
        }

        if (BlockRotate == Rotate.Rotate270) BlockRotate = Rotate.None;
        else BlockRotate += (int)Rotate.Rotate90;
        return BlockRotate;
    }
    public Rotate RotateCCW()
    {
        int i;
        for (i = 0; i < Tiles.Count; i++)
        {
            Tiles[i] = new(GetWidth() - 1 - Tiles[i].y, Tiles[i].x);
        }
        for (i = 0; i < Grids.Count; i++)
        {
            Grids[i] = new(GetWidth() - Grids[i].y, Grids[i].x);
        }

        if (HasSpike)
        {
            for (i = 0; i < SpikeTiles.Count; i++)
                SpikeTiles[i] = new(GetWidth() - 1 - SpikeTiles[i].y, SpikeTiles[i].x);
        }

        if (BlockRotate == Rotate.None) BlockRotate = Rotate.Rotate270;
        else BlockRotate -= (int)Rotate.Rotate90;
        return BlockRotate;
    }

    public bool BlockFlipX { get; private set; } = false;
    public bool FlipX()
    {
        int i;
        for (i = 0; i < Tiles.Count; i++) Tiles[i] = new(Tiles[i].x, GetWidth() - 1 - Tiles[i].y);
        for (i = 0; i < Grids.Count; i++) Grids[i] = new(Grids[i].x, GetWidth() - Grids[i].y);

        if (HasSpike)
        {
            for (i = 0; i < SpikeTiles.Count; i++)
                SpikeTiles[i] = new(SpikeTiles[i].x, GetWidth() - 1 - SpikeTiles[i].y);
        }

        BlockFlipX = !BlockFlipX;
        return BlockFlipX;
    }

    public bool BlockFlipY { get; private set; } = false;
    public bool FlipY()
    {
        int i;
        for (i = 0; i < Tiles.Count; i++) Tiles[i] = new(GetHeight() - 1 - Tiles[i].x, Tiles[i].y);
        for (i = 0; i < Grids.Count; i++) Grids[i] = new(GetHeight() - Grids[i].x, Grids[i].y);

        if (HasSpike)
        {
            for (i = 0; i < SpikeTiles.Count; i++)
                SpikeTiles[i] = new(GetHeight() - 1 - SpikeTiles[i].x, SpikeTiles[i].y);
        }

        BlockFlipY = !BlockFlipY;
        return BlockFlipY;
    }

    public bool HasSpike { get; private set; } = false;
    private List<Vector2Int> _spikeTiles;
    public List<Vector2Int> SpikeTiles;
    public void SetSpike()
    {
        HashSet<Vector2Int> tileset = _tiles.ToHashSet();
        HashSet<Vector2Int> spikeset = new();

        foreach (Vector2Int pos in tileset)
        {
            if (!tileset.Contains(new(pos.x - 1, pos.y)))
                spikeset.Add(new(pos.x - 1, pos.y));
            if (!tileset.Contains(new(pos.x + 1, pos.y)))
                spikeset.Add(new(pos.x + 1, pos.y));
            if (!tileset.Contains(new(pos.x, pos.y - 1)))
                spikeset.Add(new(pos.x, pos.y - 1));
            if (!tileset.Contains(new(pos.x, pos.y + 1)))
                spikeset.Add(new(pos.x, pos.y + 1));
        }

        _spikeTiles = spikeset.ToList();
        SpikeTiles = _spikeTiles;
        HasSpike = true;
    }
    #endregion

    #region Grid & Wire
    private List<Vector2Int> _tiles;
    public List<Vector2Int> Tiles;
    public void SetTiles(List<Vector2Int> tiles) => _tiles = tiles;

    private List<Vector2Int> _grids;
    public List<Vector2Int> Grids;
    public void SetGrids(List<Vector2Int> grids) => _grids = grids;

    public List<PortExpr> Ports { get; private set; }
    public void SetPorts(List<PortExpr> ports) => Ports = ports;
    public List<int> WireIds { get; private set; }
    #endregion

    // 블록이 인스턴스화 될 때 각 Port의 Wire들을 Unique하게 만든다
    public List<PortExpr> Instantiate()
    {
        Tiles = _tiles;
        Grids = _grids;
        WireIds = new();

        Dictionary<(string, int), int> lookup = new();
        for (int i = 0; i < Ports.Count; i++) Ports[i] = Subst(Ports[i], lookup);
        return Ports;
    }
    private PortExpr Subst(PortExpr port, Dictionary<(string, int), int> lookup)
    {
        if (port is PortVar var)
        {
            return SubstVar(var, lookup);
        } 
        else if (port is PortVert vert)
        {
            return new PortVert(SubstVar(vert.Up, lookup), SubstVar(vert.Down, lookup));
        }
        else if (port is PortHorz horz)
        {
            return new PortHorz(SubstVar(horz.Left, lookup), SubstVar(horz.Right, lookup));
        }
        return null;
    }
    private PortVar SubstVar(PortVar port, Dictionary<(string, int), int> lookup)
    {
        PortVar newPort = new(
            port.Name,
            SubstWire(port, 0, lookup),
            SubstWire(port, 1, lookup),
            SubstWire(port, 2, lookup),
            SubstWire(port, 3, lookup)
        );

        return newPort;
    }
    private Wire SubstWire(PortVar port, int param, Dictionary<(string, int), int> lookup)
    {
        //Debug.Log($"[SubstWire] block: ID={ID}, Wire={wire}");
        if (lookup.ContainsKey((port.Name, param)))
        {
            Wire result = GameManager.Instance.Wire.Wires[lookup[(port.Name, param)]];
            Debug.Log($"[SubstWire|Result] block: ID={ID}, Result Wire={result}");
            return GameManager.Instance.Wire.Wires[lookup[(port.Name, param)]];
        }
        
        Wire newWire = new(GameManager.Instance.Wire.GenerateID());
        lookup[(port.Name, param)] = newWire.ID;
        GameManager.Instance.Wire.AddWire(newWire);
        WireIds.Add(newWire.ID);

        Debug.Log($"[SubstWire|Result] block: ID={ID}, Result Wire={newWire}");
        return newWire;
    }
}
