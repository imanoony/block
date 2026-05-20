using System.Collections.Generic;
using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Sprite[] blockSprites;
    [SerializeField] private Sprite[] sblockSprites;

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
        GameObject instance = Instantiate(blockPrefab);
        BlockData blockData = new BlockData(GameManager.Instance.BlockLibrary[id]);
        BlockInstance blockInstance = instance.GetComponent<BlockInstance>();
        
        Sprite sprite = hasSpike ? sblockSprites[id] : blockSprites[id];
        blockInstance.Initialize(blockData, sprite, pos, canRotate, canFlip, hasSpike);

        return instance;
    }
}