using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public const string BLUE = "#6FCED5";
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
    public const int MAX_PORT = 4;
    public static readonly Vector3 BLOCK_SHADOW = new(0.05f, 0.05f, 0);
    public static readonly Vector3 TAG_SHADOW = new(0.015f, 0.015f, 0);
    public const float SHADOW_ALPHA = 100 / 255f;
    public const float MODULE_HIGHLIGHT_SCALE = 1.2f;
    public const int MODULE_MIN = 0;
    public const int MODULE_MAX = 6;
    public const int AUDIO_THRESHOLD0 = 2;
    public const int AUDIO_THRESHOLD1 = 3;
    public const int AUDIO_THRESHOLD2 = 4;
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

    public static Vector3 GetHoverOffset(Vector2 offset, Rotate rotate, bool flipX, bool flipY)
    {
        Vector3 off = offset;
        if (rotate == Rotate.Rotate90) off = new Vector3(-off.y, off.x, 0);
        else if (rotate == Rotate.Rotate180) off = -off;
        else if (rotate == Rotate.Rotate270) off = new Vector3(off.y, -off.x, 0);

        if (flipX) off.x = -off.x;
        else if (flipY) off.y = -off.y;
        return off;
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
        TutorialLibrary = dataParser.LoadTutorialData(tutorialPath);
        StageLibrary = dataParser.LoadStageData(stagePath);

        //dataParser.LoadData(ModuleLibrary, StageLibrary);
    }
    #endregion

    #region Data Library
    private DataParser dataParser = new();
    public Dictionary<int, BlockData> BlockLibrary { get; private set; }
    public Dictionary<int, ModuleData> ModuleLibrary { get; private set; }
    public Dictionary<int, TutorialData> TutorialLibrary { get; private set; }
    public Dictionary<int, StageData> StageLibrary { get; private set; }
    private const string blockPath = "Block", modulePath = "Module", stagePath = "Stage", tutorialPath = "Tutorial";

    #endregion

    #region Test
    void Start()
    {
        State = GameState.ModuleSelect;
        StartModule(6);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            List<StageData> stages = new();
            foreach (var kvp in StageLibrary)
            {
                stages.Add(kvp.Value);
            }
            dataParser.SaveStageData(stages, stagePath);
        }
    }
    #endregion
    public GameState State { get; private set; } = GameState.Paused;
    public void SetState(GameState state) => State = state;
    public bool IsOnAction { get; private set; } = false;
    public bool ActionOn() => IsOnAction = true;
    public bool ActionOff() => IsOnAction = false;
    public ModuleData CurrentModule { get; private set; } = null;
    public int LastStageID { get; private set; } = -1;
    public StageData CurrentStage { get; private set; } = null;
    private Dictionary<Vector2Int, bool> outputCheck = new();
    private GridTooltip gt;
    public void ResetGridIdleTile() { if (gt != null) gt.ResetGridIdleTime(); }

    // 스테이지를 시작한다
    public void StartStage(StageData stage)
    {
        if (State != GameState.Paused) { Utils.PrintError("게임이 이미 진행 중입니다."); return; }

        if (delay != null) { StopCoroutine(delay); delay = null; }

        outputCheck = new();
        Grid.RemoveCurrentStage();
        Wire.Initialize();
        Grid.InitStage(stage);
        CurrentStage = stage;

        if (stage.IsCleared) UI.StageNextAppear(stage.ID, LastStageID);
        else UI.StageNextDisappear();
        if (CurrentModule.Stages[0] != stage.ID) UI.MenuPrevAppear();
        else UI.MenuPrevDisappear();

        //UI.ResetAppear();
        //UI.QuitToBack();
        //UI.SetStageText(stage.Desc);
        UI.SetChat(stage.CircuitWidth, stage.CircuitHeight);

        Audio.ResetBGM();

        if (CurrentModule.ID == 0) gt.Initialize(stage.Inputs[0].pos);

        for (int i = 0; i < CurrentStage.Outputs.Count; i++)
            outputCheck[new Vector2Int(CurrentStage.Outputs[i].pos.x, CurrentStage.Outputs[i].pos.y)] = false;

        State = GameState.Paused;

        StartCoroutine(StageStartTrans(
            () =>
            {
                for (int i = 0; i < CurrentStage.Inputs.Count; i++) UI.EnableChat(CurrentStage.Inputs[i].pos);
                for (int i = 0; i < CurrentStage.Outputs.Count; i++) UI.EnableChat(CurrentStage.Outputs[i].pos);

                // 튜토리얼이 있다면 재생
                if (stage.TutorialID != -1)
                {
                    if (!stage.IsCleared)
                    {
                        StartTutorial();
                    }
                    UI.MenuTutorialAppear();
                    State = GameState.InGame;
                }
                else
                {
                    UI.MenuTutorialDisappear();
                    State = GameState.InGame;
                }
            }
        ));
    }
    public void StartStage(int id) => StartStage(StageLibrary[id]);

    private bool onTutorial = false;
    public void StartTutorial()
    {
        if (CurrentStage == null) return;
        if (CurrentStage.TutorialID == -1) return;

        UI.MenuDisable();

        State = GameState.Paused;
        UI.MenuDisappear();
        UI.OpenTutorialPopup(TutorialLibrary[CurrentStage.TutorialID]);
    }

    private Coroutine delay = null;
    private IEnumerator DelayChatStart()
    {
        yield return null;

        if (CurrentStage == null) yield break;
        for (int i = 0; i < CurrentStage.Inputs.Count; i++) UI.EnableChat(CurrentStage.Inputs[i].pos);
        for (int i = 0; i < CurrentStage.Outputs.Count; i++) UI.EnableChat(CurrentStage.Outputs[i].pos);

        yield return new WaitForSeconds(5f);

        if (CurrentStage == null) yield break;
        for (int i = 0; i < CurrentStage.Inputs.Count; i++) UI.DisableChat(CurrentStage.Inputs[i].pos);
        for (int i = 0; i < CurrentStage.Outputs.Count; i++) UI.DisableChat(CurrentStage.Outputs[i].pos);

        if (gt != null) gt.StartCheck();

        delay = null;
    }

    // 스테이지를 성공 처리한다
    public void SucceedGame()
    {
        State = GameState.Paused;

        if (CurrentModule.Stages.IndexOf(CurrentStage.ID) == CurrentModule.StageIndex) CurrentModule.UpStageIndex();
        CurrentStage.SetCleared(true);

        UI.ClearPanelAppear();
        UI.StageNextAppear(CurrentStage.ID, LastStageID);
        //UI.ResetDisappear();

        if (gt != null) gt.StopCheck();
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

        if (gt != null) gt.ResetGridIdleTime();
    }

    public void BackGame()
    {
        UI.MenuDisable();
        UI.DisableAllChat();

        StartCoroutine(StageEndTrans(
            () =>
            {
                State = GameState.ModuleSelect;

                Grid.RemoveCurrentStage();

                UI.ClearPanelDisappear();
                UI.StageNextDisappear();
                UI.MenuPrevDisappear();
                UI.MenuTutorialDisappear();
                UI.MenuBackDisappear();
                UI.MenuQuitAppear();
                UI.MenuDisappear();
                //UI.BackToQuit();
                //UI.ResetDisappear();

                int achievement = (int)(100 * (float)CurrentModule.StageIndex / CurrentModule.Stages.Count);
                string text = $"{CurrentModule.Desc} ({achievement}%)";
                //UI.SetStageText(text);
                UI.ModuleAppear();

                Audio.SoftMute();

                CurrentStage = null;
                CurrentModule = null;

                if (gt != null) gt.ResetGridIdleTime();
            }
        ));
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

        
        UI.DisableAllChat();

        StartCoroutine(StageEndTrans(
            () =>
            {
                State = GameState.Paused;
                UI.MenuDisappear();
                UI.ClearPanelDisappear();
                int index = CurrentModule.Stages.IndexOf(CurrentStage.ID);
                if (CurrentModule.Stages.Count > index + 1) StartStage(CurrentModule.Stages[index + 1]);
                else BackGame();
            }
        ));
    }

    public void PrevStage()
    {
        if (CurrentStage == null) return;
        if (StageLibrary.Count == 0) return;

        UI.MenuDisable();
        UI.DisableAllChat();

        StartCoroutine(StageEndTrans(
            () =>
            {
                State = GameState.Paused;
                UI.MenuDisappear();
                UI.ClearPanelDisappear();
                int index = CurrentModule.Stages.IndexOf(CurrentStage.ID);
                StartStage(CurrentModule.Stages[index - 1]);
            }
        ));        
    }

    public void StartModule(ModuleData module)
    {
        if (State != GameState.ModuleSelect) { Utils.PrintError("모듈 선택 상태가 아닙니다."); return; }
        if (module == null) { Utils.PrintError("모듈이 없습니다."); return; }
        if (module.Stages.Count == 0) return;

        //if (module.ID == 0) UI.DeactivateChat();
        //else UI.ActivateChat();

        CurrentModule = module;
        LastStageID = module.Stages[^1];

        Audio.SoftUnmute();

        if (CurrentModule.ID == 0)
        {
            if (gt == null) gt = gameObject.AddComponent<GridTooltip>();
        }
        else
        {
            if (gt != null) { Destroy(gt); gt = null; }
        }

        UI.MenuBackAppear();
        UI.MenuQuitDisappear();

        State = GameState.Paused;
        int index = module.StageIndex == module.Stages.Count ? 0 : module.StageIndex;

        
        UI.ModuleDisappear(
            () =>
            StartStage(StageLibrary[module.Stages[index]])
        );
    }
    public void StartModule(int id) => StartModule(ModuleLibrary[id]);

    #region Transition

    private IEnumerator StageStartTrans(Action onComplete)
    {
        Grid.TilePlacer.CircuitAppear();
        Grid.BlockPlacer.BlockAppear();

        yield return new WaitUntil(
            () =>
            Grid.TilePlacer.CircuitAppearTransDone &&
            Grid.BlockPlacer.BlockAppearTransDone
        );
        Grid.TilePlacer.CircuitAppearTransDone = false;
        Grid.BlockPlacer.BlockAppearTransDone = false;

        onComplete?.Invoke();
    }

    private IEnumerator StageEndTrans(Action onComplete)
    {
        Grid.TilePlacer.CircuitDisappear();
        Grid.BlockPlacer.BlockDisappear();

        yield return new WaitUntil(
            () =>
            Grid.TilePlacer.CircuitDisappearTransDone &&
            Grid.BlockPlacer.BlockDisappearTransDone
        );
        Grid.TilePlacer.CircuitDisappearTransDone = false;
        Grid.BlockPlacer.BlockDisappearTransDone = false;

        onComplete?.Invoke();
    }
    #endregion
}