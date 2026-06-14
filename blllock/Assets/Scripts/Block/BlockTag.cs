using UnityEngine;
using DG.Tweening;

public enum TagType
{
    Null = -1,
    RotateCW,
    RotateCCW,
    FlipX,
    FlipY
}

public class BlockTag: MonoBehaviour
{
    private static readonly int TagAttachHash = Animator.StringToHash("TagAttach");

    public TagType Type { get; private set; } = TagType.Null;
    
    private GameObject shadow;
    private GameObject effect;
    private SpriteRenderer sr;
    private SpriteRenderer effectSr;
    private Animator anim;
    [SerializeField] private AnimationClip tagAttachClip;
    
    public void Initialize(
        TagType type,
        Sprite sprite,
        Vector2 pos
    )
    {
        shadow = transform.GetChild(0).gameObject;
        effect = transform.GetChild(2).gameObject;
        sr = transform.GetChild(1).GetComponent<SpriteRenderer>();
        effectSr = effect.GetComponent<SpriteRenderer>();
        anim = gameObject.GetComponent<Animator>();

        Type = type;
        sr.sprite = sprite;
        transform.localPosition = pos;

        // TODO: type에 따라 effect 색상 변경
    }

    public Tween GetTagOnTween()
    {
        Sequence seq = DOTween.Sequence().Pause();
        seq.AppendCallback(
            () =>
            {
                shadow.SetActive(true);
                sr.gameObject.SetActive(true);
                anim.Play(TagAttachHash);
            }
        );

        float duration = tagAttachClip.length / 10f; // 임시 
        seq.AppendInterval(duration);

        return seq;
    }
}
