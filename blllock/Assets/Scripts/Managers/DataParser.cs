using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ModuleData
{
    public int ID { get; private set; }
    public void SetID(int id) => ID = id;
    public string Desc;
    public List<int> Stages { get; private set; } = new(); // Stage ID list

    // 클리어 하지 못한 최소 스테이지 인덱스
    // 모든 스테이지를 클리어 했을 경우 Stages.Count로 설정
    public int StageIndex { get; private set; } = 0;

    public void SetStages(List<int> stages) { Stages = stages; StageIndex = 0; }
    public void SetStageIndex(int index)
    {
        if (index < 0 || index > Stages.Count) StageIndex = 0;
        else StageIndex = index;
    }
    public void UpStageIndex()
    {
        if (StageIndex < Stages.Count) StageIndex++;
        else StageIndex = 0;
    }
}

public class StageData
{
    public int ID;
    public int BgWidth, BgHeight;
    public int TileWidth, TileHeight;
    public Vector2Int TilePosition;
    public List<(Vector2Int pos, LogicExpr expr)> Inputs = new(), Outputs = new();
    public string Desc;
    public bool IsCleared { get; private set; } = false;
    public void SetCleared(bool cleared) => IsCleared = cleared;

    #region Blocks
    public List<int> Blocks = new();
    public List<Vector2Int> BlockPositions = new();
    public List<int> RIndex = new(), FIndex = new(); // Rotate/Flip 가능한 Blocks 인덱스 리스트
    public List<int> SIndex = new(); // Spike Blocks 인덱스 리스트
    #endregion

    #region Barriers
    public List<Vector2Int> HBarriers = new(); // (Grid 좌표계, Tile 좌표계)를 사용한다.
    public List<Vector2Int> VBarriers = new(); // (Tile 좌표계, Grid 좌표계)를 사용한다.
    #endregion

    #region for JSON
    public static StageData FromRaw(Raw raw)
    {
        StageData stage = new()
        {
            ID = raw.ID,
            BgWidth = raw.BgWidth,
            BgHeight = raw.BgHeight,
            TileWidth = raw.TileWidth,
            TileHeight = raw.TileHeight,
            TilePosition = raw.TilePosition.ToVector2Int(),
            Desc = raw.Desc,
            Inputs = raw.Inputs.Select(
                io => (
                    io.pos.ToVector2Int(),
                    LogicExpr.Parse(io.expr)
                )
            ).ToList(),
            Outputs = raw.Outputs.Select(
                io => (
                    io.pos.ToVector2Int(),
                    LogicExpr.Parse(io.expr)
                )
            ).ToList(),
            Blocks = raw.Blocks,
            BlockPositions = raw.BlockPositions.Select(pos => pos.ToVector2Int()).ToList(),
            RIndex = raw.RIndex,
            FIndex = raw.FIndex,
            SIndex = raw.SIndex,
            HBarriers = raw.HBarriers.Select(pos => pos.ToVector2Int()).ToList(),
            VBarriers = raw.VBarriers.Select(pos => pos.ToVector2Int()).ToList()
        };
        return stage;
    }
    public static Raw ToRaw(StageData stage)
    {
        Raw raw = new()
        {
            ID = stage.ID,
            BgWidth = stage.BgWidth,
            BgHeight = stage.BgHeight,
            TileWidth = stage.TileWidth,
            TileHeight = stage.TileHeight,
            TilePosition = new RawPos { x = stage.TilePosition.x, y = stage.TilePosition.y },
            Inputs = stage.Inputs.Select(
                io => new RawIO
                {
                    pos = new RawPos { x = io.pos.x, y = io.pos.y },
                    expr = io.expr.ToDataString()
                }
            ).ToList(),
            Outputs = stage.Outputs.Select(
                io => new RawIO
                {
                    pos = new RawPos { x = io.pos.x, y = io.pos.y },
                    expr = io.expr.ToDataString()
                }
            ).ToList(),
            Desc = stage.Desc,
            Blocks = stage.Blocks,
            BlockPositions = stage.BlockPositions.Select(
                pos => new RawPos { x = pos.x, y = pos.y }
            ).ToList(),
            RIndex = stage.RIndex,
            FIndex = stage.FIndex,
            SIndex = stage.SIndex,
            HBarriers = stage.HBarriers.Select(
                pos => new RawPos { x = pos.x, y = pos.y }
            ).ToList(),
            VBarriers = stage.VBarriers.Select(
                pos => new RawPos { x = pos.x, y = pos.y }
            ).ToList()
        };
        return raw;
    }

