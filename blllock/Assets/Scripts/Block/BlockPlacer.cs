using System.Collections.Generic;
using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Sprite[] blockSprites;

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
        {
            if (i < triples.Count / 2) PlaceBlock(triples[i].Item1, true, triples[i].Item2, triples[i].Item3);
            else PlaceBlock(triples[i].Item1, false, triples[i].Item2, triples[i].Item3);
        }
    }

    private void PlaceBlock(int id, bool isLeft, bool canRotate, bool canFlip)
    {
        BlockData blockData = GameManager.Instance.BlockLibrary[id];
        GameObject instance = Instantiate(blockPrefab, GetPlacedWorld(isLeft), Quaternion.identity);
        instance.GetComponent<BlockInstance>().Initialize(blockData, blockSprites[id], canRotate, canFlip);
    }

    private Vector3 GetPlacedWorld(bool isLeft)
    {
        Camera cam = Camera.main;
        Vector3 center = cam.transform.position;
        float halfW = cam.orthographicSize * cam.aspect;
        float halfH = cam.orthographicSize;

        float randX = Random.Range(1f, 2f);
        float randY = Random.Range(-halfH, halfH);

        if (isLeft) return new Vector3(center.x - halfW / 2f * randX, center.y + randY);
        else return new Vector3(center.x + halfW / 2f * randX, center.y + randY);
    }
}