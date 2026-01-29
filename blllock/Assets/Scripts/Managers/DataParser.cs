using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public int ID, Width, Height;
    public List<(Vector2Int pos, LogicExpr expr)> Inputs = new(), Outputs = new();
    public string Desc;
    public bool IsCleared { get; private set; } = false;
    public void SetCleared(bool cleared) => IsCleared = cleared;

    #region Blocks
    public List<int> Blocks = new();
    public List<int> RIndex = new(), FIndex = new(); // Rotate/Flip 가능한 Blocks 인덱스 리스트
    #endregion

    #region Barriers
    public List<Vector2Int> HBarriers = new(); // (Grid 좌표계, Tile 좌표계)를 사용한다.
    public List<Vector2Int> VBarriers = new(); // (Tile 좌표계, Grid 좌표계)를 사용한다.
    #endregion

    #region for JSON

    // JSON 파일로부터 1차 파싱을 위한 보조 클래스들
    [Serializable]
    public class Raw
    {
        public int ID, Width, Height;
        public List<RawIO> Inputs, Outputs;
        public List<int> Blocks;
        public List<int> RIndex, FIndex;
        public List<RawPos> HBarriers, VBarriers;
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
                    for (int k = 0; k < items.Count; k++) wires.Add(ParseExpr<WireExpr>(items[k]));
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

    public Dictionary<int, StageData> ParseStageData(string filename)
    {
        Dictionary<int, StageData> result = new();

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
            StageData stage = new();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                if (values[j].Length == 0) continue;
                string header = headers[j].ToLower();

                if (header == ID.ToLower()) stage.ID = int.Parse(values[j]);
                else if (header == Width.ToLower()) stage.Width = int.Parse(values[j]);
                else if (header == Height.ToLower()) stage.Height = int.Parse(values[j]);
                else if (header == Desc.ToLower()) stage.Desc = values[j];
                else if (header == Inputs.ToLower() || header == Outputs.ToLower())
                {
                    List<string> items = values[j].Split(';').ToList<string>();
                    List<(Vector2Int, LogicExpr)> sources = new();

                    foreach (string item in items)
                    {
                        string[] splitted = item.Split(':');
                        Vector2Int pos = ParsePos(splitted[0]);
                        LogicExpr expr = ParseExpr<LogicExpr>(splitted[^1]);

                        sources.Add((pos, expr));
                    }

                    if (header == Inputs.ToLower()) stage.Inputs = sources;
                    else stage.Outputs = sources;
                }
                else if (header == Blocks.ToLower() || header == Rotate.ToLower() || header == Flip.ToLower())
                {
                    List<string> items = values[j].Split(';').ToList<string>();
                    List<int> parsed = items.Select(int.Parse).ToList();

                    if (header == Blocks.ToLower()) stage.Blocks = parsed;
                    else if (header == Rotate.ToLower()) stage.RIndex = parsed;
                    else stage.FIndex = parsed;
                }
            }

            result[stage.ID] = stage;
        }
        return result;
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
    private const char not = '~', and = '*', or = '+';
    private const string parens = "()";

    private T ParseExpr<T>(string exprString)
    {
        if (string.IsNullOrEmpty(exprString)) return default;

        // 바깥 레벨 괄호인지 확인
        bool IsWrappedByParentheses(string s)
        {
            if (s.Length < 2 || s[0] != '(' || s[^1] != ')') return false;

            int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '(') depth++;
                else if (s[i] == ')') depth--;

                // 마지막 문자 제외하고 depth가 0이면 바깥 괄호 아님
                if (i < s.Length - 1 && depth == 0) return false;
            }
            return depth == 0;
        }

        // WireExpr 처리
        if (typeof(T) == typeof(WireExpr))
        {
            // 단항 NOT
            if (exprString[0] == not)
            {
                string inner = exprString[1..];
                if (IsWrappedByParentheses(inner)) inner = inner[1..^1];
                return (T)(object)new WireNot(ParseExpr<WireExpr>(inner)).Clean();
            }

            // 바깥 괄호 제거
            if (IsWrappedByParentheses(exprString))
                return (T)(object)ParseExpr<WireExpr>(exprString[1..^1]);

            // 이항 연산자 처리 (*, +)
            int depth = 0;
            for (int i = 0; i < exprString.Length; i++)
            {
                char c = exprString[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (depth == 0 && (c == and || c == or))
                {
                    WireExpr left = ParseExpr<WireExpr>(exprString[0..i]);
                    WireExpr right = ParseExpr<WireExpr>(exprString[(i + 1)..]);
                    return (T)(object)(c == and ? new WireAnd(left, right) : new WireOr(left, right));
                }
            }

            // 단일 문자
            if (exprString.Length == 1)
            {
                int reservedID = GameManager.Instance.Wire.NameToReservedID(exprString[0]);
                return (T)(object)GameManager.Instance.Wire.GetReservedWire(reservedID);
            }

            return default;
        }
        // LogicExpr 처리
        else if (typeof(T) == typeof(LogicExpr))
        {
            // 단항 NOT
            if (exprString[0] == not)
            {
                string inner = exprString[1..];
                if (IsWrappedByParentheses(inner)) inner = inner[1..^1];
                return (T)(object)new NotExpr(ParseExpr<LogicExpr>(inner)).Clean();
            }

            // 바깥 괄호 제거
            if (IsWrappedByParentheses(exprString))
                return (T)(object)ParseExpr<LogicExpr>(exprString[1..^1]);

            // 이항 연산자 처리 (*, +)
            int depth = 0;
            for (int i = 0; i < exprString.Length; i++)
            {
                char c = exprString[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (depth == 0 && (c == and || c == or))
                {
                    LogicExpr left = ParseExpr<LogicExpr>(exprString[0..i]);
                    LogicExpr right = ParseExpr<LogicExpr>(exprString[(i + 1)..]);
                    return (T)(object)(c == and ? new AndExpr(left, right) : new OrExpr(left, right));
                }
            }

            // 단일 문자/숫자
            if (exprString.Length == 1)
            {
                if (char.IsDigit(exprString[0])) return (T)(object)new ConstantExpr(int.Parse(exprString));
                else return (T)(object)new VarExpr(exprString);
            }

            return default;
        }

        else throw new InvalidDataException("[ParseExpr()] 잘못된 타입");
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

