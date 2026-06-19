using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class UIPopup: MonoBehaviour
{
    [Header("Button Texts")]
    [SerializeField] private string prevText;
    [SerializeField] private string nextText;
    [SerializeField] private string closeText;
    
    [Header("GameObjects")]
    [SerializeField] private GameObject popup;
    [SerializeField] private GameObject scrim;
    [SerializeField] private GameObject window;
    [SerializeField] private GameObject buttons;

    private TutorialData tutorialCur = null;
    private int contentCnt = -1;
    private int contentCur = -1;

    // 일단 트랜지션 도중에는 인터랙션을 막도록 함.
    // 근데 플레이 해보고 약간 답답함이 있으면, 트랜지션 도중에도 
    // 이전, 다음 버튼 누르는 인터랙션 가능하도록 수정할 예정임. 
    private bool interactable = false;

    public void OpenPopup(TutorialData tutorial)
    {
        tutorialCur = tutorial;
        contentCnt = tutorial.Contents.Count;
        contentCur = 0;

        SetPopup();
        SetButtons();
        
        popup.SetActive(true);
        StartCoroutine(PopupOnCo());
    }
    public void ClosePopup()
    {
        tutorialCur = null;
        contentCnt = -1;
        contentCur = -1;

        StartCoroutine(PopupOffCo());
    }
    public void Next()
    {
        if (!interactable) return;
        if (contentCur > contentCnt - 1) return;
        if (contentCur == contentCnt - 1)
        {
            ClosePopup();
            return;
        }
        contentCur++;
        
        SetPopup();
        if (contentCur == contentCnt - 1) SetButtons();
        else if (contentCur == 1) SetButtons();
    }
    public void Previous()
    {
        if (!interactable) return;
        if (contentCur <= 0) return;
        contentCur--;

        SetPopup();
        if (contentCur == 0) SetButtons();
        else if (contentCur == contentCnt - 2) SetButtons();
    }

    private GameObject windowContent = null;
    private TextMeshProUGUI windowText = null;
    private void SetPopup()
    {
        TutorialData.Content content = tutorialCur.Contents[contentCur];

        if (windowContent == null) windowContent = window.transform.GetChild(2).gameObject;
        if (windowText == null) windowText = windowContent.GetComponentInChildren<TextMeshProUGUI>();

        windowText.text = content.Text;

        // TODO: GIF 설정
        // 일단 GIF는 나중에... (작성 중)
    }

    private GameObject prevButton = null;
    private GameObject nextButton = null;
    private TextMeshProUGUI nextButtonText = null;
    private void SetButtons()
    {
        bool hasPrev = contentCur > 0;
        bool hasNext = contentCur < contentCnt - 1;

        if (prevButton == null) prevButton = buttons.transform.GetChild(0).gameObject;
        if (nextButton == null) nextButton = buttons.transform.GetChild(1).gameObject;
        if (nextButtonText == null) nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();

        if (hasPrev) prevButton.SetActive(true);
        else prevButton.SetActive(false);

        if (hasNext) nextButtonText.text = nextText;
        else {
            if (tutorialCur.ID == 9999)
            {
                nextButton.SetActive(false);
            }
            nextButtonText.text = closeText;
        }
    }

    #region Transitions
    // 연출 관련 세팅은 하드하게 한다.
    private IEnumerator PopupOnCo()
    {
        interactable = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(ScrimOn(0.4f));
        seq.Join(WindowOn(0.4f));
        seq.AppendInterval(0.3f);
        seq.Append(ButtonsOn(0.4f));

        yield return seq.WaitForCompletion();

        interactable = true;
    }
    private IEnumerator PopupOffCo()
    {
        interactable = false;
        Sequence seq = DOTween.Sequence();
        seq.Append(ButtonsOff(0.4f));
        seq.Join(WindowOff(0.4f));
        seq.Append(ScrimOff(0.4f));

        yield return seq.WaitForCompletion();

        popup.SetActive(false);

        GameManager.Instance.SetState(GameState.InGame);
    }
    private Tween ScrimOn(float duration)
    {
        Image scrimImage = scrim.GetComponent<Image>();
        scrimImage.color = new Color(0, 0, 0, 0);
        scrim.SetActive(true);

        return scrimImage.DOFade(0.9f, duration);
    }
    private Tween ScrimOff(float duration)
    {
        Image scrimImage = scrim.GetComponent<Image>();

        return scrimImage.DOFade(0f, duration).OnComplete(() => scrim.SetActive(false));
    }
    private Sequence WindowOn(float duration)
    {
        RectTransform windowRect = window.GetComponent<RectTransform>();
        CanvasGroup windowCG = window.GetComponent<CanvasGroup>();

        windowRect.localScale = Vector3.one * 0.7f;
        windowCG.alpha = 0f;

        window.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.Append(
            windowRect.DOScale(1f, duration)
                .SetEase(Ease.OutBack, overshoot: 1.4f)
        );
        seq.Join(
            windowCG.DOFade(1f, duration * 0.5f)
        );

        return seq;
    }
    private Sequence WindowOff(float duration)
    {
        RectTransform windowRect = window.GetComponent<RectTransform>();
        CanvasGroup windowCG = window.GetComponent<CanvasGroup>();

        Sequence seq = DOTween.Sequence();
        seq.Append(
            windowRect.DOScale(0.5f, duration)
                .SetEase(Ease.OutQuad)
        );
        seq.Join(
            windowCG.DOFade(0f, duration)
        );
        seq.OnComplete(() =>
        {
            window.SetActive(false);
        });

        return seq;
    }
    private Sequence ButtonsOn(float duration)
    {
        CanvasGroup buttonsCG = buttons.GetComponent<CanvasGroup>();
        RectTransform buttonsRect = buttons.GetComponent<RectTransform>();

        buttonsCG.alpha = 0f;
        buttonsRect.anchoredPosition += Vector2.down * 24f;
        buttons.SetActive(true);

        Sequence seq = DOTween.Sequence();
        seq.Append(
            buttonsRect.DOAnchorPosY(
                buttonsRect.anchoredPosition.y + 24f,
                duration
            )
            .SetEase(Ease.OutCubic)
        );
        seq.Join(
            buttonsCG.DOFade(1f, duration * 0.8f)
        );

        return seq;
    }

    private Sequence ButtonsOff(float duration)
    {
        CanvasGroup buttonsCG = buttons.GetComponent<CanvasGroup>();
        RectTransform buttonsRect = buttons.GetComponent<RectTransform>();

        Vector2 originPos = buttonsRect.anchoredPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(
            buttonsRect.DOAnchorPosY(
                originPos.y - 16f,
                duration
            )
            .SetEase(Ease.InCubic)
        );
        seq.Join(
            buttonsCG.DOFade(0f, duration * 0.7f)
        );
        seq.OnComplete(() =>
        {
            buttons.SetActive(false);
            buttonsRect.anchoredPosition = originPos;
        });

        return seq;
    }
    #endregion
}