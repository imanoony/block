using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DataParser
{
    public class RowData
    {
        private Dictionary<string, string> fields = new Dictionary<string, string>();

        public List<(Vector2Int pos, LogicExpr expr)> Inputs { get; private set; } = new();
        public List<(Vector2Int pos, LogicExpr expr)> Outputs { get; private set; } = new();

        public string this[string columnName]
        {
            get => fields.ContainsKey(columnName) ? fields[columnName] : null;
        }

        public bool TryGetInt(string columnName, out int value)
        {
            value = 0;
            if (!fields.ContainsKey(columnName)) return false;
            return int.TryParse(fields[columnName], out value);
        }

        public bool TryGetFloat(string columnName, out float value)
        {
            value = 0;
            if (!fields.ContainsKey(columnName)) return false;
            return float.TryParse(fields[columnName], out value);
        }

        internal void AddField(string key, string value)
        {
            fields[key] = value;

            if (key == "Inputs")
                Inputs = ParseIOField(value);
            else if (key == "Outputs")
                Outputs = ParseIOField(value);
        }

        private List<(Vector2Int pos, LogicExpr expr)> ParseIOField(string field)
        {
            var result = new List<(Vector2Int, LogicExpr)>();
            if (string.IsNullOrWhiteSpace(field)) return result;

            var parts = field.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var split = trimmed.Split(':');
                if (split.Length != 2) continue;

                var posStr = split[0].Trim().Trim('(', ')');
                var exprStr = split[1].Trim();

                var posParts = posStr.Split('.');
                if (posParts.Length != 2) continue;

                if (int.TryParse(posParts[0], out int x) && int.TryParse(posParts[1], out int y))
                {
                    var expr = LogicExprParser.Parse(exprStr);
                    result.Add((new Vector2Int(x, y), expr));
                }
            }

            return result;
        }

        public override string ToString()
        {
            return string.Join(", ", fields);
        }
    }

    private static class LogicExprParser
    {
        private static int pos;
        private static string input;

        public static LogicExpr Parse(string expr)
        {
            input = expr.Replace(" ", "");
            pos = 0;
            return ParseOr();
        }

        private static LogicExpr ParseOr()
        {
            var operands = new List<LogicExpr> { ParseAnd() };
            while (Match('+'))
            {
                operands.Add(ParseAnd());
            }

            return operands.Count == 1 ? operands[0] : new OrExpr(operands);
        }

        private static LogicExpr ParseAnd()
        {
            var operands = new List<LogicExpr> { ParsePrimary() };
            while (Match('*'))
            {
                operands.Add(ParsePrimary());
            }

            return operands.Count == 1 ? operands[0] : new AndExpr(operands);
        }

        private static LogicExpr ParsePrimary()
        {
            if (Match('~'))
            {
                return new NotExpr(ParsePrimary());
            }
            if (Match('('))
            {
                var inner = ParseOr();
                Expect(')');
                return inner;
            }

            var token = ParseToken();
            if (token == "1") return new ConstantExpr(true);
            if (token == "0") return new ConstantExpr(false);
            return new VarExpr(token);
        }

        private static string ParseToken()
        {
            int start = pos;
            while (pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_'))
            {
                pos++;
            }
            return input.Substring(start, pos - start);
        }

        private static bool Match(char expected)
        {
            if (pos < input.Length && input[pos] == expected)
            {
                pos++;
                return true;
            }
            return false;
        }

        private static void Expect(char expected)
        {
            if (!Match(expected))
            {
                throw new System.Exception($"Expected '{expected}' at position {pos} in '{input}'");
            }
        }
    }

    public static List<RowData> ParseCSV(string filePath)
    {

        var rows = new List<RowData>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {filePath}");
            return rows;
        }

        var lines = File.ReadAllLines(filePath);
        if (lines.Length < 2) return rows;

        var headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split(',');
            var row = new RowData();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                var header = headers[j].Trim();
                var value = values[j].Trim();
                row.AddField(header, value);
            }

            rows.Add(row);
        }

        return rows;
    }
}

