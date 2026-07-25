using DG.Tweening;
using UnityEngine;

public enum CablePart
{
    None,
    Edge,
    Node
}

public class CableInstance : MonoBehaviour
{
    public CablePart Part { get; private set; } = CablePart.None;
    private GridManager gm;
    private SpriteRenderer sr;
    private GameObject shadow;
    private SpriteRenderer shadowSr;
    private MaterialPropertyBlock mpb;
    
    #region Edge
    private Cable edge;
    public void Initialize(
        CablePart part,
        Cable edge,
        Sprite sprite
    )
    {
        if (part != CablePart.Edge)
            throw new System.Exception("CableInstance.Initialize(): inappropriate cable type");
        
        this.edge = edge;
        Part = CablePart.Edge;

        gm = GameManager.Instance.Grid;
        sr = gameObject.GetComponent<SpriteRenderer>();

        // 위치 계산
        // edge의 Start, End가 가로로 연결되어 있는지 세로로 연결되어 있는지부터 파악.
        bool isH = edge.IsHorizontal();

        float targetX, targetY;
        Vector3 basePos = (Vector3)gm.GetTileTopLeftWorld(
            edge.A.x + gm.GetCircuitStart().x, 
            edge.A.y + gm.GetCircuitStart().y
        );
        if (isH) 
        {
            targetX = basePos.x + gm.GetTileSize().x / 2f;
            targetY = basePos.y;
        }
        else    
        {
            targetX = basePos.x;
            targetY = basePos.y - gm.GetTileSize().y / 2f;
        }
        Rotate targetR = isH ? Rotate.None : Rotate.Rotate90;

        transform.position = new(targetX, targetY);
        transform.rotation = Quaternion.Euler(0f, 0f, -(float)targetR);
        
        sr.sprite = sprite;

        shadow = transform.GetChild(0).gameObject;
        shadowSr = shadow.GetComponent<SpriteRenderer>();
        shadowSr.sprite = sprite;

        shadow.transform.localPosition = -Utils.GetCableShadowOffset(
            Utils.CABLE_SHADOW,
            targetR
        );

        mpb = new();
    }
    #endregion
    
    #region Node
    //private Vector2Int node;
    public void Initialize(
        CablePart part,
        Vector2Int node,
        Sprite sprite,
        Rotate rotate
    )
    {
        if (part != CablePart.Node)
            throw new System.Exception("CableInstance.Initialize(): inappropriate cable type");
        
        //this.node = node;
        Part = CablePart.None;

        gm = GameManager.Instance.Grid;
        sr = gameObject.GetComponent<SpriteRenderer>();

        Vector3 targetPos = (Vector3)gm.GetTileTopLeftWorld(
            node.x + gm.GetCircuitStart().x, 
            node.y + gm.GetCircuitStart().y
        );
        transform.position = targetPos;
        transform.rotation = Quaternion.Euler(0f, 0f, -(float)rotate);

        sr.sprite = sprite;

        shadow = transform.GetChild(0).gameObject;
        shadowSr = shadow.GetComponent<SpriteRenderer>();
        shadowSr.sprite = sprite;

        shadow.transform.localPosition = -Utils.GetCableShadowOffset(
            Utils.CABLE_SHADOW,
            rotate
        );

        Destroy(gameObject.GetComponent<BoxCollider2D>());

        mpb = new();
    }
    #endregion

    private void OnMouseDown()
    {
        if (Part == CablePart.Edge)
        {
            Debug.Log("Cable Mouse Down");
            gm.CablePlacer.RemoveCable(gm, edge);
        }
    }

    private const string maskIndex = "_MaskIndex", isCurve = "_IsCurve", isNode = "_IsNode";
    private const string rotation = "_Rotation", flip = "_Flip";
    public Tween GetPlaceEdgeTween(
        Vector2Int start,
        Vector2Int end,
        float duration = 0.2f,
        Ease ease = Ease.Linear
    )
    {
        Rotate rotate = Rotate.None; 
        if (start.x == end.x && start.y > end.y) rotate = Rotate.Rotate180;
        else if (start.y == end.y && start.x > end.x) rotate = Rotate.Rotate180;

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(isNode, 0f);
        mpb.SetFloat(isCurve, 0f);
        mpb.SetFloat(rotation, (float)rotate);
        mpb.SetFloat(maskIndex, 0f);
        sr.SetPropertyBlock(mpb);
        shadowSr.SetPropertyBlock(mpb);

        sr.enabled = false;
        shadowSr.enabled = false;

        return DOTween.To(
            () => 0,
            value =>
            {
                mpb.SetFloat(maskIndex, value);
                sr.SetPropertyBlock(mpb);
                shadowSr.SetPropertyBlock(mpb);
            },
            Utils.CABLE_ANIM_EDGE_COUNT - 1,
            duration
        )
        .SetEase(ease)
        .OnStart(() =>
        {
            sr.enabled = true;
            shadowSr.enabled = true;
        });
    }

