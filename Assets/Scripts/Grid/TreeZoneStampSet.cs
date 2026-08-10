using UnityEngine;
using UnityEngine.Tilemaps;

// 구역(16x16) 단위 숲 스탬프. mask = N|S|E|W (Floor/맵밖 이웃).
[CreateAssetMenu(fileName = "TreeZoneStampSet", menuName = "DungeonFront/Tree Zone Stamp Set")]
public class TreeZoneStampSet : ScriptableObject
{
    public const int ZoneSize = 16;
    public const int MaskCount = 16;
    public const int CellsPerStamp = ZoneSize * ZoneSize;

    public const int MaskN = 1;
    public const int MaskS = 2;
    public const int MaskE = 4;
    public const int MaskW = 8;

    [Tooltip("mask(0..15)별 16x16 스프라이트. 인덱스 = mask * 256 + localY * 16 + localX")]
    public Sprite[] stampSprites = new Sprite[MaskCount * CellsPerStamp];

    private TileBase[] stampTiles;

    private void OnEnable()
    {
        RebuildTiles();
    }

    public void RebuildTiles()
    {
        if (stampSprites == null || stampSprites.Length == 0)
        {
            stampTiles = null;
            return;
        }

        stampTiles = new TileBase[stampSprites.Length];
        for (int i = 0; i < stampSprites.Length; i++)
        {
            Sprite sprite = stampSprites[i];
            if (sprite == null)
            {
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            stampTiles[i] = tile;
        }
    }

    public TileBase GetTile(int mask, int localX, int localY)
    {
        if (stampTiles == null)
        {
            RebuildTiles();
        }

        if (stampTiles == null || stampTiles.Length == 0)
        {
            return null;
        }

        mask &= 0xF;
        localX = ((localX % ZoneSize) + ZoneSize) % ZoneSize;
        localY = ((localY % ZoneSize) + ZoneSize) % ZoneSize;
        int index = mask * CellsPerStamp + localY * ZoneSize + localX;
        if (index < 0 || index >= stampTiles.Length)
        {
            return stampTiles[0];
        }

        return stampTiles[index];
    }

    public static int BuildMask(bool floorNorth, bool floorSouth, bool floorEast, bool floorWest)
    {
        int mask = 0;
        if (floorNorth)
        {
            mask |= MaskN;
        }

        if (floorSouth)
        {
            mask |= MaskS;
        }

        if (floorEast)
        {
            mask |= MaskE;
        }

        if (floorWest)
        {
            mask |= MaskW;
        }

        return mask;
    }
}