    // JSON 파일로부터 1차 파싱을 위한 보조 클래스들
    [Serializable]
    public class RawStages
    {
        public List<Raw> Stages = new();
    }
    [Serializable]
    public class Raw
    {
        public int ID;
        public int BgWidth, BgHeight;
        public int TileWidth, TileHeight;
        public RawPos TilePosition;
        public List<RawIO> Inputs = new(), Outputs = new();
        public string Desc;
        public List<int> Blocks = new();
        public List<RawPos> BlockPositions = new();
        public List<int> RIndex = new(), FIndex = new();
        public List<int> SIndex = new();
        public List<RawPos> HBarriers = new(), VBarriers = new();
    }
    [Serializable]
    public class RawIO
    {
        public RawPos pos;
        public string expr;
    }
    [Serializable]
    public class RawPos
    {
        public int x;
        public int y;
        public Vector2Int ToVector2Int() => new(x, y);
    }
    #endregion
}


public class DataParser
{
    #region Constant Data
    private const string DataFolder = "Assets/Data";
    private const string ID = "ID", Width = "Width", Height = "Height", Desc = "Desc";
    private const string Tiles = "Tiles", Grids = "Grids", Ports = "Ports";
    private const string Inputs = "Inputs", Outputs = "Outputs";
    private const string Blocks = "Blocks", Rotate = "Rotate", Flip = "Flip";
    private const string Stages = "Stages";

    public Dictionary<int, BlockData> ParseBlockData(string filename)
    {
        Dictionary<int, BlockData> result = new();

        // Resources/Data 폴더 기준 경로, 확장자 제외
        TextAsset csvAsset = Resources.Load<TextAsset>($"Data/{filename}");
        if (csvAsset == null)
        {
            Utils.PrintError($"CSV 파일을 찾을 수 없음: Data/{filename}");
            return result;
        }

        // 기존 File.ReadAllLines 대신
        string[] lines = csvAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return result;

        string[] headers = lines[0].Split(',');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = line.Split(',');
            BlockData block = new();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                if (values[j].Length == 0) continue;
                string header = headers[j].ToLower();

                if (header == ID.ToLower()) block.SetID(int.Parse(values[j]));
                else if (header == Width.ToLower()) block.SetWidth(int.Parse(values[j]));
                else if (header == Height.ToLower()) block.SetHeight(int.Parse(values[j]));
                else if (header == Tiles.ToLower() || header == Grids.ToLower())
                {
                    List<string> items = values[j].Split(';').ToList<string>();
                    List<Vector2Int> vector = new();
                    for (int k = 0; k < items.Count; k++) vector.Add(ParsePos(items[k]));

                    if (header == Tiles.ToLower()) block.SetTiles(vector);
                    else block.SetGrids(vector);
                }
                else if (header == Ports.ToLower())
                {
                    List<string> items = values[j].Split(';').ToList<string>();
                    List<WireExpr> wires = new();
                    for (int k = 0; k < items.Count; k++) wires.Add(WireExpr.Parse(items[k]));
                    block.SetPorts(wires);
                }
            }

