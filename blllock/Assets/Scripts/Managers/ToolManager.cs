using System;
using System.Collections.Generic;

public enum ToolType
{
    None = -1,
    Cable,
    Resistor,
}

public class ToolManager
{
    public event Action<ToolType, int, int> OnToolCountChanged; // type, max, curr
    public event Action<ToolType> OnSelectedToolChanged;
    private Dictionary<ToolType, int> toolMaxCounts = new();
    private Dictionary<ToolType, int> toolCurrCounts = new();
    public void Initialize(Dictionary<ToolType, int> toolMaxCounts)
    {
        this.toolMaxCounts = new Dictionary<ToolType, int>(toolMaxCounts);
        toolCurrCounts = new Dictionary<ToolType, int>(toolMaxCounts);
    }

    public ToolType SelectedTool { get; private set; } = ToolType.None;

    public bool SelectTool(ToolType type)
    {
        if (type == ToolType.None) 
        {
            SelectedTool = ToolType.None;
            OnSelectedToolChanged?.Invoke(ToolType.None);
            return true;
        }
        else
        {
            if (!toolMaxCounts.ContainsKey(type)) return false;
            SelectedTool = type;
            OnSelectedToolChanged?.Invoke(type);
            return true;
        }
    }

    public bool UseTool() // selected tool을 사용하므로 파라미터 없음
    {
        if (!toolMaxCounts.ContainsKey(SelectedTool)) return false;
        if (toolCurrCounts[SelectedTool] <= 0) return false;

        toolCurrCounts[SelectedTool]--;
        OnToolCountChanged?.Invoke(SelectedTool, toolMaxCounts[SelectedTool], toolCurrCounts[SelectedTool]);

        return true;
    }

    public bool CancelTool(ToolType type)
    {
        if (!toolMaxCounts.ContainsKey(type)) return false;

        toolCurrCounts[type]++;
        OnToolCountChanged?.Invoke(type, toolMaxCounts[type], toolCurrCounts[type]);
        
        return true;
    }
}