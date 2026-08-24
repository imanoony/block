#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;



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
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"Current Tile Occupancy: {string.Join(", ", Tiles.Cast<Tile>().Where(t => t.Type == TileType.Occupied).Select(t => t.Pos))}");
        }
    }

    public Grid[,]? Grids { get; private set; } = null;
    public Tile[,]? Tiles { get; private set; } = null;
    public HEdge[,]? HEdges { get; private set; } = null;
    public VEdge[,]? VEdges { get; private set; } = null;

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
    private CablePlacer? _cablePlacer;
    public CablePlacer CablePlacer
    {
        get
        {
            if (_cablePlacer == null)
            {
                _cablePlacer = gameObject.GetComponent<CablePlacer>();
                if (_cablePlacer == null)
                    _cablePlacer = gameObject.AddComponent<CablePlacer>();
            }
            return _cablePlacer;
        }
    }
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

        HEdges = new HEdge[cHeight + 1, cWidth];
        for (int x = 0; x < cHeight + 1; x++)
            for (int y = 0; y < cWidth; y++)
                HEdges[x, y] = new HEdge(new(x, y));

        foreach (Vector2Int hbarrier in stage.HBarriers)
            HEdges[hbarrier.x, hbarrier.y].SetType(EdgeType.Barrier);

        VEdges = new VEdge[cHeight, cWidth + 1];
        for (int x = 0; x < cHeight; x++)
            for (int y = 0; y < cWidth + 1; y++)
                VEdges[x, y] = new VEdge(new(x, y));

        foreach (Vector2Int vbarrier in stage.VBarriers)
            VEdges[vbarrier.x, vbarrier.y].SetType(EdgeType.Barrier);

        // TODO: VBarriers 관련 처리

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

        // 스테이지에 해당하는 타일 배치
        // 스테이지에 해당하는 블록 배치
        TilePlacer.PlaceBackground(bgWidth, bgHeight);
        TilePlacer.PlaceCircuit(cStartX, cStartY, cWidth, cHeight);
        BlockPlacer.PlaceBlocks(stage);

        // 스테이지에 해당하는 가로 배리어 배치
        // 스테이지에 해당하는 세로 배리어 배치
        TilePlacer.PlaceHBarriers(stage.HBarriers);
        TilePlacer.PlaceVBarriers(stage.VBarriers);
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

    #region Invalids
    private List<BlockInstance> invalidBlocks = new();
    private List<CableGroup> invalidCables = new();
    public void AddInvalid(BlockInstance instance)
    {
        if (instance.Valid) return;
        invalidBlocks.Add(instance);
    }
    public void AddInvalid(CableGroup group)
    {
        if (group.Valid) return;
        invalidCables.Add(group);
    }
    public void RemoveInvalid(BlockInstance instance)
    {
        if (!instance.Valid) return;
        if (invalidBlocks.Contains(instance)) invalidBlocks.Remove(instance);
    }
    public void RemoveInvalid(CableGroup group)
    {
        if (!group.Valid) return;
        if (invalidCables.Contains(group)) invalidCables.Remove(group);
    }
    private void CheckInvalids()
    {
        int i;

        List<BlockInstance> validBlocks = new();
        for (i = 0; i < invalidBlocks.Count; i++)
        {
            bool result = invalidBlocks[i].Check(this);
            if (result) validBlocks.Add(invalidBlocks[i]);
        }
        for (i = 0; i < validBlocks.Count; i++) invalidBlocks.Remove(validBlocks[i]);

        List<CableGroup> validCables = new();
        for (i = 0; i < invalidCables.Count; i++)
        {
            bool result = CablePlacer.Check(this, invalidCables[i]);
            if (result) validCables.Add(invalidCables[i]);
        }
        for (i = 0; i < validCables.Count; i++) invalidCables.Remove(validCables[i]);
    }
    #endregion

    #region Block Placement
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
            PortExpr port = block.Ports[i];
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
            PortExpr port = block.Ports[i];
            Grids![offset.x + circuitBase.x, offset.y + circuitBase.y].RemovePort(port);
        }
        foreach (int id in block.WireIds) GameManager.Instance.Wire.RemoveWire(id);

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
        Debug.Log("Checking IsValidPos for block with baseTile: " + baseTile + " and offsets: " + string.Join(", ", offsets));
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
                    IsHEdgeOccupied(
                        baseTile.y + offset.y, 
                        baseTile.x + offset.x, 
                        baseTile.x + offset.x - 1
                    )
                ) return false;
            if (offsets.Contains(new(offset.x, offset.y - 1)))
                if (
                    IsVEdgeOccupied(
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

    // is the horizontal edge occupied?
    // edge가 가로로 긴 상태, 즉 세로로 이어진 두 칸이 
    // 막혀있다면 false, 그렇지 않다면 true.
    private bool IsHEdgeOccupied(int y, int x1, int x2)
    {
        int x = x1 > x2 ? x1 : x2;
        if (stageCache == null) return true;
        if (HEdges == null) return true;

        int circuitX = x - stageCache.CircuitPosition.x;
        int circuitY = y - stageCache.CircuitPosition.y;

        if (circuitX < 0 || circuitX >= stageCache.CircuitHeight) return false;
        if (circuitY < 0 || circuitY > stageCache.CircuitWidth) return false;

        return HEdges[circuitX, circuitY].Type != EdgeType.Empty;
    }

    // is the vertical edge occupied?
    // edge가 세로로 긴 상태, 즉 가로로 이어진 두 칸이
    // 막혀있다면 false, 그렇지 않다면 true.
    private bool IsVEdgeOccupied(int x, int y1, int y2)
    {
        int y = y1 > y2 ? y1 : y2;
        if (stageCache == null) return true;
        if (VEdges == null) return true;

        int circuitX = x - stageCache.CircuitPosition.x;
        int circuitY = y - stageCache.CircuitPosition.y;

        if (circuitX < 0 || circuitX > stageCache.CircuitHeight) return false;
        if (circuitY < 0 || circuitY >= stageCache.CircuitWidth) return false;

        return VEdges[circuitX, circuitY].Type != EdgeType.Empty;
    }

    // 블록의 포트를 모두 Compatible -> AddToDict/AddToLogic 한다.
    // 모순 발생 시 Rollback하고 false를 반환한다.
    // 모순이 발생하지 않으면 포트를 모두 wire manager에 등록하고 true를 반환한다.
    private bool IsValidPort(BlockData block, Vector2Int baseGrid, WireManager wire)
    {
        Dictionary<int, Wire> backupWires = wire.Wires.ToDictionary(kvp => kvp.Key, kvp => new Wire(kvp.Value));
        Dictionary<int, HashSet<int>> backupDict = wire.WireDict.ToDictionary(kvp => kvp.Key, kvp => new HashSet<int>(kvp.Value));
        Dictionary<int, VarExpr> backupLogic = new(wire.WireLogic);


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
                    Debug.Log($"[IsValidPort:--invalid--] try: {block.Ports[i]} -> {grid.Expr} | dict: {wire.StringOfWireDict()} | backup dict: {wire.StringOfWireDict(backupDict)} | logic: {wire.StringOfWireLogic()} | backup logic: {wire.StringOfWireLogic(backupLogic)}");

                    wire.RollBack(backupWires, backupDict, backupLogic);
                    return false;
                }
            }
        }

        return true;
    }

    public List<Vector2Int?> GetNearestTiles(Vector3 worldPos, int count = 1)
    {
        List<(Vector2Int pos, float distSq)> nearest = new();

        if (Tiles == null || count <= 0) return new();
        float thresholdSq = Utils.THRESHOLD * Utils.THRESHOLD;

        int height = Tiles.GetLength(0);
        int width = Tiles.GetLength(1);

        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                Vector3? topLeft = TilePlacer.GetTileTopLeftWorld(x, y);
                if (topLeft == null) continue;

                float distSq = ((Vector2)worldPos - (Vector2)topLeft.Value).sqrMagnitude;
                if (distSq > thresholdSq) continue;

                (Vector2Int, float) candidate = (new Vector2Int(x, y), distSq);

                if (nearest.Count < count)
                {
                    nearest.Add(candidate);
                }
                else
                {
                    int worstIdx = 0;

                    for (int i = 1; i < nearest.Count; i++)
                    {
                        if (nearest[i].distSq > nearest[worstIdx].distSq)
                            worstIdx = i;
                    }

                    if (distSq < nearest[worstIdx].distSq)
                    {
                        nearest[worstIdx] = candidate;
                    }
                }
            }
        }

        nearest.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        List<Vector2Int?> result = new();
        foreach ((Vector2Int pos, float) tile in nearest) result.Add(tile.pos);
        while (result.Count < count) result.Add(null);

        return result;
    }

    public List<Vector2Int?> GetNearestGrids(Vector3 worldPos, int count = 1, bool useThreshold = true)
    {
        List<(Vector2Int pos, float distSq)> nearest = new();
        
        if (Grids == null || count <= 0) return new();
        float thresholdSq = (Utils.THRESHOLD-2.6f) * (Utils.THRESHOLD-2.6f);

        int height = Grids.GetLength(0);
        int width = Grids.GetLength(1);

        for (int x = 0; x < height; x++)
        {
            for (int y = 0; y < width; y++)
            {
                int tileX = x + GetCircuitStart().x;
                int tileY = y + GetCircuitStart().y;

                Vector3? topLeft = TilePlacer.GetTileTopLeftWorld(tileX, tileY);
                if (topLeft == null) continue;

                float distSq = ((Vector2)worldPos - (Vector2)topLeft.Value).sqrMagnitude;
                if (useThreshold && distSq > thresholdSq) continue;

                (Vector2Int, float) candidate = (new Vector2Int(x, y), distSq);

                if (nearest.Count < count)
                {
                    nearest.Add(candidate);
                }
                else
                {
                    int worstIdx = 0;

                    for (int i = 1; i < nearest.Count; i++)
                    {
                        if (nearest[i].distSq > nearest[worstIdx].distSq)
                            worstIdx = i;
                    }

                    if (distSq < nearest[worstIdx].distSq)
                    {
                        nearest[worstIdx] = candidate;
                    }
                }
            }
        }

        nearest.Sort((a, b) => a.distSq.CompareTo(b.distSq));

        List<Vector2Int?> result = new();
        foreach ((Vector2Int pos, float) tile in nearest) result.Add(tile.pos);
        while (result.Count < count) result.Add(null);

        return result;
    }

    public Vector3? GetTileTopLeftWorld(int x, int y) => TilePlacer.GetTileTopLeftWorld(x, y);
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

    #region Cable Placement

    public void PlaceCable(Cable cable)
    {
        Edge edge = cable.ToEdge();
        switch (edge)
        {
            case HEdge he:
                if (HEdges == null) return;
                HEdges[he.Pos.x, he.Pos.y].SetType(EdgeType.Cable);
                break;
            case VEdge ve:
                if (VEdges == null) return;
                VEdges[ve.Pos.x, ve.Pos.y].SetType(EdgeType.Cable);
                break;
            default:
                break;
        }
    }

    public bool PlaceCableGroup(CableGroup group)
    {
        if (!IsValidPort(group, GameManager.Instance.Wire)) return false;

        // 점유된 그리드에 Port를 추가한다.
        PortVar port = group.Port;
        foreach (Vector2Int end in group.Ends)
        {
            Grids![end.x, end.y].AddPort(port);
        }

        // 케이블 그룹의 portIDs를 Evaluate 한다.
        GameManager.Instance.Wire.EvalAll();

        return true;
    }

    public void RemoveCable(Cable cable)
    {
        Edge edge = cable.ToEdge();
        switch (edge)
        {
            case HEdge he:
                if (HEdges == null) return;
                HEdges[he.Pos.x, he.Pos.y].SetType(EdgeType.Empty);
                break;
            case VEdge ve:
                if (VEdges == null) return;
                VEdges[ve.Pos.x, ve.Pos.y].SetType(EdgeType.Empty);
                break;
            default:
                break;
        }
    }

    public void RemoveCableGroup(CableGroup group, bool valid)
    {
        Debug.Log($"Remove Cable Group, valid? {valid}");
        if (!valid)
        {
            group.SetValid(true);
            return;
        }

        // 점유했던 그리드에서 Port를 제거한다
        PortVar port = group.Port;
        foreach (Vector2Int end in group.Ends)
        {
            Grids![end.x, end.y].RemovePort(port);
        }

        // Wire Manager의 WireDict, WireLogic을 수정한다.
        foreach (int id in group.WireIds) GameManager.Instance.Wire.RemoveWire(id);

        CheckInvalids();
        GameManager.Instance.Wire.EvalAll();
    }

    public bool IsValidPos(Cable cable)
    {
        Edge edge = cable.ToEdge();
        switch (edge)
        {
            case HEdge he:
                if (HEdges == null) return false;
                return HEdges[he.Pos.x, he.Pos.y].Type == EdgeType.Empty;
            case VEdge ve:
                if (VEdges == null) return false;
                return VEdges[ve.Pos.x, ve.Pos.y].Type == EdgeType.Empty;
            default:
                break;
        }
        return false;
    }

    // 케이블 그룹의 포트를 모두 Compatible -> AddToDict/AddToLogic 한다.
    // 모순 발생 시 Rollback하고 false를 반환한다.
    // 모순이 발생하지 않으면 포트를 모두 wire manager에 등록하고 true를 반환한다.
    private bool IsValidPort(CableGroup group, WireManager wire)
    {
        Dictionary<int, Wire> backupWires = wire.Wires.ToDictionary(kvp => kvp.Key, kvp => new Wire(kvp.Value));
        Dictionary<int, HashSet<int>> backupDict = wire.WireDict.ToDictionary(kvp => kvp.Key, kvp => new HashSet<int>(kvp.Value));
        Dictionary<int, VarExpr> backupLogic = new(wire.WireLogic);
        
        PortVar port = group.Port;

        foreach (Vector2Int end in group.Ends)
        {
            Grid grid = Grids![end.x, end.y];
            for (int i = 0; i < grid.Ports.Count; i++)
            {
                if (!wire.AddToDict(port, grid.Ports[i]))
                {
                    wire.RollBack(backupWires, backupDict, backupLogic);
                    return false;
                }
            }
            if (grid.Expr != null && grid.Type == GridType.Input)
            {
                if (!wire.AddToLogic(port, grid.Expr))
                {
                    wire.RollBack(backupWires, backupDict, backupLogic);
                    return false;
                }
            }
        }

        return true;
    }
    #endregion
}