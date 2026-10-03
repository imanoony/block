using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UITransition : MonoBehaviour
{
    [SerializeField] private Sprite right2leftSprite;
    [SerializeField] private Sprite left2rightSprite;

    private const float START_LEFT = 1920.0f;
    private const float START_RIGHT = -540.0f;
    private const float END_LEFT = -540.0f;
    private const float END_RIGHT = 1920.0f;

    private float top = 0.0f;
    private float bottom = 0.0f;
    private Color color = Color.white;
    private float duration = 0.2f;
    private Ease ease = Ease.InOutSine;
    private bool right2left = true;

    private RectTransform rect;
    private Image image;

    public void Init(
        float top,
        float bottom,
        Color color,
        float duration,
        Ease ease,
        bool right2left = true
    )
    {
        this.top = top;
        this.bottom = bottom;
        this.color = color;
        this.duration = duration;
        this.ease = ease;
        this.right2left = right2left;

        rect = gameObject.GetComponent<RectTransform>();
        image = gameObject.GetComponent<Image>();
        image.color = color;
        image.sprite = right2left ? right2leftSprite : left2rightSprite;

        Reset();
    }

    public Tween PlayHead()
    {
        Tween tween = DOTween.To(
            () => 0f, 
            t =>
            {
                if (rect == null) return;

                if (right2left)
                {
                    float left = Mathf.LerpUnclamped(START_LEFT, END_LEFT, t);
                    rect.offsetMin = new Vector2(left, bottom);
                }
                else
                {
                    float right = Mathf.LerpUnclamped(END_RIGHT, START_RIGHT, t);
                    rect.offsetMax = new Vector2(-right, -top);
                }
            }, 
            1f, 
            duration
        ).SetEase(ease);

        return tween;
    }

    public Tween PlayTail()
    {
        Tween tween = DOTween.To(
            () => 0f, 
            t =>
            {
                if (rect == null) return;

                if (right2left)
                {
                    float right = Mathf.LerpUnclamped(START_RIGHT, END_RIGHT, t);
                    rect.offsetMax = new Vector2(-right, -top);
                }
                else
                {
                    float left = Mathf.LerpUnclamped(END_LEFT, START_LEFT, t);
                    rect.offsetMin = new Vector2(left, bottom);
                }
            }, 
            1f, 
            duration
        ).SetEase(ease);

        return tween;
    }

    private void Reset()
    {
        if (rect == null) return;

        float left = right2left ? START_LEFT : END_LEFT;
        float right = right2left ? START_RIGHT : END_RIGHT;

        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
