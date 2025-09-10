using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 게임 시작 시 최초 1회만 실행
    private bool initialized = false;
    public void Initialize()
    {
        if (initialized) return;

        for (int i = 0; i < chatCount; i++)
        {
            GameObject chat = Instantiate(chatPrefab, chatParent.transform);
            chat.SetActive(false);

            chatPool.Add(chat);
            chatDisable.Add(i);
        }

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
    public Dictionary<Vector2Int, int> ChatEnablePos { get; private set; } = new();

    public void EnableGridHover(int x, int y)
    {
        GridType type;
        if ((type = GameManager.Instance.Grid.Grids[x, y].Type) != GridType.Null)
        {
            Debug.Log(GameManager.Instance.Grid.GetGridExpr(x, y));

            string expr = GameManager.Instance.Grid.GetGridExpr(x, y).ToString();
            int chatIndex = EnableChat();
            ChatEnablePos.Add(new(x, y), chatIndex);
            GameObject chat = chatPool[chatIndex];

            chat.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = expr;
            chat.GetComponent<RectTransform>().anchoredPosition = GetChatPosUI(x, y, canvas);

            if (type == GridType.Input) SetChatColor(chat, Color.white);
            else SetChatColor(chat, Utils.CodeToColor(Utils.CHAT_BLUE));
        }
        else
        {
            Debug.Log(GameManager.Instance.Grid.GetGridCacheExpr(x, y));

            LogicExpr logic = GameManager.Instance.Grid.GetGridCacheExpr(x, y);
            if (logic == null) return;

            string expr = logic.ToString();
            int chatIndex = EnableChat();
            ChatEnablePos.Add(new(x, y), chatIndex);
            GameObject chat = chatPool[chatIndex];

            chat.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = expr;
            chat.GetComponent<RectTransform>().anchoredPosition = GetChatPosUI(x, y, canvas);

            SetChatColor(chat, Color.white);
        }
    }
    public void DisableGridHover(int x, int y) => DisableChat(x, y);

    private int EnableChat()
    {
        if (chatDisable.Count == 0) DisableChat(chatEnable[0]);

        int index = chatDisable[0];
        chatDisable.RemoveAt(0);
        chatEnable.Add(index);

        chatPool[index].SetActive(true);
        return index;
    }

    private void DisableChat(int x, int y)
    {
        Vector2Int pos = new(x, y);
        if (ChatEnablePos.ContainsKey(pos))
        {
            DisableChat(ChatEnablePos[pos]);
            ChatEnablePos.Remove(pos);
        }
    }
    private void DisableChat(int index)
    {
        chatPool[index].SetActive(false);

        chatEnable.Remove(index);
        chatDisable.Add(index);

        if (ChatEnablePos.Count == 0) return;
        foreach (var kvp in ChatEnablePos)
        {
            if (kvp.Value == index) { ChatEnablePos.Remove(kvp.Key); return; }
        }
    }
    private void DisableAllChat()
    {
        foreach (var index in chatEnable.ToArray())
            DisableChat(index);
    }

    private void SetChatColor(GameObject chat, Color color)
    {
        Image body = chat.transform.GetChild(0).GetComponent<Image>();
        Image tail = chat.transform.GetChild(1).GetComponent<Image>();

        body.color = color;
        tail.color = color;
        body.color = new Color(color.r, color.g, color.b, Utils.CLEAR_ALPHA);
        tail.color = new Color(color.r, color.g, color.b, Utils.CLEAR_ALPHA);
    }

    private Vector2 chatOffset = new(9, 40);
    private Vector2 GetChatPosUI(int x, int y, Canvas canvas)
    {
        // 1. 타일 월드 좌표
        Vector3 worldPos = (Vector3)GameManager.Instance.Grid.GetTileTopLeftWorld(x, y);

        // 2. 월드 -> 스크린 좌표
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 3. Canvas 기준으로 보정
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 uiPos;
        uiPos.x = screenPos.x - canvasRect.sizeDelta.x / 2f;
        uiPos.y = screenPos.y - canvasRect.sizeDelta.y / 2f;

        return uiPos + chatOffset; // RectTransform.anchoredPosition에 바로 적용 가능
    }
    #endregion

    #region Tile Shadow
    // Tile Shadow는 UI가 아니라 GameObject지만 UIManager에서 관리
    #endregion

    #region Menu
    [Header("Menu")]
    [SerializeField] private GameObject menu;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextButton;

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

    public void NextAppear()
    {
        nextButton.interactable = true;
        nextButton.gameObject.SetActive(true);
    }

    public void NextDisappear()
    {
        nextButton.interactable = false;
        nextButton.gameObject.SetActive(false);
    }

    public void ResetAppear()
    {
        resetButton.interactable = true;
        resetButton.gameObject.SetActive(true);
    }

    public void ResetDisappear()
    {
        resetButton.interactable = false;
        resetButton.gameObject.SetActive(false);
    }

    public void MenuButton() => MenuAppear();
    public void ResetButton() => GameManager.Instance.ResetGame();
    public void CloseButton() => MenuDisappear();
    public void NextButton()
    {
        DisableAllChat();
        GameManager.Instance.NextStage();

        MenuAppear();
        stageText.text = GameManager.Instance.CurrentStage.Desc;
        NextDisappear();
        ResetAppear();
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
        if (canRotate) blockTooltipText.text = "Rotate";
        else if (canFlip) blockTooltipText.text = "Flip";

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 uiPos;
        uiPos.x = screenPos.x - canvasRect.sizeDelta.x / 2f;
        uiPos.y = screenPos.y - canvasRect.sizeDelta.y / 2f;

        blockTooltip.GetComponent<RectTransform>().anchoredPosition = uiPos;
        blockTooltip.SetActive(true);
    }
    public void BlockTooltipDisappear() => blockTooltip.SetActive(false);
    #endregion
}