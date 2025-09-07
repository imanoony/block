using System;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public const int SCALE_FACTOR = 625;
    public const int DENOMINATOR = 100;
    public const int TILE_SPACING = 100;
    public const int GRID_TEXT_SPACING = 30;
    public const int PORT_OFFSET = 20;
    public const int PORT_SIZE = 10;
    public const string RED = "#F25A7B";
    public const string BLUE = "#54DCE3";
    public const string BLACK = "#242424";
    public const string GRAY = "#B8B8B8";
    public const float THRESHOLD = 0.6f;
    public const float TILE_FILL_PERCENT = 0.7f;
    public const int PPU = 24;
    public const int MAX_PORT = 4;
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
}

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    public WireManager Wire { get; private set; }
    public GridManager Grid { get; private set; }
    public UIManager UI { get; private set; }
    public AudioManager Audio { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지됨
        Wire = new WireManager();
        Grid = gameObject.GetComponent<GridManager>();
        UI = gameObject.GetComponent<UIManager>();

        Wire.Initialize(true);
        Grid.Initialize();
        UI.Initialize();

        BlockLibrary = dataParser.ParseBlockData(blockPath);
        StageLibrary = dataParser.ParseStageData(stagePath);
    }
    #endregion

    #region Data Library
    private DataParser dataParser = new();
    public Dictionary<int, BlockData> BlockLibrary { get; private set; }
    public Dictionary<int, StageData> StageLibrary { get; private set; }
    private const string blockPath = "Block.csv", stagePath = "Stage.csv";

    #endregion

    #region Test
    void Start()
    {
        StartGame(99);
    }
    #endregion

    // 스테이지를 시작한다
    public void StartGame(StageData stage)
    {
        Grid.InitStage(stage);
    }
    public void StartGame(int id) => StartGame(StageLibrary[id]);

    // 스테이지를 성공 처리한다
    public void SucceedGame()
    {

    }

    // 스테이지를 초기화한다
    public void ResetGame()
    {

    }

    // 다음 스테이지로 이동한다
    // 다음 스테이지의 ID를 반환한다
    public int NextStage()
    {
        return 0;
    }

    // 이전 스테이지로 이동한다
    // 이전 스테이지의 ID를 반환한다
    public int PrevStage()
    {
        return 0;
    }
}