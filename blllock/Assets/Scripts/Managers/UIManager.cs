using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject chatPrefab;
    [SerializeField] private GameObject chatParent;
    private List<GameObject> chatPool = new();
    private int chatCount = 5;
    private List<int> chatEnable = new(), chatDisable = new();
    public Dictionary<Vector2Int, int> ChatEnablePos { get; private set; } = new();

    public void EnableGridHover(int x, int y)
    {
        if (GameManager.Instance.Grid.Grids[x, y].Type != GridType.Null)
        {
            Debug.Log(GameManager.Instance.Grid.GetGridExpr(x, y));

            string expr = GameManager.Instance.Grid.GetGridExpr(x, y).ToString();
            int chatIndex = EnableChat();
            ChatEnablePos.Add(new(x, y), chatIndex);
            GameObject chat = chatPool[chatIndex];

            chat.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = expr;
            chat.GetComponent<RectTransform>().anchoredPosition = GetChatPosUI(x, y, canvas);
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

}