using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BlockPort: MonoBehaviour
{
    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;

    private const string PortExpr = "_PortExpr";

    [SerializeField] private float inactiveScale = 0.5f;
    [SerializeField] private float inactiveAlpha = 0.2f;
    [SerializeField] private float portCoDuration = 0.1f;
    private const float activeScale = 1f;
    private const float activeAlpha = 1f;

    private int portTick = -1;

    private bool initialized = false;
    public void Initialize(
        Vector2Int grid,
        PortExpr port,
        List<Vector2Int> blockTiles,
        int blockWidth,
        int blockHeight,
        int tick = -1
    )
    {
        if (initialized) return;

        Vector2 gridPos = GetGridPosition(grid, blockWidth, blockHeight);
        float xoffset = GetOffsetX(grid, blockTiles);
        float yoffset = GetOffsetY(grid, blockTiles);
        
        gameObject.transform.localPosition = new(gridPos.x + xoffset, gridPos.y + yoffset);

        sr = gameObject.GetComponent<SpriteRenderer>();
        mpb = new();

        Vector4 v = Port2Vector4(port);
        mpb.SetVector(PortExpr, v);
        sr.SetPropertyBlock(mpb);

        portTick = tick;
        if (portTick != -1 && portTick != 0)
        {
            gameObject.transform.localScale = new(inactiveScale, inactiveScale);
            sr.color = new(sr.color.r, sr.color.g, sr.color.b, inactiveAlpha);
        }
        if (portTick != -1)
        {
            GameManager.Instance.Time.OnTick += PortUpdateTickHandler;
        }

        initialized = true;
    }

    void OnDestroy()
    {
        GameManager.Instance.Time.OnTick -= PortUpdateTickHandler;
    }

    public void SetBlockSortingOrder(int sortingOrder)
    {
        sr.sortingOrder = sortingOrder + 1;
    }

    private Vector2 GetGridPosition(
        Vector2Int grid,
        int blockWidth,
        int blockHeight
    )
    {
        return new(
            grid.y - blockWidth / 2f,
            blockHeight / 2f - grid.x
        );
    }

    private float GetOffsetX(
        Vector2Int grid,
        List<Vector2Int> blockTiles
    )
    {
        float xoffset = 0f;
        if (
            !blockTiles.Contains(new(grid.x-1, grid.y-1)) && 
            !blockTiles.Contains(new(grid.x, grid.y-1))
        ) // empty left
        {
            xoffset += Utils.PORT_OFFSET;
        }
        if (
            !blockTiles.Contains(new(grid.x-1, grid.y)) &&
            !blockTiles.Contains(grid)
        ) // empty right
        {
            xoffset -= Utils.PORT_OFFSET;
        }

        return xoffset;
    }

    private float GetOffsetY(
        Vector2Int grid,
        List<Vector2Int> blockTiles
    )
    {
        float yoffset = 0f;
        if (
            !blockTiles.Contains(new(grid.x-1, grid.y-1)) &&
            !blockTiles.Contains(new(grid.x-1, grid.y))
        ) // empty up
        {
            yoffset -= Utils.PORT_OFFSET;
        }
        if (
            !blockTiles.Contains(new(grid.x, grid.y-1)) &&
            !blockTiles.Contains(new(grid.x, grid.y))
        ) // empty down
        {
            yoffset += Utils.PORT_OFFSET;
        }
        
        return yoffset;
    }

    private Vector4 Port2Vector4(PortExpr port)
    {
        if (port == null) return new(0, 0, 0, 0);
        else if (port is PortVar var) return new(1, 1, 1, 1);
        else if (port is PortVert vert)
        {
            int u, d;
            if (vert.Up == null) u = 0;
            else u = 1;
            if (vert.Down == null) d = 0;
            else d = 1;
            return new(u, d, u, d);
        }
        else if (port is PortHorz horz)
        {
            int l, r;
            if (horz.Left == null) l = 0;
            else l = 1;
            if (horz.Right == null) r = 0;
            else r = 1;
            return new(l, l, r, r);
        }
        else return new(0, 0, 0, 0);
    }

    #region Transition
    private void PortUpdateTickHandler(int tick)
    {
        if (currentPortCo != null)
        {
            StopCoroutine(currentPortCo);
            currentPortCo = null;
        }

        currentPortTween?.Kill();
        currentPortTween = null;

        bool goActive = tick == portTick;
        currentPortCo = StartCoroutine(PortCo(goActive, portCoDuration));
    }

    private Coroutine currentPortCo = null;
    private Sequence currentPortTween = null;
    private IEnumerator PortCo(
        bool goActive,
        float duration,
        Action onComplete = null
    )
    {
        float targetScale, targetAlpha;

        if (goActive)
        {
            targetScale = activeScale;
            targetAlpha = activeAlpha;
        }
        else
        {
            targetScale = inactiveScale;
            targetAlpha = inactiveAlpha;
        }

        currentPortTween = DOTween.Sequence();
        currentPortTween.Join(transform.DOScale(targetScale, duration));
        currentPortTween.Join(sr.DOFade(targetAlpha, duration));

        yield return currentPortTween.WaitForCompletion();

        currentPortTween = null;
        currentPortCo = null;
        onComplete?.Invoke();
        yield break;
    }

    #endregion
}