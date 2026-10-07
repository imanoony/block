using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Rotate 
{ 
    Null = -1, 
    None = 0, 
    Rotate90 = 90, 
    Rotate180 = 180, 
    Rotate270 = 270 
}


public enum GameState { InGame, Paused, ModuleSelect }

public class GameManager : MonoBehaviour
{
    // temp for cable development
    public bool CableActivated = false;

    #region Singleton
    public static GameManager Instance { get; private set; }
    public WireManager Wire { get; private set; }
    public GridManager Grid { get; private set; }
    public ToolManager Tool { get; private set; }
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

        BlockLibrary = dataParser.ParseBlockData(blockPath);
        ModuleLibrary = dataParser.ParseModuleData(modulePath);
        TutorialLibrary = dataParser.LoadTutorialData(tutorialPath);
        StageLibrary = dataParser.LoadStageData(stagePath);

        Wire = new WireManager();
        Grid = gameObject.GetComponent<GridManager>();
        Tool = gameObject.GetComponent<ToolManager>();
        UI = gameObject.GetComponent<UIManager>();
        Audio = gameObject.GetComponent<AudioManager>();

        Wire.Initialize(true);
        Grid.Initialize();
        UI.Initialize();
        Audio.Initialize();
        Tool.Initialize();

        //dataParser.LoadData(ModuleLibrary, StageLibrary);
    }
    #endregion

    #region Data Library
    private DataParser dataParser = new();
    public Dictionary<int, BlockData> BlockLibrary { get; private set; }
    public Dictionary<int, ModuleData> ModuleLibrary { get; private set; }
    public Dictionary<int, TutorialData> TutorialLibrary { get; private set; }
    public Dictionary<int, StageData> StageLibrary { get; private set; }
    private const string blockPath = "Block", modulePath = "Module", stagePath = "Stages", tutorialPath = "Tutorial";

    #endregion

    #region Test
    void Start()
    {
        State = GameState.ModuleSelect;
        StartModule(0);
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

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            UI.PlayWorldMapTransition(true);
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            UI.PlayWorldMapTransition(false);
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

        Debug.Log($"START STAGE, ID: {stage.ID}");

        outputCheck = new();
        Grid.RemoveCurrentStage();
        Wire.Initialize();
        Tool.InitToolCounts(stage.ToolCounts);
        Grid.InitStage(stage);
        CurrentStage = stage;

        if (stage.IsCleared) UI.StageNextAppear(stage.ID, LastStageID);
        else UI.StageNextDisappear();
        if (CurrentModule.Stages[0] != stage.ID) UI.MenuPrevAppear();
        else UI.MenuPrevDisappear();

        //UI.ResetAppear();
        //UI.QuitToBack();
        //UI.SetStageText(stage.Desc);
        //UI.SetChat(stage.CircuitWidth, stage.CircuitHeight);

        // [TODO Tool 작업]
        // 지금은 그냥 활성화만, 이후 Tool Appear 애니메이션 들어오면
        // 이건 아래의 StageStartTrans로 처리함
        UI.RemoveTool();
        UI.SetTool(stage.ToolCounts);
        UI.UpdateProgress(CurrentModule, stage.ID);

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

                Grid.BlockPlacer.BlockHoverCheck();

                UI.MenuEnable();
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

        // 임시
        CurrentModule.SetCleared();
        foreach (var kvp in ModuleLibrary)
        {
            ModuleData module = kvp.Value;
            if (module.IsCleared) continue;

            List<int> conditions = module.Conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                int cond = conditions[i];
                if (!ModuleLibrary[cond].IsCleared) break;
                if (i == conditions.Count - 1) module.Unlock();
            }
        }
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
        UI.MenuDisappear();
        UI.MenuDisable();
        UI.DisableAllChat();

        State = GameState.ModuleSelect;

        RegisterTransitionEventCallBack(
            TransitionEvent.AllCovered,
            () =>
            {
                Grid.RemoveCurrentStage();

                UI.ClearPanelDisappear();
                UI.StageNextDisappear();

                UI.WorldMapAppear(CurrentModule.ID);
            }
        );

        UI.PlayWorldMapTransition(false);

        /*StartCoroutine(StageEndTrans(
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
                //UI.WorldMapAppear();

                Audio.SoftMute();

                CurrentStage = null;
                CurrentModule = null;

                if (gt != null) gt.ResetGridIdleTime();
            }
        ));*/
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
        if (!module.Unlocked) return;

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
        UI.RemoveProgress();

        State = GameState.Paused;
        int index = module.StageIndex == module.Stages.Count ? 0 : module.StageIndex;
        UI.SetProgress(CurrentModule, index);

        RegisterTransitionEventCallBack(
            TransitionEvent.AllCovered,
            () =>
            {
                UI.WorldMapDisappear();
                StartStage(StageLibrary[module.Stages[index]]);
            }
        );

        UI.PlayWorldMapTransition();
        //UI.ModuleDisappear(
        //    () =>
        //    StartStage(StageLibrary[module.Stages[index]])
        //);
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

        Grid.TilePlacer.TileBoundaryAppear();
        yield return new WaitUntil(
            () =>
            Grid.TilePlacer.TileBoundaryAppearTransDone
        );
        Grid.TilePlacer.TileBoundaryAppearTransDone = false;

        onComplete?.Invoke();
    }

    private IEnumerator StageEndTrans(Action onComplete)
    {
        Grid.TilePlacer.CircuitDisappear();
        Grid.TilePlacer.TileBoundaryDisappear();
        Grid.BlockPlacer.BlockDisappear();

        yield return new WaitUntil(
            () =>
            Grid.TilePlacer.CircuitDisappearTransDone &&
            Grid.TilePlacer.TileBoundaryDisappearTransDone &&
            Grid.BlockPlacer.BlockDisappearTransDone
        );
        Grid.TilePlacer.CircuitDisappearTransDone = false;
        Grid.TilePlacer.TileBoundaryDisappearTransDone = false;
        Grid.BlockPlacer.BlockDisappearTransDone = false;

        onComplete?.Invoke();
    }
    #endregion

    public void RegisterTransitionEventCallBack(
        TransitionEvent transitionEvent,
        Action callback
    )
    {
        UI.RegisterTransitionEventCallBack(transitionEvent, callback);
    }
}