            result[block.ID] = block;
        }
        return result;
    }

    public Dictionary<int, ModuleData> ParseModuleData(string filename)
    {
        Dictionary<int, ModuleData> result = new();

        TextAsset csvAsset = Resources.Load<TextAsset>($"Data/{filename}");
        if (csvAsset == null)
        {
            Utils.PrintError($"CSV 파일을 찾을 수 없음: Data/{filename}");
            return result;
        }

        string[] lines = csvAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return result;

        string[] headers = lines[0].Split(',');
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = line.Split(',');
            ModuleData module = new();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                if (values[j].Length == 0) continue;
                string header = headers[j].ToLower();

                if (header == ID.ToLower()) module.SetID(int.Parse(values[j]));
                else if (header == Desc.ToLower()) module.Desc = values[j];
                else if (header == Stages.ToLower())
                {
                    List<string> items = values[j].Split(';').ToList<string>();
                    List<int> parsed = items.Select(int.Parse).ToList();
                    module.SetStages(parsed);
                }
            }

            result[module.ID] = module;
        }

        return result;
    }

    public Dictionary<int, StageData> LoadStageData(string filename)
    {
        int i;
        StageData.RawStages rawStages;
        StageData.Raw raw;
        StageData stage;
        TextAsset jsonAsset;
        Dictionary<int, StageData> result = new();

        // Resources/Data 폴더 기준 경로, 확장자 제외
        jsonAsset = Resources.Load<TextAsset>($"Data/{filename}");
        if (jsonAsset == null)
        {
            Utils.PrintError($"JSON 파일을 찾을 수 없음: Data/{filename}");
            return result;
        }

        rawStages = JsonUtility.FromJson<StageData.RawStages>(jsonAsset.text);
        for (i = 0; i < rawStages.Stages.Count; i++)
        {
            raw = rawStages.Stages[i];
            stage = StageData.FromRaw(raw);
            result[stage.ID] = stage;
        }
        
        return result;
    }

    public void SaveStageData(List<StageData> stages, string filename)
    {
#if UNITY_EDITOR
        int i;
        StageData.RawStages rawStages = new();
        List<StageData.Raw> rawList = new();
        StageData.Raw raw;
        StageData stage;
        string path, json;

        for (i = 0; i < stages.Count; i++)
        {
            stage = stages[i];
            raw = StageData.ToRaw(stage);
            rawList.Add(raw);
        }
        rawStages.Stages = rawList;

        json = JsonUtility.ToJson(rawStages, true);
        path = Path.Combine(Application.dataPath, $"Resources/Data/{filename}.json");
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
#endif
    }

    // CSV(,) 고려 (x.y)의 형태로 input 받는다.
    // 예시: posString == (1.2), posString == (4.4)
    private Vector2Int ParsePos(string posString)
    {
        string trimmed = posString[1..^1]; // 괄호 제거
        string[] splitted = trimmed.Split('.');
        if (splitted.Length > 2) return new(-1, -1);

        return new(int.Parse(splitted[0]), int.Parse(splitted[^1]));
    }
    #endregion

    #region Player Data

    // ModuleData의 StageIndex 불러오기
    // StageData의 IsCleared 불러오기
    public void LoadData(Dictionary<int, ModuleData> modules, Dictionary<int, StageData> stages)
    {
        for (int i = 0; i < modules.Count; i++)
        {
            modules[i].SetStageIndex(PlayerPrefs.GetInt(modules[i].ID.ToString(), 0));
            for (int j = 0; j < modules[i].StageIndex; j++)
            {
                if (j == modules[i].Stages.Count) break;
                int stageID = modules[i].Stages[j];
                if (stages.ContainsKey(stageID)) stages[stageID].SetCleared(true);
            }
        }
    }

    // ModuleData의 StageIndex 저장하기
    public void SaveData(Dictionary<int, ModuleData> modules)
    {
        for (int i = 0; i < modules.Count; i++) PlayerPrefs.SetInt(modules[i].ID.ToString(), modules[i].StageIndex);
        PlayerPrefs.Save();
    }

    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
    #endregion
}

