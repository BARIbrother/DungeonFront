using UnityEngine;
using UnityEngine.Tilemaps;

// 32px 그리드: 밑(BOTTOM)·옆(SIDE)·가운데(MID) 스프라이트 → 런타임 Tile 캐시.
[CreateAssetMenu(fileName = "TreeBorderTileSet", menuName = "DungeonFront/Tree Border Tile Set")]
public class TreeBorderTileSet : ScriptableObject
{
    [Header("2x2 MID (fill)")]
    public Sprite[] mid2x2Sprites = new Sprite[4];

    [Header("1x4 SIDE")]
    public Sprite[] sideLeft1x4Sprites = new Sprite[4];
    public Sprite[] sideRight1x4Sprites = new Sprite[4];

    [Header("16x4 BOTTOM (floor grad + 7 trees)")]
    public Sprite[] bottom16x4Sprites = new Sprite[64];

    private TileBase[] mid2x2;
    private TileBase[] sideLeft1x4;
    private TileBase[] sideRight1x4;
    private TileBase[] bottom16x4;

    private void OnEnable()
    {
        RebuildTiles();
    }

    public void RebuildTiles()
    {
        mid2x2 = BuildTiles(mid2x2Sprites);
        sideLeft1x4 = BuildTiles(sideLeft1x4Sprites);
        sideRight1x4 = BuildTiles(sideRight1x4Sprites);
        bottom16x4 = BuildTiles(bottom16x4Sprites);
    }

    public TileBase GetTile(TreeBorderTileKind kind, int localX, int localY)
    {
        if (mid2x2 == null)
        {
            RebuildTiles();
        }

        switch (kind)
        {
            case TreeBorderTileKind.Mid:
            case TreeBorderTileKind.EdgeTop:
                return GetFromGrid(mid2x2, 2, 2, localX, localY);

            case TreeBorderTileKind.SideLeft:
            case TreeBorderTileKind.CornerTopLeft:
                return GetFromGrid(sideLeft1x4, 1, 4, localX, localY);

            case TreeBorderTileKind.SideRight:
            case TreeBorderTileKind.CornerTopRight:
                return GetFromGrid(sideRight1x4, 1, 4, localX, localY);

            case TreeBorderTileKind.Bottom:
                return GetFromGrid(bottom16x4, 16, 4, localX, localY);

            default:
                return null;
        }
    }

    private static TileBase GetFromGrid(TileBase[] tiles, int cols, int rows, int localX, int localY)
    {
        if (tiles == null || tiles.Length == 0)
        {
            return null;
        }

        int index = (localX % cols) + (localY % rows) * cols;
        return GetClamped(tiles, index);
    }

    private static TileBase[] BuildTiles(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        var tiles = new TileBase[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tiles[i] = tile;
        }

        return tiles;
    }

    private static TileBase GetClamped(TileBase[] tiles, int index)
    {
        if (tiles == null || tiles.Length == 0)
        {
            return null;
        }

        if (index < 0 || index >= tiles.Length)
        {
            return tiles[0];
        }

        return tiles[index];
    }
}
