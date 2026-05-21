using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;

        InitModule();

        initialized = true;
    }

    #region Chat 
    [Header("Chat")]
    [SerializeField] private UIChat chat;
    private Vector2 chatOffset = new(15, 40);
    public void SetChat(int circuitWidth, int circuitHeight)
    {
        chat.SetChat(circuitWidth, circuitHeight);
    }
    public bool IsChatEnabled(Vector2Int pos) => chat.IsChatEnabled(pos);
    public void EnableChat(int x, int y) => chat.EnableChat(x, y);
    public void EnableChat(Vector2Int pos) => EnableChat(pos.x, pos.y);
    public void DisableChat(int x, int y) => chat.DisableChat(x, y);
    public void DisableChat(Vector2Int pos) => DisableChat(pos.x, pos.y);
    public void DisableAllChat() => chat.DisableAllChat();
    #endregion

    #region Menu
    [Header("Menu")]
    [SerializeField] private GameObject menu;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button backButton;
    
    
    public void MenuAppear()
    {
        if (menu.activeSelf) return;

        menu.SetActive(true);
        menuButton.interactable = false;
        menuButton.gameObject.SetActive(false);
    }

    public void MenuDisappear()
    {
        if (!menu.activeSelf) return;

        menu.SetActive(false);
        menuButton.interactable = true;
        menuButton.gameObject.SetActive(true);
    }

    public void SetStageText(string text) => stageText.text = text;

    public void NextAppear(int stageID, int lastStageID)
    {
        nextButton.interactable = true;
        nextButton.gameObject.SetActive(true);

        if (stageID == lastStageID) nextButton.gameObject.GetComponent<TextMeshProUGUI>().text = "COMPLETE";
        else nextButton.gameObject.GetComponent<TextMeshProUGUI>().text = "NEXT";
    }
    public void NextDisappear() { nextButton.interactable = false; nextButton.gameObject.SetActive(false); }
    
    public void PrevAppear() { prevButton.interactable = true; prevButton.gameObject.SetActive(true); }
    public void PrevDisappear() { prevButton.interactable = false; prevButton.gameObject.SetActive(false); }

    public void ResetAppear() { resetButton.interactable = true; resetButton.gameObject.SetActive(true);}
    public void ResetDisappear() { resetButton.interactable = false; resetButton.gameObject.SetActive(false); }

    public void ResetButton() => GameManager.Instance.ResetGame();
    public void MenuButton() => MenuAppear();
    public void CloseButton() => MenuDisappear();

    private bool isQuit = false;
    public void BackButton()
    {
        DisableAllChat();

        if (isQuit) GameManager.Instance.QuitGame();
        else GameManager.Instance.BackGame();
    }
    public void BackToQuit()
    {
        backButton.gameObject.GetComponent<TextMeshProUGUI>().text = "QUIT";
        isQuit = true;
    }
    public void QuitToBack()
    {
        backButton.gameObject.GetComponent<TextMeshProUGUI>().text = "BACK";
        isQuit = false;
    }

    public void NextButton()
    {
        DisableAllChat();
        GameManager.Instance.NextStage();
    }
    public void PrevButton()
    {
        DisableAllChat();
        GameManager.Instance.PrevStage();
    }
    #endregion

    #region Block Tooltip
    [Header("Block Tooltip")]
    [SerializeField] private GameObject blockTooltip;
    [SerializeField] private TextMeshProUGUI blockTooltipText;

    public void BlockTooltipAppear(bool canRotate, bool canFlip, Vector3 worldPos)
    {
        if (blockTooltip.activeSelf) return;
        if (!canRotate && !canFlip) return;

        // 툴팁 텍스트 설정
        if (canRotate) blockTooltipText.text = "Rotate";
        else if (canFlip) blockTooltipText.text = "Flip";

        // 월드 좌표 -> 스크린 좌표
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // Canvas 기준 로컬 좌표 계산
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        // offset 적용 후 툴팁 위치 설정
        blockTooltip.GetComponent<RectTransform>().anchoredPosition = localPoint + chatOffset;
        blockTooltip.SetActive(true);
    }

    public void BlockTooltipDisappear() => blockTooltip.SetActive(false);
    #endregion

    #region Grid Tooltip
    [Header("Grid Tooltip")]
    [SerializeField] private RectTransform gridTooltip;

    public void GridTooltipAppear(Vector2Int gridPos)
    {
        Vector3 pos = chat.GetChatPosUI(gridPos.x, gridPos.y, canvas);
        gridTooltip.anchoredPosition = pos;
        gridTooltip.gameObject.SetActive(true);
    }

    public void GridTooltipDisappear() => gridTooltip.gameObject.SetActive(false);
    #endregion

    #region Module UI
    [Header("Module")]
    [SerializeField] private GameObject moduleParent;
    [SerializeField] private Sprite[] moduleSprites;
    private Sprite TryToGetSprite(int moduleID)
    {
        if (moduleSprites.Length > moduleID + 1 && moduleID >= 0) return moduleSprites[moduleID + 1];
        else return moduleSprites[0];
    }

    private List<GameObject> modulePool = new();
    private List<Image> moduleImages = new();
    private List<Vector3> modulePositions = new();
    private int moduleFocus = 2; // 현재 선택된 모듈 ID, -1은 null과 동일
    private const int moduleCenter = 2;

    private void InitModule()
    {
        for (int i = 0; i < moduleParent.transform.childCount; i++)
        {
            modulePool.Add(moduleParent.transform.GetChild(i).gameObject);
            moduleImages.Add(modulePool[i].GetComponent<Image>());
            modulePositions.Add(modulePool[i].transform.localPosition);
        }

        RectTransform rt = moduleParent.GetComponent<RectTransform>();
        rt.offsetMax = new(0, -Screen.height);
        rt.offsetMin = new(0, -Screen.height);
    }

    private float moduleAppearTime = 0.4f;
    private Coroutine moduleAppearCoroutine = null;
    public void ModuleAppear()
    {
        if (moduleAppearCoroutine != null) StopCoroutine(moduleAppearCoroutine);
        moduleAppearCoroutine = StartCoroutine(ModuleAppearTrans());
    }
    public void ModuleDisappear()
    {
        if (moduleAppearCoroutine != null) StopCoroutine(moduleAppearCoroutine);
        moduleAppearCoroutine = StartCoroutine(ModuleDisappearTrans());
    }
    private IEnumerator ModuleAppearTrans()
    {
        moduleParent.SetActive(true);

        // Set Module Sprites
        moduleImages[moduleCenter].sprite = TryToGetSprite(moduleFocus);
        moduleImages[moduleCenter - 1].sprite = TryToGetSprite(moduleFocus - 1);
        moduleImages[moduleCenter + 1].sprite = TryToGetSprite(moduleFocus + 1);
        if (moduleFocus == Utils.MODULE_MIN) modulePool[moduleCenter - 1].SetActive(false);
        else if (moduleFocus == Utils.MODULE_MAX) modulePool[moduleCenter + 1].SetActive(false);

        // Transition
        RectTransform rt = moduleParent.GetComponent<RectTransform>();

        Vector2 currentMin = rt.offsetMin, currentMax = rt.offsetMax;
        Vector2 targetMin = Vector2.zero, targetMax = Vector2.zero;

        Vector2 velocityMin = Vector2.zero;
        Vector2 velocityMax = Vector2.zero;

        while ((currentMin - targetMin).sqrMagnitude > 0.01f || (currentMax - targetMax).sqrMagnitude > 0.01f)
        {
            // SmoothDamp
            currentMin = Vector2.SmoothDamp(currentMin, targetMin, ref velocityMin, moduleAppearTime);
            currentMax = Vector2.SmoothDamp(currentMax, targetMax, ref velocityMax, moduleAppearTime);

            rt.offsetMin = currentMin;
            rt.offsetMax = currentMax;

            yield return null;
        }

        // Final Modification
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        EnableModuleClick();
        moduleAppearCoroutine = null;
        yield break;
    }

    public IEnumerator ModuleDisappearTrans()
    {
        DisableModuleClick();

        RectTransform rt = moduleParent.GetComponent<RectTransform>();

        Vector2 currentMin = rt.offsetMin, currentMax = rt.offsetMax;
        Vector2 targetMin = new(0, -Screen.height), targetMax = new(0, -Screen.height);

        Vector2 velocityMin = Vector2.zero;
        Vector2 velocityMax = Vector2.zero;
        
        while ((currentMin - targetMin).sqrMagnitude > 0.01f || (currentMax - targetMax).sqrMagnitude > 0.01f)
        {
            // SmoothDamp
            currentMin = Vector2.SmoothDamp(currentMin, targetMin, ref velocityMin, moduleAppearTime);
            currentMax = Vector2.SmoothDamp(currentMax, targetMax, ref velocityMax, moduleAppearTime);

            rt.offsetMin = currentMin;
            rt.offsetMax = currentMax;

            yield return null;
        }

        rt.offsetMin = new(0, -Screen.height);
        rt.offsetMax = new(0, -Screen.height);

        moduleFocus = GameManager.Instance.CurrentModule.ID;
        moduleParent.SetActive(false);

        moduleAppearCoroutine = null;
        yield break;
    }

    private void EnableModuleClick() { for (int i = 0; i < modulePool.Count; i++) modulePool[i].GetComponent<Button>().interactable = true; }

    private void DisableModuleClick() { for (int i = 0; i < modulePool.Count; i++) modulePool[i].GetComponent<Button>().interactable = false; }

    public void ScrollModule(GameObject module)
    {
        int index = modulePool.IndexOf(module);
        if (index == -1) return; // module이 modulePool에 없는 경우
        if (scrollCoroutine != null) return; // 이미 스크롤 중인 경우

        if (index < moduleCenter) scrollCoroutine = StartCoroutine(LeftScrollModule());
        else if (index > moduleCenter) scrollCoroutine = StartCoroutine(RightScrollModule());

        // index == moduleCenter
        else GameManager.Instance.StartModule(moduleFocus);
    }

    private Coroutine scrollCoroutine = null;
    private float scrollTime = 0.35f; // 스크롤 애니메이션 시간

    private IEnumerator LeftScrollModule() // 더 낮은 모듈로
    {
        if (moduleFocus == -1) yield break;
        if (moduleFocus == Utils.MODULE_MIN) yield break;

        moduleFocus--;
        moduleImages[moduleCenter - 2].sprite = TryToGetSprite(moduleFocus - 1);
        if (moduleFocus == Utils.MODULE_MIN)
            modulePool[moduleCenter - 2].SetActive(false);

        ModuleData module = GameManager.Instance.ModuleLibrary[moduleFocus];
        int achievement = module.Stages.Count != 0 ? (int)(100 * (float)module.StageIndex / module.Stages.Count) : 0;
        string text = $"{module.Desc} ({achievement}%)";
        SetStageText(text);

        List<Vector2> targetPositions = new List<Vector2>();
        List<Vector3> targetScales = new List<Vector3>();
        List<Color> targetColors = new List<Color>();

        for (int i = 0; i < modulePool.Count - 1; i++)
        {
            targetPositions.Add(modulePositions[i + 1]);
            if (i == moduleCenter - 1)
            {
                targetScales.Add(Vector3.one * Utils.MODULE_HIGHLIGHT_SCALE);
                targetColors.Add(Color.white);
            }
            else
            {
                targetScales.Add(Vector3.one);
                targetColors.Add(Utils.CodeToColor(Utils.GRAY));
            }
        }

        List<Vector2> startPositions = new List<Vector2>();
        List<Vector3> startScales = new List<Vector3>();
        List<Color> startColors = new List<Color>();
        List<RectTransform> moduleRects = new();
        List<Image> moduleLocalImages = new();
        for (int i = 0; i < modulePool.Count - 1; i++)
        {
            RectTransform rt = modulePool[i].GetComponent<RectTransform>();
            moduleRects.Add(rt);
            startPositions.Add(rt.anchoredPosition);
            startScales.Add(rt.localScale);

            Image img = modulePool[i].GetComponent<Image>();
            moduleLocalImages.Add(img);
            startColors.Add(img.color);
        }

        // 코루틴으로 보간
        float elapsed = 0f;
        while (elapsed < scrollTime)
        {
            float t = elapsed / scrollTime;

            for (int i = 0; i < modulePool.Count - 1; i++)
            {
                moduleRects[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);
                moduleRects[i].localScale = Vector3.Lerp(startScales[i], targetScales[i], t);
                moduleLocalImages[i].color = Color.Lerp(startColors[i], targetColors[i], t);
            }

            elapsed += Time.smoothDeltaTime;
            yield return null;
        }

        // 마지막 보정
        for (int i = 0; i < modulePool.Count - 1; i++)
        {
            moduleRects[i].anchoredPosition = targetPositions[i];
            moduleRects[i].localScale = targetScales[i];
            moduleLocalImages[i].color = targetColors[i];
        }

        // 맨 뒤 요소를 앞으로 보내기
        GameObject maxModule = modulePool[^1];
        modulePool.RemoveAt(modulePool.Count - 1);
        modulePool.Insert(0, maxModule);
        modulePool[0].GetComponent<RectTransform>().anchoredPosition = modulePositions[0];
        modulePool[0].GetComponent<RectTransform>().localScale = Vector3.one;
        modulePool[0].GetComponent<Image>().color = Utils.CodeToColor(Utils.GRAY);

        Image maxImage = moduleImages[^1];
        moduleImages.RemoveAt(moduleImages.Count - 1);
        moduleImages.Insert(0, maxImage);

        modulePool[0].SetActive(true);
        modulePool[^1].SetActive(true);

        yield return new WaitForSeconds(0.1f);
        scrollCoroutine = null;
    }

    private IEnumerator RightScrollModule() // 더 높은 모듈로
    {
        if (moduleFocus == -1) yield break;
        if (moduleFocus == Utils.MODULE_MAX) yield break;

        moduleFocus++;
        moduleImages[moduleCenter + 2].sprite = TryToGetSprite(moduleFocus + 1);
        if (moduleFocus == Utils.MODULE_MAX)
            modulePool[moduleCenter + 2].SetActive(false);

        ModuleData module = GameManager.Instance.ModuleLibrary[moduleFocus];
        int achievement = module.Stages.Count != 0 ? (int)(100 * (float)module.StageIndex / module.Stages.Count) : 0;
        string text = $"{module.Desc} ({achievement}%)";
        SetStageText(text);

        List<Vector2> targetPositions = new List<Vector2>();
        List<Vector3> targetScales = new List<Vector3>();
        List<Color> targetColors = new List<Color>();

        for (int i = 1; i < modulePool.Count; i++)
        {
            targetPositions.Add(modulePositions[i - 1]);
            if (i == moduleCenter + 1)
            {
                targetScales.Add(Vector3.one * Utils.MODULE_HIGHLIGHT_SCALE);
                targetColors.Add(Color.white);
            }
            else
            {
                targetScales.Add(Vector3.one);
                targetColors.Add(Utils.CodeToColor(Utils.GRAY));
            }
        }

        List<Vector2> startPositions = new List<Vector2>();
        List<Vector3> startScales = new List<Vector3>();
        List<Color> startColors = new List<Color>();
        List<RectTransform> moduleRects = new();
        List<Image> moduleLocalImages = new();
        for (int i = 1; i < modulePool.Count; i++)
        {
            RectTransform rt = modulePool[i].GetComponent<RectTransform>();
            moduleRects.Add(rt);
            startPositions.Add(rt.anchoredPosition);
            startScales.Add(rt.localScale);

            Image img = modulePool[i].GetComponent<Image>();
            moduleLocalImages.Add(img);
            startColors.Add(img.color);
        }

        // 코루틴으로 보간
        float elapsed = 0f;
        while (elapsed < scrollTime)
        {
            float t = elapsed / scrollTime;

            for (int i = 1; i < modulePool.Count; i++)
            {
                moduleRects[i - 1].anchoredPosition = Vector2.Lerp(startPositions[i - 1], targetPositions[i - 1], t);
                moduleRects[i - 1].localScale = Vector3.Lerp(startScales[i - 1], targetScales[i - 1], t);
                moduleLocalImages[i - 1].color = Color.Lerp(startColors[i - 1], targetColors[i - 1], t);
            }

            elapsed += Time.smoothDeltaTime;
            yield return null;
        }

        // 마지막 보정
        for (int i = 1; i < modulePool.Count; i++)
        {
            moduleRects[i - 1].anchoredPosition = targetPositions[i - 1];
            moduleRects[i - 1].localScale = targetScales[i - 1];
            moduleLocalImages[i - 1].color = targetColors[i - 1];
        }

        // 맨 앞 요소를 뒤로 보내기
        GameObject minModule = modulePool[0];
        modulePool.RemoveAt(0);
        modulePool.Add(minModule);
        modulePool[^1].GetComponent<RectTransform>().anchoredPosition = modulePositions[^1];
        modulePool[^1].GetComponent<RectTransform>().localScale = Vector3.one;
        modulePool[^1].GetComponent<Image>().color = Utils.CodeToColor(Utils.GRAY);

        Image minImage = moduleImages[0];
        moduleImages.RemoveAt(0);
        moduleImages.Add(minImage);

        modulePool[0].SetActive(true);
        modulePool[^1].SetActive(true);

        yield return new WaitForSeconds(0.1f);
        scrollCoroutine = null;
    }

    #endregion

    #region Tutorial Popup
    [Header("Tutorial Popup")]
    [SerializeField] private UIPopup popup;
    public void OpenTutorialPopup(TutorialData tutorial) => popup.OpenPopup(tutorial);
    #endregion
}