    public Tween GetPlaceNodeTween(
        CableConnection old,
        CableConnection now,
        bool isEnd,
        float duration = 0.2f,
        Ease ease = Ease.Linear
    )
    {
        Rotate rotate = Rotate.None;
        bool curve = false;

        int nowI = (int)now;
        int oldI = (int)old;

        switch (nowI)
        {
            case 0b1000 or 0b0100 or 0b0010 or 0b0001:
                if (isEnd) rotate = Rotate.Rotate180;
                break;
            case 0b1100:
                if (oldI == 0b1000 && !isEnd) rotate = Rotate.Rotate180;
                if (oldI == 0b0100 && isEnd) rotate = Rotate.Rotate180;
                break;
            case 0b0011:
                if (oldI == 0b0001 && isEnd) rotate = Rotate.Rotate180;
                if (oldI == 0b0010 && !isEnd) rotate = Rotate.Rotate180;
                break;
            case 0b1110:
                if (oldI == 0b1100)
                {
                    if (isEnd) rotate = Rotate.Rotate90;
                    else rotate = Rotate.Rotate270;
                }
                else if (oldI == 0b1010 && !isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b0110 && isEnd) rotate = Rotate.Rotate180;
                break;
            case 0b1101:
                if (oldI == 0b1100)
                {
                    if (isEnd) rotate = Rotate.Rotate90;
                    else rotate = Rotate.Rotate270;
                } 
                else if (oldI == 0b1001 && isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b0101 && !isEnd) rotate = Rotate.Rotate180;
                break;
            case 0b1011:
                if (oldI == 0b1010 && isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b1001 && !isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b0011)
                {
                    if (isEnd) rotate = Rotate.Rotate90;
                    else rotate = Rotate.Rotate270;
                }
                break;
            case 0b0111:
                if (oldI == 0b0110 && !isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b0101 && isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b0011)
                {
                    if (isEnd) rotate = Rotate.Rotate90;
                    else rotate = Rotate.Rotate270;
                }
                break;
            case 0b1111:
                if (oldI == 0b1110)
                {
                    if (isEnd) rotate = Rotate.Rotate270;
                    else rotate = Rotate.Rotate90;
                } 
                else if (oldI == 0b1101)
                {
                    if (isEnd) rotate = Rotate.Rotate90;
                    else rotate = Rotate.Rotate270;
                }
                else if (oldI == 0b1011 && !isEnd) rotate = Rotate.Rotate180;
                else if (oldI == 0b0111 && isEnd) rotate = Rotate.Rotate180;
                break;
        }
        // old와 now를 바탕으로 Rotate, Curve, Flip 결정

        sr.GetPropertyBlock(mpb);
        mpb.SetFloat(isNode, 1f);
        mpb.SetFloat(isCurve, curve ? 1f : 0f);
        mpb.SetFloat(rotation, (float)rotate);
        mpb.SetFloat(maskIndex, 0f);
        sr.SetPropertyBlock(mpb);
        shadowSr.SetPropertyBlock(mpb);

        sr.enabled = false;
        shadowSr.enabled = false;
        
        return DOTween.To(
            () => 0,
            value =>
            {
                mpb.SetFloat(maskIndex, value);
                sr.SetPropertyBlock(mpb);
                shadowSr.SetPropertyBlock(mpb);
            },
            curve ? Utils.CABLE_ANIM_NODE_CURVE_COUNT - 1 : Utils.CABLE_ANIM_NODE_COUNT - 1,
            duration
        )
        .SetEase(ease)
        .OnStart(() =>
        {
            sr.enabled = true;
            shadowSr.enabled = true;
        });
    }
}
