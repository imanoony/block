using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class UIModule : MonoBehaviour
{
    #region Module UI
    [Header("Module")]
    [SerializeField] private GameObject moduleParent;
    [SerializeField] private Sprite[] moduleSprites;

    private float appearTime = 1f;
    private float scrollTime = 0.5f;

    private Ease appearEase = Ease.OutCubic;
    private Ease disappearEase = Ease.InCubic;
    private Ease scrollEase = Ease.OutCubic;
    private const int moduleCenter = 2;

    private List<GameObject> modulePool = new();
    private List<Image> moduleImages = new();
    private List<Vector2> modulePositions = new();
    private RectTransform rt;

    private int moduleFocus = 0;

    private Tween currentTween = null;
    private Tween scrollTween = null;

    private Sprite TryToGetSprite(int moduleID)
    {
        if (moduleSprites.Length > moduleID + 1 && moduleID >= 0)
            return moduleSprites[moduleID + 1];

        return moduleSprites[0];
    }
    
    public void InitModule()
    {
        rt = moduleParent.GetComponent<RectTransform>();

        for (int i = 0; i < moduleParent.transform.childCount; i++)
        {
            GameObject module = moduleParent.transform.GetChild(i).gameObject;

            modulePool.Add(module);
            moduleImages.Add(module.GetComponent<Image>());
            modulePositions.Add(module.GetComponent<RectTransform>().anchoredPosition);
        }

        rt.offsetMin = new(0, -Screen.height);
        rt.offsetMax = new(0, -Screen.height);
    }
    #endregion

    #region Appear / Disappear
    public void ModuleAppear()
    {
        currentTween?.Kill();
        scrollTween?.Kill();

        moduleParent.SetActive(true);

        RefreshModuleSprites();
        RefreshModuleVisible();
        RefreshModuleStyleImmediate();

        DisableModuleClick();

        Sequence seq = DOTween.Sequence();
        currentTween = seq;

        seq.Join(
            DOTween.To(
                () => rt.offsetMin,
                x => rt.offsetMin = x,
                Vector2.zero,
                appearTime
            ).SetEase(appearEase)
        );

        seq.Join(
            DOTween.To(
                () => rt.offsetMax,
                x => rt.offsetMax = x,
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
                () => rt.offsetMin,
                x => rt.offsetMin = x,
                target,
                appearTime
            ).SetEase(disappearEase)
        );

        seq.Join(
            DOTween.To(
                () => rt.offsetMax,
                x => rt.offsetMax = x,
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
    public void ScrollModule(GameObject module)
    {
        int index = modulePool.IndexOf(module);

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

        moduleImages[0].sprite =
            TryToGetSprite(nextFocus - 1);

        Sequence seq = DOTween.Sequence();
        scrollTween = seq;

        for (int i = 0; i < modulePool.Count - 1; i++)
        {
            RectTransform rt = modulePool[i].GetComponent<RectTransform>();
            Image img = moduleImages[i];

            Vector2 targetPos = modulePositions[i + 1];

            Vector3 targetScale =
                i == moduleCenter - 1
                ? Vector3.one * Utils.MODULE_HIGHLIGHT_SCALE
                : Vector3.one;

            Color targetColor =
                i == moduleCenter - 1
                ? Color.white
                : Utils.CodeToColor(Utils.GRAY);

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

            RefreshModuleVisible();

            EnableModuleClick();

            scrollTween = null;
        });
    }

    private void RightScroll()
    {
        if (moduleFocus == Utils.MODULE_MAX) return;

        DisableModuleClick();

        int nextFocus = moduleFocus + 1;

        moduleImages[^1].sprite =
            TryToGetSprite(nextFocus + 1);

        Sequence seq = DOTween.Sequence();
        scrollTween = seq;

        for (int i = 1; i < modulePool.Count; i++)
        {
            RectTransform rt = modulePool[i].GetComponent<RectTransform>();
            Image img = moduleImages[i];

            Vector2 targetPos = modulePositions[i - 1];

            Vector3 targetScale =
                i == moduleCenter + 1
                ? Vector3.one * Utils.MODULE_HIGHLIGHT_SCALE
                : Vector3.one;

            Color targetColor =
                i == moduleCenter + 1
                ? Color.white
                : Utils.CodeToColor(Utils.GRAY);

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

            RefreshModuleVisible();

            EnableModuleClick();

            scrollTween = null;
        });
    }
    #endregion

    #region Refresh
    private void RefreshModuleSprites()
    {
        moduleImages[moduleCenter].sprite =
            TryToGetSprite(moduleFocus);

        moduleImages[moduleCenter - 1].sprite =
            TryToGetSprite(moduleFocus - 1);

        moduleImages[moduleCenter + 1].sprite =
            TryToGetSprite(moduleFocus + 1);

        moduleImages[0].sprite =
            TryToGetSprite(moduleFocus - 2);

        moduleImages[^1].sprite =
            TryToGetSprite(moduleFocus + 2);
    }

    private void RefreshModuleVisible()
    {
        for (int i = 0; i < modulePool.Count; i++)
            modulePool[i].SetActive(true);

        if (moduleFocus == Utils.MODULE_MIN)
            modulePool[moduleCenter - 1].SetActive(false);

        if (moduleFocus == Utils.MODULE_MIN + 1)
            modulePool[0].SetActive(false);

        if (moduleFocus == Utils.MODULE_MAX)
            modulePool[moduleCenter + 1].SetActive(false);

        if (moduleFocus == Utils.MODULE_MAX - 1)
            modulePool[^1].SetActive(false);
    }

    private void RefreshModuleStyleImmediate()
    {
        for (int i = 0; i < modulePool.Count; i++)
        {
            RectTransform rt =
                modulePool[i].GetComponent<RectTransform>();

            Image img =
                moduleImages[i];

            rt.anchoredPosition = modulePositions[i];

            if (i == moduleCenter)
            {
                rt.localScale =
                    Vector3.one * Utils.MODULE_HIGHLIGHT_SCALE;

                img.color = Color.white;
            }
            else
            {
                rt.localScale = Vector3.one;

                img.color =
                    Utils.CodeToColor(Utils.GRAY);
            }
        }
    }
    #endregion

    #region Rotate
    private void RotateRight()
    {
        GameObject last = modulePool[^1];
        modulePool.RemoveAt(modulePool.Count - 1);
        modulePool.Insert(0, last);

        Image lastImage = moduleImages[^1];
        moduleImages.RemoveAt(moduleImages.Count - 1);
        moduleImages.Insert(0, lastImage);

        RefreshModuleStyleImmediate();
        RefreshModuleSprites();
    }

    private void RotateLeft()
    {
        GameObject first = modulePool[0];
        modulePool.RemoveAt(0);
        modulePool.Add(first);

        Image firstImage = moduleImages[0];
        moduleImages.RemoveAt(0);
        moduleImages.Add(firstImage);

        RefreshModuleStyleImmediate();
        RefreshModuleSprites();
    }
    #endregion

    #region Button
    private void EnableModuleClick()
    {
        for (int i = 0; i < modulePool.Count; i++)
        {
            if (modulePool[i].TryGetComponent<Button>(out var btn))
                btn.interactable = true;
        }
    }

    private void DisableModuleClick()
    {
        for (int i = 0; i < modulePool.Count; i++)
        {
            if (modulePool[i].TryGetComponent<Button>(out var btn))
                btn.interactable = false;
        }
    }
    #endregion
}