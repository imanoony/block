using System.Collections;
using DG.Tweening;
using UnityEngine;

public class ResistorInstance : MonoBehaviour
{
    private Resistor resistorData;
    private bool isH;
    private bool isTweening = false;
    public void StartTweening() => isTweening = true;
    public void EndTweening() => isTweening = false;

    public bool IsInteractable()
    {
        if (isTweening) return false;
        return true;
    }

    [SerializeField] private string hResistorAnim = "HResistorAnim";
    [SerializeField] private string vResistorAnim = "VResistorAnim";
    private Animator animator;
    private GridManager gm;

    public void Initialize(
        Vector2Int start,
        Vector2Int end
    )
    {
        if (!Utils.IsAdjacentGrids(start, end)) { Utils.PrintError("Resistor의 양 끝 점은 이웃이어야 함."); return; }

        resistorData = new(start, end);
        if (start.x == end.x) isH = true;
        else isH = false;

        animator = gameObject.GetComponent<Animator>();
        gm = GameManager.Instance.Grid;

        float targetX, targetY;
        Vector3 basePos = (Vector3)gm.GetTileTopLeftWorld(
            resistorData.A.x, 
            resistorData.A.y
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

        transform.position = new(targetX, targetY);
    }

    private Coroutine resistorCo = null;
    public void ResistorAppear()
    {
        if (resistorCo != null) StopCoroutine(resistorCo);
        resistorCo = StartCoroutine(ResistorAppearCo());
    }
    private IEnumerator ResistorAppearCo()
    {
        string anim = isH ? hResistorAnim : vResistorAnim;
        animator.Play(anim);

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
        
        resistorCo = null;
        yield break;
    }
}