using System.Collections.Generic;
using UnityEngine;

public class BlockPort: MonoBehaviour
{
    private SpriteRenderer sr;
    public void Initialize(
        Vector2Int grid,
        PortExpr port,
        List<Vector2Int> blockTiles,
        int blockWidth,
        int blockHeight,
        int tick = -1
    )
    {
        Vector2 gridPos = GetGridPosition(grid, blockWidth, blockHeight);
        float xoffset = GetOffsetX(grid, blockTiles);
        float yoffset = GetOffsetY(grid, blockTiles);
        
        gameObject.transform.localPosition = new(gridPos.x + xoffset, gridPos.y + yoffset);

        sr = gameObject.GetComponent<SpriteRenderer>();
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
}