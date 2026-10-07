using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIModuleScroll : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum Axis { Horizontal, Vertical }

    [Header("Meta")]
    [SerializeField] private UIModuleMeta meta;

    [Header("Layout")]
    [SerializeField] private Axis axis = Axis.Horizontal;
    [SerializeField] private List<GameObject> moduleObjects;
    [SerializeField] private RectTransform viewport;


    [Header("Feel")]
    [SerializeField, Range(0.001f, 1f)] private float decelerationRaate = 0.135f;
    [SerializeField] private float snapSpeedThreshold = 1.5f;
    [SerializeField] private float snapSmoothTime = 0.12f;
    [SerializeField] private float overscrollResistance = 0.35f;
    [SerializeField] private float maxOverscroll = 0.5f;
    [SerializeField] private float maxSpeed = 1.2f;
    [SerializeField] private float metaChangeThreshold = 0.35f;
    [SerializeField] private float metaAppearThreshold = 0.3f;

    public Action<UIModule, int> OnBindItem;
    public Action<int> OnFocusChanged;
    public Action<int> OnMetaFocusChanged;

    private List<RectTransform> moduleRects = new();
    private List<UIModule> modules = new();
    private int[] bindIndex; // pool index to module id

    private float pos;
    private float velocity;
    private float spacing = 860f;
    private  int snapTarget;
    private bool dragging;
    private bool snapping;
    private bool initialized = false;

    private bool IsH => axis == Axis.Horizontal;

    private int poolCount = 5;
    private int moduleCount = 0;
    private int focusedIndex = 0;
    private UIModule focusedModule = null;

    public void Init(int moduleCount)
    {
        if (initialized) return;

        this.moduleCount = moduleCount;

        poolCount = moduleObjects.Count;
        moduleRects.Clear();
        for (int i = 0; i < poolCount; i++)
        {
            moduleRects.Add(moduleObjects[i].GetComponent<RectTransform>());
            modules.Add(moduleObjects[i].GetComponent<UIModule>());
        }
        bindIndex = new int[poolCount];
        Array.Fill(bindIndex, -1);

        dragging = false;
        snapping = false;
        velocity = 0f;

        initialized = true;
    }

    public void InitFocus(int focusedIndex)
    {
        this.focusedIndex = focusedIndex;
        pos = focusedIndex;

        dragging = false;
        snapping = false;
        velocity = 0f;

        Refresh();
    }

    public void ScrollToNext(bool instant = false) => ScrollTo(focusedIndex + 1, instant);
    public void ScrollToPrev(bool instant = false) => ScrollTo(focusedIndex - 1, instant);
    public void ScrollTo(int indexToFocus, bool instant = false)
    {
        int focus = Mathf.Clamp(indexToFocus, 0, moduleCount - 1);
        if (instant)
        {
            pos = focus;
            snapping = false;
            velocity = 0f;

            Refresh();
        }
        else
        {
            snapTarget = focus;
            snapping = true;
        }
    }

    private void Refresh()
    {
        int center = Mathf.RoundToInt(pos);
        int half = poolCount / 2;

        int focus = Mathf.Clamp(Mathf.RoundToInt(pos), 0, moduleCount - 1);
        if (focus != focusedIndex)
        {
            focusedIndex = focus;
            OnFocusChanged?.Invoke(focusedIndex);

            focusedModule.SetFocused(false);
        }

        for (int off = -half; off <= half; off++)
        {
            int i = center + off;
            int slot = ((i % poolCount) + poolCount) % poolCount;
            RectTransform rt = moduleRects[slot];
            if (i == focus) 
            {
                focusedModule = modules[slot];
                focusedModule.SetFocused(true);
            }

            if (i < 0 || i > moduleCount - 1)
            {
                if (rt.gameObject.activeSelf) rt.gameObject.SetActive(false);
                bindIndex[slot] = -1;
                continue;
            }

            if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
            if (bindIndex[slot] != i)
            {
                bindIndex[slot] = i;
                OnBindItem?.Invoke(modules[slot], i);
            }

            float offset = (i - pos) * spacing;
            rt.anchoredPosition = IsH ? new Vector2(offset, 0) : new Vector2(0, -offset);
        }
        
        float closeness = Mathf.Abs(pos - focus);

        if (Mathf.Abs(velocity) < snapSpeedThreshold)
        {
            if (
                closeness < metaChangeThreshold && 
                !meta.CheckModuleID(focus)
            )
                OnMetaFocusChanged?.Invoke(focus);
            meta.SetAlpha(Mathf.Clamp01(-(1/metaAppearThreshold)*closeness + 1));

        }
        if (focusedModule != null) 
            focusedModule.SetScrimAlpha(Mathf.Clamp01(1/metaAppearThreshold*closeness) * 0.2f);
    }

    public void OnBeginDrag(PointerEventData e) 
    {
        dragging = true;
        snapping = false;
        velocity = 0f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport, 
            e.position - e.delta,
            e.pressEventCamera,
            out var prev
        );
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            e.position,
            e.pressEventCamera,
            out var curr
        );

        float deltaPx = IsH ? (curr.x - prev.x) : (curr.y - prev.y);
        float deltaItems = (IsH ? -deltaPx : deltaPx) / spacing;

        if (
            (pos <= 0f && deltaItems < 0f) || 
            (pos >= moduleCount - 1 && deltaItems > 0f)
        )
        {
            deltaItems *= overscrollResistance;
        }

        pos = Mathf.Clamp(
            pos + deltaItems,
            -maxOverscroll,
            moduleCount - 1 + maxOverscroll
        );

        float dt = Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
        float instV = deltaItems / dt;
        velocity = Mathf.Lerp(velocity, instV, 0.5f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        dragging = false;
        velocity = Mathf.Clamp(velocity, -maxSpeed, maxSpeed);
        snapping = false;
    }

    void Update()
    {
        if (!initialized) return;
        if (dragging)
        {
            Refresh();
            return;
        }

        float dt = Time.unscaledDeltaTime;

        if (!snapping)
        {
            if (Mathf.Abs(velocity) > 0f)
            {
                pos += velocity  * dt;
                velocity *= Mathf.Pow(decelerationRaate, dt);
            }

            bool outOfBounds = pos < 0f || pos > moduleCount - 1;
            if (outOfBounds || Mathf.Abs(velocity) < snapSpeedThreshold)
                BeginSnap();
        }

        if (snapping)
        {
            pos = Mathf.SmoothDamp(
                pos,
                snapTarget,
                ref velocity,
                snapSmoothTime,
                Mathf.Infinity,
                dt
            );

            if (
                Mathf.Abs(pos - snapTarget) < 0.0005f && 
                Mathf.Abs(velocity) < 0.001f
            )
            {
                pos = snapTarget;
                velocity = 0f;
                snapping = false;
            }
        }

        Refresh();
    }

    private void BeginSnap()
    {
        snapping = true;
        snapTarget = Mathf.Clamp(
            Mathf.RoundToInt(pos), 
            0, 
            moduleCount - 1
        );
    }
}
