using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;

        module.InitModule();

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
    public void DeactivateChat() => chat.gameObject.SetActive(false);
    public void ActivateChat() => chat.gameObject.SetActive(true);
    #endregion

    #region Menu
    [Header("Menu")]
    // Menu
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject menu;
    private GameObject prevButton = null;
    private GameObject tutorialButton = null;
    private GameObject backButton = null;
    private GameObject quitButton = null;
    private GameObject closeButton = null;
    private RectTransform menuRT = null;
    public void MenuDisable()
    {
        // caching
        if (prevButton == null) prevButton = menu.transform.Find("PrevButton").gameObject;
        if (tutorialButton == null) tutorialButton = menu.transform.Find("TutorialButton").gameObject;
        if (backButton == null) backButton = menu.transform.Find("BackButton").gameObject;
        if (quitButton == null) quitButton = menu.transform.Find("QuitButton").gameObject;
        if (closeButton == null) closeButton = menu.transform.Find("CloseButton").gameObject;
        if (menuRT == null) menuRT = menu.GetComponent<RectTransform>();

        prevButton.GetComponent<Button>().interactable = false;
        tutorialButton.GetComponent<Button>().interactable = false;
        backButton.GetComponent<Button>().interactable = false;
        quitButton.GetComponent<Button>().interactable = false;
        closeButton.GetComponent<Button>().interactable = false;
    }
    public void MenuEnable()
    {
        // caching
        if (prevButton == null) prevButton = menu.transform.Find("PrevButton").gameObject;
        if (tutorialButton == null) tutorialButton = menu.transform.Find("TutorialButton").gameObject;
        if (backButton == null) backButton = menu.transform.Find("BackButton").gameObject;
        if (quitButton == null) quitButton = menu.transform.Find("QuitButton").gameObject;
        if (closeButton == null) closeButton = menu.transform.Find("CloseButton").gameObject;
        if (menuRT == null) menuRT = menu.GetComponent<RectTransform>();

        prevButton.GetComponent<Button>().interactable = true;
        tutorialButton.GetComponent<Button>().interactable = true;
        backButton.GetComponent<Button>().interactable = true;
        quitButton.GetComponent<Button>().interactable = true;
        closeButton.GetComponent<Button>().interactable = true;
    }
    public void MenuAppear()
    {
        // caching
        if (prevButton == null) prevButton = menu.transform.Find("PrevButton").gameObject;
        if (tutorialButton == null) tutorialButton = menu.transform.Find("TutorialButton").gameObject;
        if (backButton == null) backButton = menu.transform.Find("BackButton").gameObject;
        if (quitButton == null) quitButton = menu.transform.Find("QuitButton").gameObject;
        if (closeButton == null) closeButton = menu.transform.Find("CloseButton").gameObject;
        if (menuRT == null) menuRT = menu.GetComponent<RectTransform>();

        if (menu.activeSelf) return;
        
        if (menuCo != null) StopCoroutine(menuCo);
        menuTween?.Kill();
        menuButton.interactable = false;
        menuCo = StartCoroutine(MenuAppearCo());
    }
    public void MenuDisappear()
    {
        if (menuRT == null) menuRT = menu.GetComponent<RectTransform>();

        if (!menu.activeSelf) return;
        MenuDisable();

        if (menuCo != null) StopCoroutine(menuCo);
        menuTween?.Kill();
        menuCo = StartCoroutine(MenuDisappearCo());
    }
    private Tween menuTween = null;
    private Coroutine menuCo = null;
    private IEnumerator MenuAppearCo()
    {
        menu.SetActive(true);

        prevButton.SetActive(prevAppear);
        tutorialButton.SetActive(tutorialAppear);
        backButton.SetActive(backAppear);
        quitButton.SetActive(quitAppear);

        menuRT.localScale = Vector3.zero;
        Tween t = menuRT.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        yield return t.WaitForCompletion();

        menuTween = null;
        menuCo = null;

        MenuEnable();
    }
    private IEnumerator MenuDisappearCo()
    {
        Tween t = menuRT.DOScale(0f, 0.4f).SetEase(Ease.InBack);

        yield return t.WaitForCompletion();

        menu.SetActive(false);

        menuTween = null;
        menuCo = null;

        menuButton.interactable = true;
    }

    private bool prevAppear = false;
    private bool tutorialAppear = false;
    private bool backAppear = false;
    private bool quitAppear = false;
    public void MenuPrevAppear() => prevAppear = true;
    public void MenuPrevDisappear() => prevAppear = false;
    public void MenuTutorialAppear() => tutorialAppear = true;
    public void MenuTutorialDisappear() => tutorialAppear = false;
    public void MenuBackAppear() => backAppear = true;
    public void MenuBackDisappear() => backAppear = false;
    public void MenuQuitAppear() => quitAppear = true;
    public void MenuQuitDisappear() => quitAppear = false;
    #endregion

    #region Stage Clear

    [Header("Stage Clear")] 
    [SerializeField] private RectTransform clearPanel;
    [SerializeField] private Button nextButton;

    private Coroutine clearPanelCo = null;
    private Tween clearPanelTween = null;

    public void ClearPanelAppear()
    {
        if (clearPanelCo != null)
            StopCoroutine(clearPanelCo);

        clearPanelTween?.Kill();

        clearPanelCo = StartCoroutine(ClearPanelAppearCo());
    }

    public void ClearPanelDisappear()
    {
        if (clearPanelCo != null)
            StopCoroutine(clearPanelCo);

        clearPanelTween?.Kill();

        clearPanelCo = StartCoroutine(ClearPanelDisappearCo());
    }

    private IEnumerator ClearPanelAppearCo()
    {
        clearPanel.gameObject.SetActive(true);

        Vector2 pos = clearPanel.anchoredPosition;
        pos.x = 300f;
        clearPanel.anchoredPosition = pos;

        clearPanel.localScale = Vector3.one * 0.9f;

        Sequence seq = DOTween.Sequence();
        clearPanelTween = seq;

        seq.Append(
            clearPanel.DOAnchorPosX(50f, 0.4f)
                .SetEase(Ease.OutCubic)
        );

        seq.Join(
            clearPanel.DOScale(1f, 0.4f)
                .SetEase(Ease.OutBack)
        );

        seq.Append(
            clearPanel.DOAnchorPosX(42f, 0.06f)
                .SetEase(Ease.OutQuad)
        );

        seq.Append(
            clearPanel.DOAnchorPosX(50f, 0.08f)
                .SetEase(Ease.OutQuad)
        );

        yield return seq.WaitForCompletion();

        clearPanelTween = null;
        clearPanelCo = null;
    }

    private IEnumerator ClearPanelDisappearCo()
    {
        Sequence seq = DOTween.Sequence();
        clearPanelTween = seq;

        seq.Append(
            clearPanel.DOAnchorPosX(300f, 0.25f)
                .SetEase(Ease.InCubic)
        );

        seq.Join(
            clearPanel.DOScale(0.9f, 0.25f)
                .SetEase(Ease.InQuad)
        );

        seq.OnComplete(() =>
        {
            clearPanel.gameObject.SetActive(false);
        });

        yield return seq.WaitForCompletion();

        clearPanelTween = null;
        clearPanelCo = null;
    }

    public void StageNextAppear(int stageID, int lastStageID)
    {
        if (stageNextCo != null)
            StopCoroutine(stageNextCo);

        stageNextTween?.Kill();

        if (stageID == lastStageID)
            nextButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "완성";
        else
            nextButton.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "다음";

        stageNextCo = StartCoroutine(StageNextAppearCo());
    }

    public void StageNextDisappear()
    {
        if (stageNextCo != null)
            StopCoroutine(stageNextCo);

        stageNextTween?.Kill();

        stageNextCo = StartCoroutine(StageNextDisappearCo());
    }

    private Coroutine stageNextCo = null;
    private Tween stageNextTween = null;

    private IEnumerator StageNextAppearCo()
    {
        CanvasGroup nextCG = nextButton.gameObject.GetComponent<CanvasGroup>();
        RectTransform nextRect = nextButton.gameObject.GetComponent<RectTransform>();

        nextCG.alpha = 0f;
        nextRect.anchoredPosition = new(
            nextRect.anchoredPosition.x,
            -346
        );

        nextButton.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();
        stageNextTween = seq;

        seq.Append(
            nextRect.DOAnchorPosY(
                -330,
                0.4f
            )
            .SetEase(Ease.OutCubic)
        );

        seq.Join(
            nextCG.DOFade(1f, 0.4f * 0.8f)
        );

        yield return seq.WaitForCompletion();

        nextRect.anchoredPosition = new(
            nextRect.anchoredPosition.x,
            -330
        );
        nextButton.interactable = true;

        stageNextTween = null;
        stageNextCo = null;
    }

    private IEnumerator StageNextDisappearCo()
    {
        CanvasGroup nextCG = nextButton.gameObject.GetComponent<CanvasGroup>();
        RectTransform nextRect = nextButton.gameObject.GetComponent<RectTransform>();

        Vector2 originPos = nextRect.anchoredPosition;

        nextButton.interactable = false;

        Sequence seq = DOTween.Sequence();
        stageNextTween = seq;

        seq.Append(
            nextRect.DOAnchorPosY(
                -346,
                0.4f
            )
            .SetEase(Ease.InCubic)
        );

        seq.Join(
            nextCG.DOFade(0f, 0.4f * 0.7f)
        );

        seq.OnComplete(() =>
        {   
            nextRect.anchoredPosition = new(
                nextRect.anchoredPosition.x,
                -346
            );
            nextButton.gameObject.SetActive(false);
        });

        yield return seq.WaitForCompletion();

        stageNextTween = null;
        stageNextCo = null;
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
    [SerializeField] private UIModule module;
    public void ModuleAppear() => module.ModuleAppear();
    public void ModuleDisappear(Action onComplete) => module.ModuleDisappear(onComplete);
    #endregion

    #region Tutorial Popup
    [Header("Tutorial Popup")]
    [SerializeField] private UIPopup popup;
    public void OpenTutorialPopup(TutorialData tutorial) => popup.OpenPopup(tutorial);
    #endregion
}