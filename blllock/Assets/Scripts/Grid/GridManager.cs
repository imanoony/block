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
    public bool IsCircuit { get; private set; } = false;
    public Tile(Vector2Int pos, TileType type = TileType.Empty) { Pos = pos; Type = type; }
    public void SetType(TileType type) => Type = type;
    public void SetIsCircuit(bool isCircuit) => IsCircuit = isCircuit;
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

    public HashSet<Vector2Int> HBarriers { get; private set; } = new();
    public HashSet<Vector2Int> VBarriers { get; private set; } = new();
    private StageData? stageCache = null;

    public void InitStage(int id) => InitStage(GameManager.Instance.StageLibrary[id]);

    public void InitStage(StageData stage)
    {
        stageCache = stage;

        int bgWidth = stage.BgWidth;
        int bgHeight = stage.BgHeight;
        int cWidth = stage.CircuitWidth;
        int cHeight = stage.CircuitHeight;
        int cStartX = stage.CircuitPosition.x;
        int cStartY = stage.CircuitPosition.y;

        Grids = new Grid[cHeight + 1, cWidth + 1];
        for (int x = 0; x < cHeight + 1; x++)
            for (int y = 0; y < cWidth + 1; y++)
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

        Tiles = new Tile[bgHeight, bgWidth];
        for (int x = 0; x < bgHeight; x++)
        {
            for (int y = 0; y < bgWidth; y++)
            {
                bool isHorizontalBorder =
                    (x == cStartX - 1 || x == cStartX + cHeight) &&
                    y >= cStartY - 1 && y <= cStartY + cWidth;

                bool isVerticalBorder =
                    (y == cStartY - 1 || y == cStartY + cWidth) &&
                    x >= cStartX - 1 && x <= cStartX + cHeight;

                bool isCircuit =
                    x >= cStartX &&
                    x < cStartX + cHeight &&
                    y >= cStartY &&
                    y < cStartY + cWidth;

                Tile tile;

                if (isHorizontalBorder || isVerticalBorder)
                {
                    tile = new Tile(new(x, y), TileType.Occupied);
                }
                else
                {
                    tile = new Tile(new(x, y));

                    if (isCircuit)
                    {
                        tile.SetIsCircuit(true);
                    }
                }

                Tiles[x, y] = tile;
            }
        }
        // DEBUG
        /*for (int x = 0; x < bgHeight; x++)
        {
            for (int y = 0; y < bgWidth; y++)
            {
                Tile tile = Tiles[x, y];

                Debug.Log(
                    $"Tile ({x}, {y}) | " +
                    $"Occupied: {tile.Type == TileType.Occupied} | " +
                    $"IsCircuit: {tile.IsCircuit}"
                );
            }
        }*/

        HBarriers = stage.HBarriers.ToHashSet();
        VBarriers = stage.VBarriers.ToHashSet();

        // 스테이지에 해당하는 타일 배치
        // 스테이지에 해당하는 블록 배치
        TilePlacer.PlaceBackground(bgWidth, bgHeight);
        TilePlacer.PlaceCircuit(cStartX, cStartY, cWidth, cHeight);
        BlockPlacer.PlaceBlocks(stage);

        // 스테이지에 해당하는 가로 배리어 배치
        // 스테이지에 해당하는 세로 배리어 배치
        TilePlacer.PlaceHBarriers(HBarriers);
        TilePlacer.PlaceVBarriers(VBarriers);
    }

    public void RemoveCurrentStage()
    {
        if (Tiles != null) Tiles = null;
        if (Grids != null) Grids = null;
        
        placeCount = 0;
        GameManager.Instance.Audio.ResetBGM();

        TilePlacer.RemoveCircuit();
        BlockPlacer.RemoveBlocks();
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
        List<BlockInstance> valids = new();
        for (int i = 0; i < invalids.Count; i++)
        {
            bool result = invalids[i].Check(this);
            if (result) valids.Add(invalids[i]);
        }
        for (int i = 0; i < valids.Count; i++) invalids.Remove(valids[i]);
    }

    private int placeCount = 0;
    public bool PlaceBlock(BlockData block, Vector2Int baseTile)
    {
        // 점유된 타일을 Occupied 상태로 변경한다.
        List<Vector2Int> tileOffsets = block.Tiles;
        foreach (Vector2Int offset in tileOffsets)
            Tiles![offset.x + baseTile.x, offset.y + baseTile.y].SetType(TileType.Occupied);
        List<Vector2Int> spikeOffsets = block.HasSpike ? block.SpikeTiles : new();
        foreach (Vector2Int spike in spikeOffsets)
        {
            if (!IsInTileBounds(baseTile.x + spike.x, baseTile.y + spike.y)) continue;
            Tiles![spike.x + baseTile.x, spike.y + baseTile.y].SetType(TileType.Occupied);
        }

        if (!IsInCircuit(baseTile.x, baseTile.y)) return true;
        
        Vector2Int circuitBase = GetCircuitBase(baseTile.x, baseTile.y);
        if (!IsValidPort(block, circuitBase, GameManager.Instance.Wire)) return false;

        // 점유된 그리드에 Port를 추가한다.
        List<Vector2Int> gridOffsets = block.Grids;
        for (int i = 0; i < gridOffsets.Count; i++)
        {
            Vector2Int offset = gridOffsets[i];
            WireExpr port = block.Ports[i];
            Grids![offset.x + circuitBase.x, offset.y + circuitBase.y].AddPort(port);
        }

        // 블록의 portIDs를 Evaluate 한다.
        GameManager.Instance.Wire.EvalAll();

        placeCount++;
        if (placeCount == Utils.AUDIO_THRESHOLD0) GameManager.Instance.Audio.PlayBGM(1);
        if (placeCount == Utils.AUDIO_THRESHOLD1) GameManager.Instance.Audio.PlayBGM(2);
        if (placeCount == Utils.AUDIO_THRESHOLD2) GameManager.Instance.Audio.PlayBGM(3);
        return true;
    }

    public void RemoveBlock(BlockData block, Vector2Int baseTile, bool valid)
    {
        // 점유했던 타일을 Empty 상태로 변경한다.
        List<Vector2Int> tileOffsets = block.Tiles;
        foreach (Vector2Int offset in tileOffsets)
            Tiles![offset.x + baseTile.x, offset.y + baseTile.y].SetType(TileType.Empty);
        List<Vector2Int> spikeOffsets = block.HasSpike ? block.SpikeTiles : new();
        foreach (Vector2Int spike in spikeOffsets)
        {
            if (!IsInTileBounds(baseTile.x + spike.x, baseTile.y + spike.y)) continue;
            Tiles![spike.x + baseTile.x, spike.y + baseTile.y].SetType(TileType.Empty);
        }

        if (!IsInCircuit(baseTile.x, baseTile.y)) return;

        if (!valid) return;

        Vector2Int circuitBase = GetCircuitBase(baseTile.x, baseTile.y);

        // 점유했던 그리드에서 Port를 제거한다.
        // Wire Manager의 WireDict, WireLogic을 수정한다.
        List<Vector2Int> gridOffsets = block.Grids;
        for (int i = 0; i < gridOffsets.Count; i++)
        {
            Vector2Int offset = gridOffsets[i];
            WireExpr port = block.Ports[i];
            Grids![offset.x + circuitBase.x, offset.y + circuitBase.y].RemovePort(port);
        }
        foreach (int id in block.PortIds) GameManager.Instance.Wire.RemoveWire(id);
        GameManager.Instance.Wire.RemoveSignature();

        CheckInvalids();
        GameManager.Instance.Wire.EvalAll();

        placeCount--;
        if (placeCount == Utils.AUDIO_THRESHOLD0 - 1) GameManager.Instance.Audio.StopBGM(1);
        if (placeCount == Utils.AUDIO_THRESHOLD1 - 1) GameManager.Instance.Audio.StopBGM(2);
        if (placeCount == Utils.AUDIO_THRESHOLD2 - 1) GameManager.Instance.Audio.StopBGM(3);
    }

    public bool IsValidPos(BlockData block, Vector2Int baseTile)
    {
        HashSet<Vector2Int> offsets = block.Tiles.ToHashSet();
        foreach (Vector2Int offset in offsets)
        {
            if (!IsInTileBounds(baseTile.x + offset.x, baseTile.y + offset.y)) return false;
            if (!IsEmptyTile(baseTile.x + offset.x, baseTile.y + offset.y)) return false;
        }

        HashSet<Vector2Int> spikes = block.HasSpike ? block.SpikeTiles.ToHashSet() : new();
        foreach (Vector2Int spike in spikes)
        {
            if (!IsInTileBounds(baseTile.x + spike.x, baseTile.y + spike.y)) continue;
            if (!IsEmptyTile(baseTile.x + spike.x, baseTile.y + spike.y)) return false;
        }

        offsets.UnionWith(spikes);
        foreach (Vector2Int offset in offsets)
        {
            // barrier checking
            if (offsets.Contains(new(offset.x - 1, offset.y)))
                if (
                    IsHBarriered(
                        baseTile.y + offset.y, 
                        baseTile.x + offset.x, 
                        baseTile.x + offset.x - 1
                    )
                ) return false;
            if (offsets.Contains(new(offset.x, offset.y - 1)))
                if (
                    IsVBarriered(
                        baseTile.x + offset.x,
                        baseTile.y + offset.y,
                        baseTile.y + offset.y - 1
                    )
                ) return false;
        }

        return true;
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
    public bool IsInCircuit(int x, int y)
    {
        if (Tiles == null) return false;
        if (!IsInTileBounds(x, y)) return false;
        return Tiles[x, y].IsCircuit;
    }

    // is horizontally barriered?
    // barrier가 가로로 긴 상태, 즉 세로로 이어진 두 칸이 
    // 막혀있다면 false, 그렇지 않다면 true.
    private bool IsHBarriered(int y, int x1, int x2)
    {
        int x = x1 > x2 ? x1 : x2;
        if (stageCache == null) return false;
        int circuitStartX = stageCache.CircuitPosition.x;
        int circuitStartY = stageCache.CircuitPosition.y;
        return HBarriers.Contains(new(x - circuitStartX, y - circuitStartY));
    }

    // is vertically barriered?
    // barrier가 세로로 긴 상태, 즉 가로로 이어진 두 칸이
    // 막혀있다면 false, 그렇지 않다면 true.
    private bool IsVBarriered(int x, int y1, int y2)
    {
        int y = y1 > y2 ? y1 : y2;
        if (stageCache == null) return false;
        int circuitStartX = stageCache.CircuitPosition.x;
        int circuitStartY = stageCache.CircuitPosition.y;
        return VBarriers.Contains(new(x - circuitStartX, y - circuitStartY));
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
    public Vector3 GetTileTopLeftForChat(int x, int y)
    {
        Debug.Log($"[GetTileTopLeftForChat] (x:{x}, y:{y})");
        Vector3 pos = (Vector3)TilePlacer.GetTileTopLeftWorld(x, y)!;
        pos = new(pos.x, pos.y + GetTileSize().y / 16f, pos.z);
        return pos;
    }
    private Vector2Int GetCircuitBase(int x, int y)
    {
        if (stageCache == null) return Vector2Int.zero;
        int cStartX = stageCache.CircuitPosition.x;
        int cStartY = stageCache.CircuitPosition.y;

        return new Vector2Int(x - cStartX, y - cStartY);
    }
    public Vector2Int GetCircuitStart() => stageCache == null ? Vector2Int.zero : stageCache.CircuitPosition;
    #endregion
}