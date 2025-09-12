#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public enum TileType { Empty, Occupied }
public enum GridType { Null, Input, Output }

public class Tile
{
    public Vector2Int Pos { get; private set; }
    public TileType Type { get; private set; }
    public Tile(Vector2Int pos, TileType type = TileType.Empty) { Pos = pos; Type = type; }
    public void SetType(TileType type) => Type = type;
}

public class Grid
{
    public Vector2Int Pos { get; private set; }
    public GridType Type { get; private set; }
    public LogicExpr? Expr { get; private set; } = null; // input, output과 관련된 상수 LogicExpr
    public List<WireExpr> Ports { get; private set; } = new(); // 인접한 Ports들 (최대 4개)
    public event Action? OnPortsChanged;
    public Grid(Vector2Int pos, GridType type = GridType.Null) { Pos = pos; Type = type; }
    public void SetType(GridType type) => Type = type;
    public void SetExpr(LogicExpr? expr) => Expr = expr;
    public bool AddPort(WireExpr port)
    {
        if (Ports.Count >= Utils.MAX_PORT) return false;
        Ports.Add(port);
        OnPortsChanged?.Invoke();
        return true;
    }
    public void RemovePort(WireExpr port)
    {
        Ports.Remove(port);
        OnPortsChanged?.Invoke();
    }
}

