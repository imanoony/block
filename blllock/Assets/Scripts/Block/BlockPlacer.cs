using System.Collections.Generic;
using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;

    public void PlaceBlocks(StageData stage)
    {
        // 사용할 세 리스트 미리 정리하기
        List<int> blocks = stage.Blocks;
        List<int> rotate = stage.RIndex, flip = stage.FIndex;

        List<(int, bool, bool)> triples = new();
        for (int i = 0; i < blocks.Count; i++)
        {
            bool r = rotate.Contains(i);
            bool f = flip.Contains(i);
            triples.Add((blocks[i], r, f));
        }
        triples.Shuffle();

        for (int i = 0; i < triples.Count; i++)
            PlaceBlock(triples[i].Item1, triples[i].Item2, triples[i].Item3);
    }

    private void PlaceBlock(int id, bool canRotate, bool canFlip)
    {
        BlockData blockData = GameManager.Instance.BlockLibrary[id];
        GameObject instance = Instantiate(blockPrefab, GetPlacedWorld(), Quaternion.identity);
        instance.GetComponent<BlockInstance>().Initialize(blockData, canRotate, canFlip);
    }

    private Vector3 GetPlacedWorld()
    {
        return new Vector3(0, 0);
    }
}