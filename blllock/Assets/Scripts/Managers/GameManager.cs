using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class Utils
{
    public const float BLOCK_Z = -2f;
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
    public const float CLEAR_ALPHA = 0.4f;
    public const float THRESHOLD = 0.6f;
    public const float TILE_FILL_PERCENT1 = 0.5f;
    public const float TILE_FILL_PERCENT2 = 0.7f;
    public const float FILL_THRESHOLD = 9;
    public const int PPU = 24;
    public const int MAX_PORT = 4;
    public static readonly Vector3 HOVER = new Vector3(0.1f, 0.1f, 0);
    public const float SHADOW_ALPHA = 100 / 255f;
    public const float MODULE_HIGHLIGHT_SCALE = 1.2f;
    public const int MODULE_MIN = 0;
    public const int MODULE_MAX = 4;
    public const int AUDIO_THRESHOLD0 = 2;
    public const int AUDIO_THRESHOLD1 = 4;
    public const int AUDIO_THRESHOLD2 = 6;
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

    public static Vector3 GetHoverOffset(Rotate rotate, bool flip)
    {
        Vector3 offset = HOVER;
        if (rotate == Rotate.Rotate90) offset = new Vector3(-offset.y, offset.x, 0);
        else if (rotate == Rotate.Rotate180) offset = -offset;
        else if (rotate == Rotate.Rotate270) offset = new Vector3(offset.y, -offset.x, 0);

        //if (flip) offset.x = -offset.x;
        return offset;
    }
}

public enum GameState { InGame, Paused, ModuleSelect }

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
        Audio = gameObject.GetComponent<AudioManager>();

        Wire.Initialize(true);
        Grid.Initialize();
        UI.Initialize();
        Audio.Initialize();

        BlockLibrary = dataParser.ParseBlockData(blockPath);
        ModuleLibrary = dataParser.ParseModuleData(modulePath);
        StageLibrary = dataParser.ParseStageData(stagePath);

        dataParser.LoadData(ModuleLibrary, StageLibrary);
    }
    #endregion

    #region Data Library
    private DataParser dataParser = new();
    public Dictionary<int, BlockData> BlockLibrary { get; private set; }
    public Dictionary<int, ModuleData> ModuleLibrary { get; private set; }
    public Dictionary<int, StageData> StageLibrary { get; private set; }
    private const string blockPath = "Block", modulePath = "Module", stagePath = "Stage";

    #endregion

    #region Test
    void Start()
    {
        State = GameState.ModuleSelect;
        StartModule(0);
    }
    #endregion

    public GameState State { get; private set; } = GameState.Paused;
    public ModuleData CurrentModule { get; private set; } = null;
    public int LastStageID { get; private set; } = -1;
    public StageData CurrentStage { get; private set; } = null;
    private Dictionary<Vector2Int, bool> outputCheck = new();

    // 스테이지를 시작한다
    public void StartGame(StageData stage)
    {
        if (State != GameState.Paused) { Utils.PrintError("게임이 이미 진행 중입니다."); return; }

        outputCheck = new();
        Grid.RemoveCurrentStage();
        Wire.Initialize();
        Grid.InitStage(stage);
        CurrentStage = stage;

        if (stage.IsCleared) UI.NextAppear(stage.ID, LastStageID);
        else UI.NextDisappear();
        if (CurrentModule.Stages[0] != stage.ID) UI.PrevAppear();
        else UI.PrevDisappear();

        UI.ResetAppear();
        UI.QuitToBack();
        UI.SetStageText(stage.Desc);

        Audio.ResetBGM();

        for (int i = 0; i < CurrentStage.Outputs.Count; i++)
            outputCheck[new Vector2Int(CurrentStage.Outputs[i].pos.x, CurrentStage.Outputs[i].pos.y)] = false;

        State = GameState.InGame;
    }
    public void StartGame(int id) => StartGame(StageLibrary[id]);

    // 스테이지를 성공 처리한다
    public void SucceedGame()
    {
        State = GameState.Paused;

        if (CurrentStage.ID == CurrentModule.StageIndex) CurrentModule.UpStageIndex();
        CurrentStage.SetCleared(true);

        UI.NextAppear(CurrentStage.ID, LastStageID);
        UI.ResetDisappear();
    }

    public void OutputCheck(Vector2Int pos, bool state)
    {
        if (!outputCheck.ContainsKey(pos)) { Utils.PrintError($"OutputCheck: 해당 위치에 Output이 없습니다. {pos}"); return; }
        outputCheck[pos] = state;

        foreach (var check in outputCheck)
            if (!check.Value) return;

        SucceedGame();
    }

    // 스테이지를 초기화한다
    public void ResetGame()
    {
        State = GameState.Paused;
        Grid.RemoveCurrentStage();
        Grid.InitStage(CurrentStage);
        State = GameState.InGame;
    }

    public void BackGame()
    {
        State = GameState.ModuleSelect;

        Grid.RemoveCurrentStage();

        UI.NextDisappear();
        UI.PrevDisappear();
        UI.BackToQuit();
        UI.ResetDisappear();

        UI.SetStageText(CurrentModule.Desc);
        UI.ModuleAppear();

        Audio.SoftMute();

        CurrentStage = null;
        CurrentModule = null;
    }

    public void QuitGame()
    {
        dataParser.SaveData(ModuleLibrary);
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터에서는 Play 모드 종료
    #else
        Application.Quit(); // 빌드된 게임에서는 종료
    #endif
    }

    public void NextStage()
    {
        if (CurrentStage == null) return;
        if (StageLibrary.Count == 0) return;

        State = GameState.Paused;
        int index = CurrentModule.Stages.IndexOf(CurrentStage.ID);
        if (CurrentModule.Stages.Count > index + 1) StartGame(CurrentModule.Stages[index + 1]);
        else BackGame();
    }

    public void PrevStage()
    {
        if (CurrentStage == null) return;
        if (StageLibrary.Count == 0) return;

        State = GameState.Paused;
        int index = CurrentModule.Stages.IndexOf(CurrentStage.ID);
        StartGame(CurrentModule.Stages[index - 1]);
    }

    public void StartModule(ModuleData module)
    {
        if (State != GameState.ModuleSelect) { Utils.PrintError("모듈 선택 상태가 아닙니다."); return; }
        if (module == null) { Utils.PrintError("모듈이 없습니다."); return; }

        CurrentModule = module;
        LastStageID = module.Stages[^1];
        UI.ModuleDisappear();

        Audio.SoftUnmute();

        State = GameState.Paused;
        int index = module.StageIndex == module.Stages.Count ? 0 : module.StageIndex;
        StartGame(StageLibrary[module.Stages[index]]);
    }
    public void StartModule(int id) => StartModule(ModuleLibrary[id]);
}