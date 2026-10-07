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

    private CanvasGroup progressCanvasGroup;
    private List<GameObject> progresses = new();
    
    private int moduleID = -1;
    private string nameString = "";
    private int progressPercent = 0;
    private int progressCount = 0;
    private int progressIndex = 0;

    //private Tween nameTextTween = null;
    //private Tween progressTextTween = null;
    //private Tween progressTween = null;

    public void Init()
    {
        progressCanvasGroup = progressParent.GetComponent<CanvasGroup>();

        // TODO    
    }

    public void InitModule(ModuleData module)
    {
        moduleID = module.ID;
        nameString = module.Desc;
        progressCount = module.Stages.Count;
        progressIndex = module.StageIndex;
        progressPercent = progressCount == 0 ? 0 : Mathf.RoundToInt((float)progressIndex / (float)progressCount * 100f);

        for (int i = 0; i < progresses.Count; i++)
        {
            Destroy(progresses[i]);
        }
        progresses.Clear();

        GameObject progressGo;
        for (int i = 0; i < progressCount; i++)
        {
            progressGo = Instantiate( // TODO: pooling?
                progressPrefab,
                progressParent.transform
            );
            progresses.Add(progressGo);

            UIProgress progress = progressGo.GetComponent<UIProgress>();

            if (i < progressIndex)
            {
                progress.SetTypeImmediate(ProgressType.Cleared);
            }
        }

        nameText.text = module.Desc;
        progressText.SetText("{0}%", progressPercent);

        //nameTextTween?.Kill();
        //progressTextTween?.Kill();
        //progressTween?.Kill();

        //nameTextTween = null;
        //progressTextTween = null;
        //progressTween = null;
    }

    public void SetAlpha(float alpha)
    {
        nameText.alpha = alpha;
        progressText.alpha = alpha;
        progressCanvasGroup.alpha = alpha;
    }

    // return moduleID is same as id
    public bool CheckModuleID(int id) => moduleID == id;

    /*public void PlayName(float duration)
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
        for (int i = 0; i < progressCount; i++)
        {
            progressGo = Instantiate(
                progressPrefab,
                progressParent.transform
            );
            progresses.Add(progressGo);

            UIProgress progress = progressGo.GetComponent<UIProgress>();

            if (i < progressIndex)
            {
                seq.InsertCallback(
                    interval * (float)i,
                    () =>
                    {
                        progress.SetType(ProgressType.Active);
                        progress.SetType(ProgressType.Cleared);
                    }
                );
            }
        }
        seq.OnKill(() => progressTween = null);

        progressTween = seq;
    }*/
}
