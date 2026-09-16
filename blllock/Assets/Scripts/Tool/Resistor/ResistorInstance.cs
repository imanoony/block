using System.Collections;
using DG.Tweening;
using UnityEngine;

public class ResistorInstance : MonoBehaviour
{
    public Resistor ResistorData { get; private set; }
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

    public void Initialize(Resistor resistor)
    {
        ResistorData = resistor;
        if (resistor.A.x == resistor.B.x) isH = true;
        else isH = false;

        animator = gameObject.GetComponent<Animator>();
        gm = GameManager.Instance.Grid;

        float targetX, targetY;
        Vector3 basePos = (Vector3)gm.GetTileTopLeftWorld(
            ResistorData.A.x, 
            ResistorData.A.y
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