using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;

        for (int i = 0; i < chatCount + 1; i++) // 여유분 1개의 chat이 항상 존재
        {
            GameObject chat = Instantiate(chatPrefab, chatParent.transform);
            chat.SetActive(false);

            chatPool.Add(chat);
            chatDisable.Add(i);
            chatCoroutines.Add(null);
        }

        InitModule();

        initialized = true;
    }

    #region Chat 
    [SerializeField] private Canvas canvas;
    [Header("Chat")]
    [SerializeField] private GameObject chatPrefab;
    [SerializeField] private GameObject chatParent;
    private List<GameObject> chatPool = new();
    private int chatCount = 5;
    private List<int> chatEnable = new(), chatDisable = new();
    private List<Coroutine> chatCoroutines = new();
    public Dictionary<Vector2Int, int> ChatEnablePos { get; private set; } = new();

    public void EnableGridHover(int x, int y)
    {
        GridType type;
        if ((type = GameManager.Instance.Grid.Grids[x, y].Type) != GridType.Null)
        {
            GameManager.Instance.ResetGridIdleTile();

            string expr = GameManager.Instance.Grid.GetGridExpr(x, y).ToString();
            Color color = type == GridType.Input ? Utils.CodeToColor(Utils.CHAT_BLUE) : Color.white;

            EnableChat(new(x, y), expr, color);
        }
        else
        {
            LogicExpr logic = GameManager.Instance.Grid.GetGridCacheExpr(x, y);
            if (logic == null) return;

            GameManager.Instance.ResetGridIdleTile();
            
            string expr = logic.ToString();
            Color color = Utils.CodeToColor(Utils.CHAT_BLUE);

            EnableChat(new(x, y), expr, color);
        }
    }
    public void EnableGridHover(Vector2Int pos) => EnableGridHover(pos.x, pos.y);
    public void DisableGridHover(int x, int y) { GameManager.Instance.ResetGridIdleTile(); DisableChat(new Vector2Int(x, y)); }
    public void DisableGridHover(Vector2Int pos) { GameManager.Instance.ResetGridIdleTile(); DisableChat(pos); }

    private void EnableChat(Vector2Int pos, string expr, Color color)
    {
        int index;

        // Disable Coroutine 도중에 Enable 요청 들어옴
        // 또는 Enable Coroutine 도중에 Enable 요청 들어옴
        // 또는 이미 Enabled 상태
        if (ChatEnablePos.ContainsKey(pos))
        {
            index = ChatEnablePos[pos];
            if (chatDisable.Contains(index)) chatDisable.Remove(index);
            if (!chatEnable.Contains(index)) chatEnable.Add(index);

            if (chatCoroutines[index] != null)
            {
                StopCoroutine(chatCoroutines[index]);
                chatCoroutines[index] = StartCoroutine(ChatCoroutine(index, true));
                return;
            }
            else return;
        }

        if (chatDisable.Count == 1)
        {
            Debug.Log("chatDisable이 하나밖에 없어서 Enable한 거 하나 없앰");
            DisableChat(chatEnable[0]);
        }

        // 미사용 chat을 chatPool에서 빼내 쓰는 상황
        // Expr, Position, Color 미리 설정하고 트랜지션만 코루틴 처리
        index = chatDisable[0];
        Debug.Log($"인덱스는 {index}");

        chatDisable.RemoveAt(0);
        if (!chatEnable.Contains(index)) chatEnable.Add(index);
        ChatEnablePos.Add(pos, index);

        GameObject chat = chatPool[index];
        chat.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = expr;
        Vector2 chatPos = GetChatPosUI(pos.x, pos.y, canvas);
        chat.GetComponent<RectTransform>().anchoredPosition = new(chatPos.x, chatPos.y - transitionY);
        SetChatColor(chat, color);

        chatCoroutines[index] = StartCoroutine(ChatCoroutine(index, true));
    }

    private void DisableChat(Vector2Int pos)
    {
        // Enable Coroutine 도중에 Disable 요청 들어옴
        // 또는 Disable Coroutine 도중에 Disable 요청 들어옴
        // 또는 아직 Enabled 상태
        if (ChatEnablePos.ContainsKey(pos))
        {
            int index = ChatEnablePos[pos];
            if (chatCoroutines[index] != null) StopCoroutine(chatCoroutines[index]);
            chatCoroutines[index] = StartCoroutine(ChatCoroutine(index, false));

            chatEnable.Remove(index);
            if (!chatDisable.Contains(index)) chatDisable.Add(index);
        }
    }
    private void DisableChat(int index)
    {
        if (chatEnable.Contains(index))
        {
            if (chatCoroutines[index] != null) StopCoroutine(chatCoroutines[index]);
            chatCoroutines[index] = StartCoroutine(ChatCoroutine(index, false));

            chatEnable.Remove(index);
            if (!chatDisable.Contains(index)) chatDisable.Add(index);
        }
    }

    private void DisableAllChat()
    {
        foreach (var index in chatEnable.ToArray())
        {
            chatDisable.Add(index);
            chatPool[index].SetActive(false);
        }
        chatEnable.Clear();
        ChatEnablePos.Clear();
    }

    private float chatTime = 0.3f;
    private float transitionY = 5f;
    private IEnumerator ChatCoroutine(int index, bool isAppear)
    {
        GameObject chat = chatPool[index];
        Image chatBody = chat.transform.GetChild(0).gameObject.GetComponent<Image>();
        Image chatTail = chat.transform.GetChild(1).gameObject.GetComponent<Image>();
        TextMeshProUGUI chatExpr = chatBody.gameObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        RectTransform chatRect = chat.GetComponent<RectTransform>();

        chat.SetActive(true);

        float targetA, targetY, targetAExpr;
        if (isAppear) { targetA = Utils.CLEAR_ALPHA; targetY = transitionY; targetAExpr = 1f; } // 반투명
        else { targetA = 0f; targetY = -transitionY; targetAExpr = 0f; } // 투명

        Color start = chatBody.color, startExpr = chatExpr.color;
        Color target = new(start.r, start.g, start.b, targetA);
        Color targetExpr = new(startExpr.r, startExpr.g, startExpr.b, targetAExpr);

        Vector2 startPos = chatRect.anchoredPosition;
        Vector2 targetPos = new(startPos.x, startPos.y + targetY);

        float elapsed = 0f;
        while (elapsed < chatTime)
        {
            float t = elapsed / chatTime;

            chatBody.color = Color.Lerp(start, target, t);
            chatTail.color = chatBody.color;
            chatExpr.color = Color.Lerp(startExpr, targetExpr, t);
            chatRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            elapsed += Time.smoothDeltaTime;
            yield return null;
        }

        chatBody.color = target;
        chatTail.color = chatBody.color;
        chatExpr.color = targetExpr;
        chatRect.anchoredPosition = targetPos;

        if (!isAppear)
        {
            foreach (var kvp in ChatEnablePos)
            {
                if (kvp.Value == index) { ChatEnablePos.Remove(kvp.Key); break; }
            }
            chat.SetActive(false);
        }

        chatCoroutines[index] = null;
        yield break;
    }

    private void SetChatColor(GameObject chat, Color color)
    {
        Image body = chat.transform.GetChild(0).GetComponent<Image>();
        Image tail = chat.transform.GetChild(1).GetComponent<Image>();

        float alpha = body.color.a;
        body.color = color;
        tail.color = color;
        body.color = new Color(color.r, color.g, color.b, alpha);
        tail.color = new Color(color.r, color.g, color.b, alpha);
    }

    private Vector2 chatOffset = new(15, 40);
    private Vector2 GetChatPosUI(int x, int y, Canvas canvas)
    {
        // 1. 타일 월드 좌표
        Vector3 worldPos = (Vector3)GameManager.Instance.Grid.GetTileTopLeftForChat(x, y);

        // 2. 월드 -> 스크린 좌표
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 3. Canvas RectTransform 기준 로컬 좌표 계산
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        return localPoint; // RectTransform.anchoredPosition에 바로 적용 가능
    }
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
        Vector3 pos = GetChatPosUI(gridPos.x, gridPos.y, canvas);
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
}