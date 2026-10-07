using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine.EventSystems;

public class UIModule : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image image;
    [SerializeField] private Image scrimImage;

    private bool isFocused = false;
    private int moduleID = -1;

    public void InitModule(ModuleData module)
    {
        if (module.Thumbnail != null)
            image.sprite = module.Thumbnail;

        moduleID = module.ID;
    }

    public void SetScrimAlpha(float alpha)
    {
        scrimImage.color = new(
            scrimImage.color.r,
            scrimImage.color.g,
            scrimImage.color.b,
            alpha
        );
    }

    public void SetFocused(bool focused = true) => isFocused = focused;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (moduleID == -1) return;
        if (!isFocused) return;

        GameManager.Instance.StartModule(moduleID);
    }
}