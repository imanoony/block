using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DataParser
{
    public class RowData
    {
        private Dictionary<string, string> fields = new Dictionary<string, string>();

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
        }

        public override string ToString()
        {
            return string.Join(", ", fields);
        }
    }

    public static List<RowData> ParseCSV(string filePath)
    {
        var result = new List<RowData>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {filePath}");
            return result;
        }

        var lines = File.ReadAllLines(filePath);
        if (lines.Length < 2) return result;

        var headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var parts = line.Split(',');

            var row = new RowData();
            for (int j = 0; j < headers.Length && j < parts.Length; j++)
            {
                row.AddField(headers[j].Trim(), parts[j].Trim());
            }

            result.Add(row);
        }

        return result;
    }
}
