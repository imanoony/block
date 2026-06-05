using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BlockPlacer : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject blockParent;
    [SerializeField] private GameObject placedBlockRoot;
    [SerializeField] private Sprite[] blockSprites;
    [SerializeField] private Sprite[] sblockSprites;
    [SerializeField] private Sprite[] blockGhostSprites;

    private List<GameObject> blockInstances = null;
    public void RemoveBlocks()
    {
        if (blockInstances != null)
        {
            foreach (var block in blockInstances)
                Destroy(block);
            blockInstances.Clear();
        }
        blockInstances = null;
    }

    public void PlaceBlocks(StageData stage)
    {
        blockInstances = new List<GameObject>();
        blockParent.transform.localPosition = Vector3.zero;

        // 사용할 세 리스트 미리 정리하기
        List<int> blocks = stage.Blocks;
        List<int> rotate = stage.RIndex, flip = stage.FIndex;
        List<int> spike = stage.SIndex;
        List<Vector2Int> blockPositions = stage.BlockPositions;
        if (blockPositions.Count < blocks.Count)
        {
            // 부족한만큼 (0, 0)으로 채워넣기
            for (int i = blockPositions.Count; i < blocks.Count; i++)
                blockPositions.Add(Vector2Int.zero);
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            bool r = rotate.Contains(i);
            bool f = flip.Contains(i);
            bool s = spike.Contains(i);
            blockInstances.Add(PlaceBlock(blocks[i], blockPositions[i], r, f, s));
        }
    }

    private GameObject PlaceBlock(
        int id, 
        Vector2Int pos, 
        bool canRotate, 
        bool canFlip, 
        bool hasSpike
    )
    {
        GameObject instance = Instantiate(blockPrefab, blockParent.transform);
        BlockData blockData = new BlockData(GameManager.Instance.BlockLibrary[id]);
        BlockInstance blockInstance = instance.GetComponent<BlockInstance>();
        
        Sprite sprite = hasSpike ? sblockSprites[id] : blockSprites[id];
        Sprite ghostSprite = blockGhostSprites[id];
        blockInstance.Initialize(blockData, sprite, pos, ghostSprite, placedBlockRoot.transform, canRotate, canFlip, hasSpike);

        return instance;
    }

    [HideInInspector] public bool BlockAppearTransDone = false;
    [HideInInspector] public bool BlockDisappearTransDone = false;
    public void BlockAppear()
    {
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(BlockAppearCo());
    }

    public void BlockDisappear()
    {
        if (currentCo != null) StopCoroutine(currentCo);
        currentCo = StartCoroutine(BlockDisappearCo());
    }

    #region Transition
    private Ease transitionEase = Ease.OutCubic;
    private Tween currentTween = null;
    private Coroutine currentCo = null;
    private IEnumerator BlockAppearCo()
    {
        currentTween?.Kill();

        Sequence seq = DOTween.Sequence();
        currentTween = seq;

        Camera cam = Camera.main;

        for (int i = 0; i < blockInstances.Count; i++)
        {
            GameObject block = blockInstances[i];
            if (block == null) continue;

            Vector3 targetPos = block.transform.position;

            Vector3 viewportPos = cam.WorldToViewportPoint(targetPos);
            viewportPos.y = -0.2f;

            Vector3 startPos = cam.ViewportToWorldPoint(viewportPos);
            startPos.z = targetPos.z;
            block.transform.position = startPos;

            Tween t = block.transform.DOMove(targetPos, 1.2f)
                .SetEase(transitionEase)
                .SetDelay(i * 0.1f);
            
            seq.Join(t);
        }

        yield return seq.WaitForCompletion();
        
        if (currentTween == seq) currentTween = null;
        currentCo = null;

        BlockAppearTransDone = true;
    }
    private IEnumerator BlockDisappearCo()
    {
        currentTween?.Kill();
        if (blockParent.transform.childCount == 0)
        {
            BlockDisappearTransDone = true;
            currentCo = null;
            yield break;
        }

        Camera cam = Camera.main;
        Vector3 startPos = blockParent.transform.position;

        Vector3 viewportPos = cam.WorldToViewportPoint(startPos);
        viewportPos.y -= 1f;
        
        Vector3 targetPos = cam.ViewportToWorldPoint(viewportPos);
        targetPos.z = startPos.z;

        Tween t = blockParent.transform.DOMove(targetPos, 1.2f).SetEase(Ease.InQuad);
        currentTween = t;

        yield return t.WaitForCompletion();

        if (currentTween == t) currentTween = null;
        currentCo = null;

        BlockDisappearTransDone = true;
    }
    #endregion
}