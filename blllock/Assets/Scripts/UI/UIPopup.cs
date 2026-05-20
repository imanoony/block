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

        interactable = true; // 임시

        // TODO: 트랜지션 시작시키기
    }
    public void ClosePopup()
    {
        tutorialCur = null;
        contentCnt = -1;
        contentCur = -1;

        popup.SetActive(false); // (임시)

        // TODO: 트랜지션 시작하고 끝났을 때 액션에 popup 닫기 예약해두기
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
        else nextButtonText.text = closeText;
    }

    #region Transitions
    #endregion
}