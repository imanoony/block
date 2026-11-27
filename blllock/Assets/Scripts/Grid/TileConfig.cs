using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "TileConfig",
    menuName = "Game/Tile Config"
)]
public class TileConfig : ScriptableObject
{
    public TileBase[] bgTiles;
    public TileBase[] circuitTiles;

    public int bgResourceWidth;
    public int bgResourceHeight;

    public int circuitResourceWidth;
    public int circuitResourceHeight;
}