public class GridManager : MonoBehaviour
{
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;
        // TODO
        initialized = true;
    }
    void Update() // for debugging
    {
        if (Input.GetMouseButtonUp(1))
        {
            string debugText = $@"
[현재 WireDict] {GameManager.Instance.Wire.StringOfWireDict()}
[현재 Wires] {GameManager.Instance.Wire.StringOfWires()}
[현재 WireLogic] {GameManager.Instance.Wire.StringOfWireLogic()}
";
            Debug.Log(debugText);
        }
    }

    public Grid[,]? Grids { get; private set; } = null;
    public Tile[,]? Tiles { get; private set; } = null;
    private TilePlacer? _tilePlacer;
    public TilePlacer TilePlacer
    {
        get
        {
            if (_tilePlacer == null)
            {
                _tilePlacer = gameObject.GetComponent<TilePlacer>();
                if (_tilePlacer == null)
                    _tilePlacer = gameObject.AddComponent<TilePlacer>();
            }
            return _tilePlacer;
        }
    }
    private BlockPlacer? _blockPlacer;
    public BlockPlacer BlockPlacer
    {
        get
        {
            if (_blockPlacer == null)
            {
                _blockPlacer = gameObject.GetComponent<BlockPlacer>();
                if (_blockPlacer == null)
                    _blockPlacer = gameObject.AddComponent<BlockPlacer>();
            }
            return _blockPlacer;
        }
    }

    public void InitStage(int id) => InitStage(GameManager.Instance.StageLibrary[id]);

    public void InitStage(StageData stage)
    {
        int width = stage.Width, height = stage.Height;

        Grids = new Grid[height + 1, width + 1];
        for (int x = 0; x < height + 1; x++)
            for (int y = 0; y < width + 1; y++)
                Grids[x, y] = new Grid(new(x, y));

        foreach (var (pos, expr) in stage.Inputs)
        {
            Grids[pos.x, pos.y].SetType(GridType.Input);
            Grids[pos.x, pos.y].SetExpr(expr);
        }

        foreach (var (pos, expr) in stage.Outputs)
        {
            Grids[pos.x, pos.y].SetType(GridType.Output);
            Grids[pos.x, pos.y].SetExpr(expr);
        }

        Tiles = new Tile[height, width];
        for (int x = 0; x < height; x++)
            for (int y = 0; y < width; y++)
                Tiles[x, y] = new Tile(new(x, y));

        // 스테이지에 해당하는 타일 배치
        // 스테이지에 해당하는 블록 배치
        TilePlacer.PlaceTiles(width, height);
        BlockPlacer.PlaceBlocks(stage);

        // TODO
    }

    public void RemoveCurrentStage()
    {
        if (Tiles != null) Tiles = null;
        if (Grids != null) Grids = null;

        TilePlacer.RemoveTiles();
        BlockPlacer.RemoveBlocks();
    }

    private bool IsEmptyTile(int x, int y)
    {
        if (Tiles == null) return false;
        if (!IsInTileBounds(x, y)) return false;
        return Tiles[x, y].Type == TileType.Empty;
    }
    private bool IsInTileBounds(int x, int y)
    {
        if (Tiles == null) return false;
        return x >= 0 && y >= 0 && x < Tiles.GetLength(0) && y < Tiles.GetLength(1);
    }
    public LogicExpr? GetGridExpr(int x, int y)
    {
        if (Grids == null) return null;
        return Grids[x, y].Expr;
    }
    public LogicExpr? GetGridCacheExpr(int x, int y)
    {
        if (Grids == null) return null;
        if (Grids[x, y].Ports.Count == 0) return null;
        return GameManager.Instance.Wire.EvalCache(Grids[x, y].Ports[0]);
    }
    public GridType GetGridType(int x, int y)
    {
        if (Grids == null) return GridType.Null;
        return Grids[x, y].Type;
    }

    #region Block Placement
    private List<BlockInstance> invalids = new();
    public void AddInvalid(BlockInstance instance)
    {
        if (instance.Valid) return;
        invalids.Add(instance);
    }
    public void RemoveInvalid(BlockInstance instance)
    {
        if (!instance.Valid) return;
        if (invalids.Contains(instance)) invalids.Remove(instance);
    }
    private void CheckInvalids()
    {
        foreach (BlockInstance invalid in invalids) invalid.Check(this);
    }

    public bool PlaceBlock(BlockData block, Vector2Int baseTile)
    {
        // 점유된 타일을 Occupied 상태로 변경한다.
        List<Vector2Int> tileOffsets = block.Tiles;
        foreach (Vector2Int offset in tileOffsets)
            Tiles![offset.x + baseTile.x, offset.y + baseTile.y].SetType(TileType.Occupied);

        if (!IsValidPort(block, baseTile, GameManager.Instance.Wire)) return false;

        // 점유된 그리드에 Port를 추가한다.
        List<Vector2Int> gridOffsets = block.Grids;
        for (int i = 0; i < gridOffsets.Count; i++)
        {
            Vector2Int offset = gridOffsets[i];
            WireExpr port = block.Ports[i];
            Grids![offset.x + baseTile.x, offset.y + baseTile.y].AddPort(port);
        }

        CheckInvalids();
        // 블록의 portIDs를 Evaluate 한다.
        GameManager.Instance.Wire.EvalAll();
        Debug.Log($"[PlaceBlock():{block.ID}] EvalAll() Done -- {block.Ports[0]}:{block.Ports[0].Cache}");

        return true;
    }

    public void RemoveBlock(BlockData block, Vector2Int baseTile, bool valid)
    {
        // 점유했던 타일을 Empty 상태로 변경한다.
        List<Vector2Int> tileOffsets = block.Tiles;
        foreach (Vector2Int offset in tileOffsets)
            Tiles![offset.x + baseTile.x, offset.y + baseTile.y].SetType(TileType.Empty);

        if (!valid) return;

        // 점유했던 그리드에서 Port를 제거한다.
        // Wire Manager의 WireDict, WireLogic을 수정한다.
        List<Vector2Int> gridOffsets = block.Grids;
        for (int i = 0; i < gridOffsets.Count; i++)
        {
            Vector2Int offset = gridOffsets[i];
            WireExpr port = block.Ports[i];
            Grids![offset.x + baseTile.x, offset.y + baseTile.y].RemovePort(port);
        }
        foreach (int id in block.PortIds) GameManager.Instance.Wire.RemoveWire(id);
        GameManager.Instance.Wire.RemoveSignature();

        CheckInvalids();
        GameManager.Instance.Wire.EvalAll();
    }

    public bool IsValidPos(BlockData block, Vector2Int baseTile)
    {
        List<Vector2Int> offsets = block.Tiles;
        foreach (Vector2Int offset in offsets)
        {
            if (!IsInTileBounds(baseTile.x + offset.x, baseTile.y + offset.y)) return false;
            if (!IsEmptyTile(baseTile.x + offset.x, baseTile.y + offset.y)) return false;
        }

        return true;
    }

    // 블록의 포트를 모두 Compatible -> AddToDict/AddToLogic 한다.
    // 모순 발생 시 Rollback하고 false를 반환한다.
    // 모순이 발생하지 않으면 포트를 모두 wire manager에 등록하고 true를 반환한다.
    private bool IsValidPort(BlockData block, Vector2Int baseGrid, WireManager wire)
    {
        Dictionary<int, Wire> backupWires = wire.Wires.ToDictionary(kvp => kvp.Key, kvp => new Wire(kvp.Value));
        Dictionary<int, HashSet<int>> backupDict = wire.WireDict.ToDictionary(kvp => kvp.Key, kvp => new HashSet<int>(kvp.Value));
        Dictionary<int, LogicExpr> backupLogic = new(wire.WireLogic);


        List<Vector2Int> offsets = block.Grids;
        for (int i = 0; i < offsets.Count; i++)
        {
            Grid grid = Grids![baseGrid.x + offsets[i].x, baseGrid.y + offsets[i].y];
            for (int j = 0; j < grid.Ports.Count; j++)
            {
                if (!wire.AddToDict(block.Ports[i], grid.Ports[j]))
                {
                    Debug.Log($"[IsValidPort:--invalid--] try: {block.Ports[i]} -> {grid.Ports[j]} | dict: {wire.StringOfWireDict()} | backup: {wire.StringOfWireDict(backupDict)}");

                    wire.RollBack(backupWires, backupDict, backupLogic);
                    return false;
                }
            }
            if (grid.Expr != null && grid.Type == GridType.Input)
            {
                if (!wire.AddToLogic(block.Ports[i], grid.Expr))
                {
                    Debug.Log($"[IsValidPort:--invalid--] try: {block.Ports[i]} -> {grid.Expr} | dict: {wire.StringOfWireLogic()} | backup: {wire.StringOfWireLogic(backupLogic)}");

                    wire.RollBack(backupWires, backupDict, backupLogic);
                    return false;
                }
            }
        }

        return true;
    }

    public Vector2Int? GetNearestTile(Vector3 worldPos)
    {
        if (Tiles == null) return null;

        float minDist = float.MaxValue;
        Vector2Int? nearest = null;

        int height = Tiles.GetLength(0), width = Tiles.GetLength(1);
        Vector3? topLeft;

        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                topLeft = TilePlacer.GetTileTopLeftWorld(x, y);
                if (topLeft == null) continue;
                float dist = Vector2.Distance((Vector2)worldPos, (Vector2)topLeft);

                if (dist < minDist) { minDist = dist; nearest = new Vector2Int(x, y); }
            }
        }

        Debug.Log($"nearest: {nearest}, minDict: {minDist}");

        return minDist <= Utils.THRESHOLD ? nearest : null;
    }

    public Vector3 GetBlockCenterOnTile(int x, int y, int height, int width) => TilePlacer.GetBlockCenterOnTile(x, y, height, width);
    public Vector3 GetTileSize() => TilePlacer.GetTileSize();
    public Vector3? GetTileTopLeftWorld(int x, int y) => TilePlacer.GetTileTopLeftWorld(x, y);
    #endregion
}