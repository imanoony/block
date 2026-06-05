using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class UIChat : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [Header("Chat")]
    [SerializeField] private GameObject chatPrefab;
    [SerializeField] private GameObject chatParent;

    private Dictionary<Vector2Int, GameObject> chatDict = new();
    private Dictionary<Vector2Int, ChatState> stateDict = new();
    private Dictionary<Vector2Int, Tween> tweenDict = new();

    // 반투명 처리용
    private Dictionary<Vector2Int, Tween> dimTweenDict = new();
    private Dictionary<Vector2Int, bool> dimStateDict = new();

    private enum ChatState
    {
        Hidden,
        Showing,
        Shown,
        Hiding
    }

    private float chatTime = 0.3f;
    private float transitionY = 5f;

    // dim 설정
    private float dimAlpha = 0.3f;
    private float dimTime = 0.15f;

    public void SetChat(int circuitWidth, int circuitHeight)
    {
        ClearChat();

        for (int x = 0; x < circuitHeight + 1; x++)
        {
            for (int y = 0; y < circuitWidth + 1; y++)
            {
                Vector2Int pos = new(x, y);

                GameObject chat =
                    Instantiate(chatPrefab, chatParent.transform);

                Vector2 chatPos =
                    GetChatPosUI(pos.x, pos.y, canvas);

                RectTransform rect =
                    chat.GetComponent<RectTransform>();

                CanvasGroup cg =
                    chat.GetComponent<CanvasGroup>();

                rect.anchoredPosition = chatPos;
                cg.alpha = 0f;

                chat.SetActive(false);

                chatDict[pos] = chat;
                stateDict[pos] = ChatState.Hidden;
                tweenDict[pos] = null;

                dimTweenDict[pos] = null;
                dimStateDict[pos] = false;
            }
        }
    }

    public void ClearChat()
    {
        foreach (var tween in tweenDict.Values)
        {
            tween?.Kill();
        }

        foreach (var tween in dimTweenDict.Values)
        {
            tween?.Kill();
        }

        foreach (var chat in chatDict.Values)
        {
            Destroy(chat);
        }

        chatDict.Clear();
        stateDict.Clear();
        tweenDict.Clear();

        dimTweenDict.Clear();
        dimStateDict.Clear();
    }

    public bool IsChatEnabled(Vector2Int pos)
    {
        if (!stateDict.ContainsKey(pos))
            return false;

        return stateDict[pos] == ChatState.Shown ||
               stateDict[pos] == ChatState.Showing;
    }

    public void EnableChat(int x, int y)
    {
        Vector2Int pos = new(x, y);

        if (!chatDict.ContainsKey(pos))
            return;

        GridType type;

        string expr;
        Color color;

        if ((type = GameManager.Instance.Grid.Grids[x, y].Type)
            != GridType.Null)
        {
            expr =
                GameManager.Instance.Grid
                .GetGridExpr(x, y)
                .ToString();

            color = type == GridType.Input
                ? Utils.CodeToColor(Utils.CHAT_BLUE)
                : Color.white;
        }
        else
        {
            LogicExpr logic =
                GameManager.Instance.Grid
                .GetGridCacheExpr(x, y);

            if (logic == null)
                return;

            expr = logic.ToString();
            color = Utils.CodeToColor(Utils.CHAT_BLUE);
        }

        ShowChat(pos, expr, color);
    }

    public void DisableChat(int x, int y)
    {
        Vector2Int pos = new(x, y);

        if (!chatDict.ContainsKey(pos))
            return;

        HideChat(pos);
    }

    public void DisableAllChat()
    {
        foreach (Vector2Int pos in chatDict.Keys)
        {
            HideChat(pos);
        }
    }

    // =========================
    // DIM API
    // =========================

    public void DimChat(int x, int y)
    {
        Vector2Int pos = new(x, y);

        if (!chatDict.ContainsKey(pos))
            return;

        SetChatDim(pos, true);
    }

    public void UndimChat(int x, int y)
    {
        Vector2Int pos = new(x, y);

        if (!chatDict.ContainsKey(pos))
            return;

        SetChatDim(pos, false);
    }

    public void UndimAllChat()
    {
        foreach (Vector2Int pos in chatDict.Keys)
        {
            SetChatDim(pos, false);
        }
    }

    private void SetChatDim(Vector2Int pos, bool dim)
    {
        if (dimStateDict[pos] == dim)
            return;

        GameObject chat = chatDict[pos];

        if (!chat.activeSelf)
        {
            dimStateDict[pos] = dim;
            return;
        }

        CanvasGroup cg = chat.GetComponent<CanvasGroup>();

        dimTweenDict[pos]?.Kill();

        float targetAlpha = dim
            ? dimAlpha
            : 1f;

        dimStateDict[pos] = dim;

        Tween tween = cg
            .DOFade(targetAlpha, dimTime)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                dimTweenDict[pos] = null;
            });

        dimTweenDict[pos] = tween;
    }

    // =========================
    // SHOW / HIDE
    // =========================

    private void ShowChat(
        Vector2Int pos,
        string expr,
        Color color
    )
    {
        ChatState state = stateDict[pos];

        // 이미 켜져있거나 켜지는 중이면 무시
        if (state == ChatState.Shown ||
            state == ChatState.Showing)
            return;

        GameObject chat = chatDict[pos];

        tweenDict[pos]?.Kill();

        CanvasGroup cg =
            chat.GetComponent<CanvasGroup>();

        RectTransform rect =
            chat.GetComponent<RectTransform>();

        TextMeshProUGUI text =
            chat.transform.GetChild(1)
            .GetComponentInChildren<TextMeshProUGUI>();

        text.text = expr;

        SetChatColor(chat, color);

        chat.SetActive(true);

        stateDict[pos] = ChatState.Showing;

        Vector2 basePos =
            GetChatPosUI(pos.x, pos.y, canvas);

        rect.anchoredPosition =
            new Vector2(
                basePos.x,
                basePos.y - transitionY
            );

        cg.alpha = 0f;

        float targetAlpha =
            dimStateDict[pos]
            ? dimAlpha
            : 1f;

        Sequence seq = DOTween.Sequence();

        seq.Join(
            rect.DOAnchorPosY(basePos.y, chatTime)
                .SetEase(Ease.OutCubic)
        );

        seq.Join(
            cg.DOFade(targetAlpha, chatTime)
              .SetEase(Ease.OutCubic)
        );

        seq.OnComplete(() =>
        {
            cg.alpha = targetAlpha;

            stateDict[pos] = ChatState.Shown;
            tweenDict[pos] = null;
        });

        tweenDict[pos] = seq;
    }

    private void HideChat(Vector2Int pos)
    {
        ChatState state = stateDict[pos];

        // 이미 꺼져있거나 꺼지는 중이면 무시
        if (state == ChatState.Hidden ||
            state == ChatState.Hiding)
            return;

        GameObject chat = chatDict[pos];

        tweenDict[pos]?.Kill();

        CanvasGroup cg =
            chat.GetComponent<CanvasGroup>();

        RectTransform rect =
            chat.GetComponent<RectTransform>();

        stateDict[pos] = ChatState.Hiding;

        float targetY =
            rect.anchoredPosition.y - transitionY;

        Sequence seq = DOTween.Sequence();

        seq.Join(
            rect.DOAnchorPosY(targetY, chatTime)
                .SetEase(Ease.InCubic)
        );

        seq.Join(
            cg.DOFade(0f, chatTime)
              .SetEase(Ease.InCubic)
        );

        seq.OnComplete(() =>
        {
            chat.SetActive(false);

            stateDict[pos] = ChatState.Hidden;
            tweenDict[pos] = null;
        });

        tweenDict[pos] = seq;
    }

    // =========================
    // VISUAL
    // =========================

    private void SetChatColor(
        GameObject chat,
        Color color
    )
    {
        Image tail = chat.transform.GetChild(0)
            .GetChild(1)
            .GetComponent<Image>();

        Image body = chat.transform.GetChild(1)
            .GetChild(1)
            .GetComponent<Image>();

        body.color = color;
        tail.color = color;
    }

    public Vector2 GetChatPosUI(
        int x,
        int y,
        Canvas canvas
    )
    {
        Vector2Int cStart =
            GameManager.Instance.Grid.GetCircuitStart();

        Vector3 worldPos =
            GameManager.Instance.Grid
            .GetTileTopLeftForChat(
                x + cStart.x,
                y + cStart.y
            );

        Vector3 screenPos =
            Camera.main.WorldToScreenPoint(worldPos);

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        Vector2 localPoint;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode
                    == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera,
                out localPoint
            );

        return localPoint;
    }
}