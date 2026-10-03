using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIModuleMeta : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject progressParent;
    [SerializeField] private GameObject progressPrefab;

    private List<GameObject> progresses = new();
    
    private string nameString = "";
    private int progressPercent = 0;
    private int progressCount = 0;
    private int progressIndex = 0;

    private Tween nameTextTween = null;
    private Tween progressTextTween = null;
    private Tween progressTween = null;
    public void Init(ModuleData module)
    {
        nameString = module.Desc;
        progressCount = module.Stages.Count;
        progressIndex = module.StageIndex;
        progressPercent = Mathf.RoundToInt((float)progressIndex / (float)progressCount * 100f);

        nameTextTween?.Kill();
        progressTextTween?.Kill();
        progressTween?.Kill();

        nameTextTween = null;
        progressTextTween = null;
        progressTween = null;
    }

    public void PlayName(float duration)
    {
        nameTextTween?.Kill();

        Sequence seq = DOTween.Sequence();
        seq.Append(nameText.DOFade(0f, duration / 2f));
        seq.AppendCallback(() => nameText.text = nameString);
        seq.Append(nameText.DOFade(1f, duration / 2f));

        seq.OnKill(() => nameTextTween = null);
        nameTextTween = seq;
    }

    public void PlayProgressText(float duration)
    {
        progressTextTween?.Kill();
        
        int current = 0;
        progressText.text = "0%";
        
        Tween t = DOTween.To(
            () => current,
            v =>
            {
                current = v;
                progressText.text = $"{v}%";
            },
            progressPercent,
            duration
        )
        .SetEase(Ease.OutCubic)
        .OnKill(() => progressTextTween = null);

        progressTextTween = t;
    }

    public void PlayProgress(float duration)
    {
        progressTween?.Kill();

        for (int i = 0; i < progresses.Count; i++)
        {
            Destroy(progresses[i]);
        }
        progresses.Clear();

        float interval = duration / (float)progressCount;

        Sequence seq = DOTween.Sequence();
        GameObject progressGo;
        UIProgress progress;
        ProgressType type;
        for (int i = 0; i < progressCount; i++)
        {
            progressGo = Instantiate(
                progressPrefab,
                progressParent.transform
            );
            progresses.Add(progressGo);

            progress = progressGo.GetComponent<UIProgress>();
            type = i < progressIndex ? ProgressType.Cleared : ProgressType.Locked;

            seq.InsertCallback(
                interval * (float)i,
                () =>
                {
                    progress.SetType(type);
                }
            );
        }
        seq.OnKill(() => progressTween = null);

        progressTween = seq;
    }
}
