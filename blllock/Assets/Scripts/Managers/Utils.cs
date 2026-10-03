using System;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public const float GRID_IDLE = 10f;
    public const float BLOCK_Z = -2f;
    public const int BLOCK_SORT_NORMAL = 20;
    public const int BLOCK_SORT_ACTION = 30;
    public const int BLOCK_SORT_DRAG = 40;
    public const int SCALE_FACTOR = 625;
    public const int DENOMINATOR = 100;
    public const int TILE_SPACING = 100;
    public const int GRID_TEXT_SPACING = 30;
    public const int PORT_OFFSET = 20;
    public const int PORT_SIZE = 10;
    public const string RED = "#F25A7B";
    public const string CHAT_RED = "#FFD0D0";
    public const string BLUE = "#54DCE3";
    public const string CHAT_BLUE = "#D0F6FF";
    public const string BLACK = "#242424";
    public const string GRAY = "#B8B8B8";
    public const string GREEN = "#CAFFCA";
    public const string YELLOW = "#FEFCCD";
    public const string CLEAR = "#F0F0F0FF";
    public const string TAG_ROTATE = "#99C79D";
    public const string TAG_FLIP = "#C29363";
    public const float CLEAR_ALPHA = 1f;
    public const float THRESHOLD = 3f;
    public const int MAX_SNAP_COUNT = 20;
    public const float TILE_FILL_PERCENT1 = 0.5f;
    public const float TILE_FILL_PERCENT2 = 0.7f;
    public const float FILL_THRESHOLD = 9;
    public const int PPU = 24;
    public const int MAX_PORT = 8;
    public static readonly Vector3 BLOCK_SHADOW = new(0.05f, 0.05f, 0);
    public static readonly Vector3 TAG_SHADOW = new(0.015f, 0.015f, 0);
    public static readonly Vector3 CABLE_SHADOW =  new(0.03f, 0.03f, 0);
    public const float SHADOW_ALPHA = 100 / 255f;
    public const float MODULE_HIGHLIGHT_SCALE = 1.2f;
    public const int MODULE_MIN = 0;
    public const int MODULE_MAX = 11;
    public const int AUDIO_THRESHOLD0 = 2;
    public const int AUDIO_THRESHOLD1 = 3;
    public const int AUDIO_THRESHOLD2 = 4;
    public const int CABLE_ANIM_EDGE_COUNT = 25;
    public const int CABLE_ANIM_NODE_COUNT = 15;
    public const int CABLE_ANIM_NODE_CURVE_COUNT = 12;
    public const float TOOL_OFFSET_X = -80f;
    public const float TOOL_OFFSET_Y = 120f;
    public const float PROGRESS_MAX_HEIGHT = 900f;
    public const float PROGRESS_OFFSET_X = 80f;
    public const float PROGRESS_OFFSET_Y = 75f;
    public const string THUMBNAILS_PATH = "Thumbnails";
    public const char NOT = '~', VERT = '*', HORZ = '+';
    public const string PARENS = "()";
    public static bool IsWrappedByParentheses(string s)
    {
        if (s.Length < 2 || s[0] != PARENS[0] || s[^1] != PARENS[1]) return false;
        int depth = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == PARENS[0]) depth++;
            else if (s[i] == PARENS[1]) depth--;

            if (i < s.Length - 1 && depth == 0) return false;
        }
        return depth == 0;
    }
    public static void PrintWarning(string message)
    {
        Debug.LogWarning($"<color=orange>[{DateTime.Now:HH:mm:ss}] Warning:</color> {message}");
    }
    public static void PrintError(string message)
    {
        Debug.LogError($"<color=red>[{DateTime.Now:HH:mm:ss}] Error:</color> {message}");
    }
    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1); // [0, i] 범위
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]); // C# 7 튜플 스왑
        }
    }

    public static Rect Boundary { get; private set; }
    public static void SetBoundary(Rect boundary) => Boundary = boundary;

    public static Color CodeToColor(string colorCode)
    {
        if (string.IsNullOrWhiteSpace(colorCode)) return Color.white; // default fallback

        Color color;
        if (ColorUtility.TryParseHtmlString(colorCode, out color)) return color;
        else { PrintError("[CodeToColor] cannot parse"); return Color.white; }
    }

    public static Vector3 GetBlockShadowOffset(Vector2 offset, Rotate rotate, bool flipX, bool flipY)
    {
        Vector3 off = offset;
        if (rotate == Rotate.Rotate90) off = new Vector3(-off.y, off.x, 0);
        else if (rotate == Rotate.Rotate180) off = -off;
        else if (rotate == Rotate.Rotate270) off = new Vector3(off.y, -off.x, 0);

        if (flipX) off.x = -off.x;
        else if (flipY) off.y = -off.y;
        return off;
    }

    public static Vector3 GetCableShadowOffset(Vector2 offset, Rotate rotate)
    {
        Vector3 off = offset;
        if (rotate == Rotate.Rotate90) off = new Vector3(-off.y, off.x, 0);
        else if (rotate == Rotate.Rotate180) off = -off;
        else if (rotate == Rotate.Rotate270) off = new Vector3(off.y, -off.x, 0);

        return off;
    }

    public static bool IsAdjacentGrids(Vector2Int a, Vector2Int b)
    {
        if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
            return false;

        return true;
    }
    
    public static (Vector2Int, Vector2Int) SortPositions(Vector2Int a, Vector2Int b)
    {
        Vector2Int A, B;

        if (a.x < b.x || (a.x == b.x && a.y < b.y))
        {
            A = a; 
            B = b;
        }
        else
        {
            A = b;
            B = a;
        }

        return (A, B);
    }

    public static Edge ToEdge(EdgeType type, Vector2Int a, Vector2Int b)
    {
        (Vector2Int A, Vector2Int B) = SortPositions(a, b);
        if (A.x == B.x) // Horizontal
        {
            return new HEdge(new(A.x, A.y), type);
        }
        else // Vertical
        {
            return new VEdge(new(A.x, A.y), type);
        }
    }
}