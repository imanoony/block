using UnityEngine;
using System;

public static class Utils
{
    public const int SCALE_FACTOR = 625;
    public const int DENOMINATOR = 100;
    public const int TILE_SPACING = 100;
    public const int GRID_TEXT_SPACING = 30;
    public const int PORT_OFFSET = 20;
    public const int PORT_SIZE = 10;
    public const string RED = "#FE817D";
    public const string BLUE = "#7391FE";
    public const string BLACK = "#242424";
    public const string GRAY = "#717171";
    public const float THRESHOLD = 5f;
    public static void PrintWarning(string message)
    {
        Debug.LogWarning($"<color=orange>[{DateTime.Now:HH:mm:ss}] Warning:</color> {message}");
    }
    public static void PrintError(string message)
    {
        Debug.LogError($"<color=red>[{DateTime.Now:HH:mm:ss}] Error:</color> {message}");
    }
}