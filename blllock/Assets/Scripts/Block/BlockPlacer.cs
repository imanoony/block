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

        List<(int, bool, bool, bool)> quad = new();
        for (int i = 0; i < blocks.Count; i++)
        {
            bool r = rotate.Contains(i);
            bool f = flip.Contains(i);
            bool s = spike.Contains(i);
            quad.Add((blocks[i], r, f, s));
        }
        quad.Shuffle();

        for (int i = 0; i < quad.Count; i++)
        {
            if (i < quad.Count / 2) blockInstances.Add(PlaceBlock(quad[i].Item1, true, quad[i].Item2, quad[i].Item3, quad[i].Item4));
            else blockInstances.Add(PlaceBlock(quad[i].Item1, false, quad[i].Item2, quad[i].Item3, quad[i].Item4));
        }
    }

    private GameObject PlaceBlock(int id, bool isLeft, bool canRotate, bool canFlip, bool hasSpike)
    {
        BlockData blockData = new BlockData(GameManager.Instance.BlockLibrary[id]);
        GameObject instance = Instantiate(blockPrefab, GetPlacedWorld(isLeft), Quaternion.identity);
        Sprite sprite = hasSpike ? sblockSprites[id] : blockSprites[id];
        instance.GetComponent<BlockInstance>().Initialize(blockData, sprite, canRotate, canFlip, hasSpike);

        return instance;
    }

    private Vector3 GetPlacedWorld(bool isLeft)
    {
        Camera cam = Camera.main;
        Vector3 center = cam.transform.position;
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;

        float randX = Random.Range(1f, 2f);
        float randY = Random.Range(-halfH, halfH);

        if (isLeft) return new Vector3(center.x - halfW / 2f * randX, center.y + randY, Utils.BLOCK_Z);
        else return new Vector3(center.x + halfW / 2f * randX, center.y + randY, Utils.BLOCK_Z);
    }
}