using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class UIModule : MonoBehaviour
{
    private class ModuleStyle
    {
        public GameObject gameObject;
        public RectTransform rect;
        public Image image;   
        public Button button;
    }

    private class ModuleConfig
    {
        public Vector2 position;
        public Vector2 scale;
        public Color color;
    }

    #region Module UI
    [Header("Module")]
    [SerializeField] private GameObject moduleParent;
    [SerializeField] private Sprite[] moduleSprites;
    [SerializeField] private Sprite moduleDefault;

    private float appearTime = 1f;
    private float scrollTime = 0.5f;

    private Ease appearEase = Ease.OutCubic;
    private Ease disappearEase = Ease.InCubic;
    private Ease scrollEase = Ease.OutCubic;
    private const int moduleCenter = 3;
    private int moduleCount;

    private List<ModuleStyle> moduleStyles = new();
    private List<ModuleConfig> moduleConfigs = new();
    private RectTransform moduleParentRt;

    private int moduleFocus = 0;

    private Tween currentTween = null;
    private Tween scrollTween = null;

    private Sprite TryToGetSprite(int moduleID)
    {
        if (moduleSprites.Length > moduleID && moduleID >= 0)
        {
            return moduleSprites[moduleID];
        }
        else return moduleDefault;
    }
    
    public void InitModule()
    {
        moduleCount = moduleParent.transform.childCount;

        for (int i = 0; i < moduleCount; i++)
        {
            GameObject module = moduleParent.transform.GetChild(i).gameObject;
            ModuleStyle ms = new()
            {
                gameObject = module,
                rect = module.GetComponent<RectTransform>(),
                image = module.GetComponent<Image>(),
                button = module.GetComponent<Button>()
            };
            ModuleConfig mc = new()
            {
                position = ms.rect.anchoredPosition,
                scale = ms.rect.localScale,
                color = ms.image.color
            };
            moduleStyles.Add(ms);
            moduleConfigs.Add(mc);

            ms.button.onClick.AddListener(() => ScrollModule(ms));
        }

        moduleParentRt = moduleParent.GetComponent<RectTransform>();

        moduleParentRt.offsetMin = new(0, -Screen.height);
        moduleParentRt.offsetMax = new(0, -Screen.height);
    }
    #endregion

    #region Appear / Disappear
    public void ModuleAppear()
    {
        currentTween?.Kill();
        scrollTween?.Kill();

        moduleParent.SetActive(true);

        RefreshModules();

        DisableModuleClick();

        Sequence seq = DOTween.Sequence();
        currentTween = seq;

        seq.Join(
            DOTween.To(
                () => moduleParentRt.offsetMin,
                x => moduleParentRt.offsetMin = x,
                Vector2.zero,
                appearTime
            ).SetEase(appearEase)
        );

        seq.Join(
            DOTween.To(
                () => moduleParentRt.offsetMax,
                x => moduleParentRt.offsetMax = x,
                Vector2.zero,
                appearTime
            ).SetEase(appearEase)
        );

        seq.OnComplete(() =>
        {
            EnableModuleClick();
            currentTween = null;
        });
    }

    public void ModuleDisappear(Action onComplete)
    {
        currentTween?.Kill();
        scrollTween?.Kill();

        DisableModuleClick();

        Vector2 target = new(0, -Screen.height);

        Sequence seq = DOTween.Sequence();
        currentTween = seq;

        seq.Join(
            DOTween.To(
                () => moduleParentRt.offsetMin,
                x => moduleParentRt.offsetMin = x,
                target,
                appearTime
            ).SetEase(disappearEase)
        );

        seq.Join(
            DOTween.To(
                () => moduleParentRt.offsetMax,
                x => moduleParentRt.offsetMax = x,
                target,
                appearTime
            ).SetEase(disappearEase)
        );

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            moduleFocus = GameManager.Instance.CurrentModule.ID;
            moduleParent.SetActive(false);
            currentTween = null;
        });
    }
    #endregion

    #region Scroll
    private void ScrollModule(ModuleStyle ms)
    {
        int index = moduleStyles.IndexOf(ms);

        if (index == -1) return;
        if (scrollTween != null) return;
        if (currentTween != null) return;

        if (index < moduleCenter)
        {
            LeftScroll();
        }
        else if (index > moduleCenter)
        {
            RightScroll();
        }
        else
        {
            GameManager.Instance.StartModule(moduleFocus);
        }
    }

    private void LeftScroll()
    {
        if (moduleFocus == Utils.MODULE_MIN) return;

        DisableModuleClick();

        int nextFocus = moduleFocus - 1;

        Sequence seq = DOTween.Sequence();
        scrollTween = seq;

        for (int i = 0; i < moduleCount - 1; i++)
        {
            RectTransform rt = moduleStyles[i].rect;
            Image img = moduleStyles[i].image;

            Vector2 targetPos = moduleConfigs[i+1].position;
            Vector3 targetScale = moduleConfigs[i+1].scale;
            Color targetColor = moduleConfigs[i+1].color;

            seq.Join(
                rt.DOAnchorPos(targetPos, scrollTime)
                .SetEase(scrollEase)
            );

            seq.Join(
                rt.DOScale(targetScale, scrollTime)
                .SetEase(scrollEase)
            );

            seq.Join(
                img.DOColor(targetColor, scrollTime)
                .SetEase(scrollEase)
            );
        }

        seq.OnComplete(() =>
        {
            moduleFocus = nextFocus;

            RotateRight();
            EnableModuleClick();

            scrollTween = null;
        });
    }

    private void RightScroll()
    {
        if (moduleFocus == Utils.MODULE_MAX) return;

        DisableModuleClick();

        int nextFocus = moduleFocus + 1;

        Sequence seq = DOTween.Sequence();
        scrollTween = seq;

        for (int i = 1; i < moduleCount; i++)
        {
            RectTransform rt = moduleStyles[i].rect;
            Image img = moduleStyles[i].image;

            Vector2 targetPos = moduleConfigs[i-1].position;
            Vector3 targetScale = moduleConfigs[i-1].scale;
            Color targetColor = moduleConfigs[i-1].color;

            seq.Join(
                rt.DOAnchorPos(targetPos, scrollTime)
                .SetEase(scrollEase)
            );

            seq.Join(
                rt.DOScale(targetScale, scrollTime)
                .SetEase(scrollEase)
            );

            seq.Join(
                img.DOColor(targetColor, scrollTime)
                .SetEase(scrollEase)
            );
        }

        seq.OnComplete(() =>
        {
            moduleFocus = nextFocus;

            RotateLeft();
            EnableModuleClick();

            scrollTween = null;
        });
    }
    #endregion

    #region Refresh
    private void RefreshModules()
    {
        for (int i = 0; i < moduleParent.transform.childCount; i++)
        {
            RefreshModule(i);
        }
    }
    private void RefreshModule(int i)
    {
        int offset = i - moduleCenter;
        int moduleNum = moduleFocus + offset;
        if (moduleNum < Utils.MODULE_MIN || moduleNum > Utils.MODULE_MAX)
        {
            moduleStyles[i].gameObject.SetActive(false);
        }
        else
        {
            moduleStyles[i].image.sprite = TryToGetSprite(moduleNum);
            moduleStyles[i].image.color = moduleConfigs[i].color;
            moduleStyles[i].rect.anchoredPosition = moduleConfigs[i].position;
            moduleStyles[i].rect.localScale = moduleConfigs[i].scale;
            moduleStyles[i].gameObject.SetActive(true);
        }
    }
    #endregion

    #region Rotate
    private void RotateRight()
    {
        ModuleStyle last = moduleStyles[^1];
        moduleStyles.RemoveAt(moduleCount - 1);
        moduleStyles.Insert(0, last);

        RefreshModule(0);
    }

    private void RotateLeft()
    {
        ModuleStyle first = moduleStyles[0];
        moduleStyles.RemoveAt(0);
        moduleStyles.Add(first);

        RefreshModule(moduleCount - 1);
    }
    #endregion

    #region Button
    private void EnableModuleClick()
    {
        for (int i = 0; i < moduleCount; i++)
        {
            moduleStyles[i].button.interactable = true;
        }
    }

    private void DisableModuleClick()
    {
        for (int i = 0; i < moduleCount; i++)
        {
            moduleStyles[i].button.interactable = false;
        }
    }
    #endregion
}