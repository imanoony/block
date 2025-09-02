using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class StageData
{
    public int ID, Width, Height;
    public List<(Vector2Int pos, LogicExpr expr)> Inputs, Outputs;
}

public static class DataParser
{
    private const string ID = "ID", Width = "Width", Height = "Height";
    private const string Sprite = "Sprite", Tiles = "Tiles", Grids = "Grids", Ports = "Ports";
    private const string Inputs = "Inputs", Outputs = "Outputs";
    public static Dictionary<int, BlockData> ParseBlockData(string filepath)
    {
        Dictionary<int, BlockData> result = new();

        if (!File.Exists(filepath))
        {
            Utils.PrintError($"CSV 파일을 찾을 수 없음: {filepath}");
            return result;
        }

        string[] lines = File.ReadAllLines(filepath);
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
                string header = headers[j].ToLower();

                if (header == ID.ToLower()) block.SetID(int.Parse(values[j]));
                else if (header == Width.ToLower()) block.SetWidth(int.Parse(values[j]));
                else if (header == Height.ToLower()) block.SetHeight(int.Parse(values[j]));
                else if (header == Sprite.ToLower()) { } // TODO
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
    public static Dictionary<int, StageData> ParseStageData(string filepath)
    {
        Dictionary<int, StageData> result = new();

        if (!File.Exists(filepath))
        {
            Utils.PrintError($"CSV 파일을 찾을 수 없음: {filepath}");
            return result;
        }

        string[] lines = File.ReadAllLines(filepath);
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
                string header = headers[j].ToLower();

                if (header == ID.ToLower()) stage.ID = int.Parse(values[j]);
                else if (header == Width.ToLower()) stage.Width = int.Parse(values[j]);
                else if (header == Height.ToLower()) stage.Height = int.Parse(values[j]);
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
            }

            result[stage.ID] = stage;
        }
        return result;
    }

    // CSV(,) 고려 (x.y)의 형태로 input 받는다.
    // 예시: posString == (1.2), posString == (4.4)
    private static Vector2Int ParsePos(string posString)
    {
        string trimmed = posString[1..^1]; // 괄호 제거
        string[] splitted = trimmed.Split('.');
        if (splitted.Length > 2) return new(-1, -1);

        return new(int.Parse(splitted[0]), int.Parse(splitted[^1]));
    }
    private const char not = '~', and = '*', or = '+';
    private const string parens = "()";
    private static T ParseExpr<T>(string exprString)
    {
        if (exprString == "") return default;
        int pos = -1;

        if (typeof(T) == typeof(WireExpr))
        {
            if (exprString[0] == not) return (T)(object)new WireNot(ParseExpr<WireExpr>(exprString[1..])).Clean();
            if (exprString[0] == parens[0] && exprString[^1] == parens[^1]) return (T)(object)ParseExpr<WireExpr>(exprString[1..^1]);

            WireExpr left, right;
            int depth = 0;
            for (int i = 0; i < exprString.Length; i++)
            {
                char c = exprString[i];

                if (c == parens[0]) depth++;
                else if (c == parens[^1]) depth--;
                else if (depth == 0 && (c == and || c == or))
                {
                    pos = i; // 바깥 레벨에서 연산자 발견
                    break;
                }
            }
            if (pos != -1)
            {
                left = ParseExpr<WireExpr>(exprString[0..pos]);
                right = ParseExpr<WireExpr>(exprString[(pos + 1)..]);

                if (exprString[pos] == and) return (T)(object)new WireAnd(left, right);
                else return (T)(object)new WireOr(left, right);
            }

            if (exprString.Length == 1)
            {
                int reservedID = GameManager.Instance.Wire.NameToReservedID(exprString[0]);
                return (T)(object)GameManager.Instance.Wire.GetReservedWire(reservedID);
            }

            return default;
        }
        else if (typeof(T) == typeof(LogicExpr))
        {
            if (exprString[0] == not) return (T)(object)new NotExpr(ParseExpr<LogicExpr>(exprString[1..])).Clean();
            if (exprString[0] == parens[0] && exprString[^1] == parens[^1]) return (T)(object)ParseExpr<LogicExpr>(exprString[1..^1]);

            LogicExpr left, right;
            int depth = 0;
            for (int i = 0; i < exprString.Length; i++)
            {
                char c = exprString[i];

                if (c == parens[0]) depth++;
                else if (c == parens[^1]) depth--;
                else if (depth == 0 && (c == and || c == or))
                {
                    pos = i; // 바깥 레벨에서 연산자 발견
                    break;
                }
            }
            if (pos != -1)
            {
                left = ParseExpr<LogicExpr>(exprString[0..pos]);
                right = ParseExpr<LogicExpr>(exprString[(pos + 1)..]);

                if (exprString[pos] == and) return (T)(object)new AndExpr(left, right);
                else return (T)(object)new OrExpr(left, right);
            }

            if (exprString.Length == 1)
            {
                if (char.IsDigit(exprString[0])) return (T)(object)new ConstantExpr(int.Parse(exprString));
                else return (T)(object)new VarExpr(exprString);
            }

            return default;
        }

        else throw new InvalidDataException("[ParseExpr()] 잘못된 타입");
    }
}

