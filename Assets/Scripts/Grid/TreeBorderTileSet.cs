using UnityEngine;
using UnityEngine.Tilemaps;

// 32px 그리드 셀 단위로 잘린 숲 경계 스프라이트 → 런타임 Tile 캐시.
[CreateAssetMenu(fileName = "TreeBorderTileSet", menuName = "DungeonFront/Tree Border Tile Set")]
public class TreeBorderTileSet : ScriptableObject
{
    [Header("2x2 (64px fill)")]
    public Sprite[] fill2x2Sprites = new Sprite[4];

    [Header("3x4 (96x128 edge)")]
    public Sprite[] edgeLeft3x4Sprites = new Sprite[12];
    public Sprite[] edgeRight3x4Sprites = new Sprite[12];

    [Header("2x4 (64x128 fringe)")]
    public Sprite[] fringeLeft2x4Sprites = new Sprite[8];
    public Sprite[] fringeRight2x4Sprites = new Sprite[8];

    private TileBase[] fill2x2;
    private TileBase[] edgeLeft3x4;
    private TileBase[] edgeRight3x4;
    private TileBase[] fringeLeft2x4;
    private TileBase[] fringeRight2x4;

    private void OnEnable()
    {
        RebuildTiles();
    }

    public void RebuildTiles()
    {
        fill2x2 = BuildTiles(fill2x2Sprites);
        edgeLeft3x4 = BuildTiles(edgeLeft3x4Sprites);
        edgeRight3x4 = BuildTiles(edgeRight3x4Sprites);
        fringeLeft2x4 = BuildTiles(fringeLeft2x4Sprites);
        fringeRight2x4 = BuildTiles(fringeRight2x4Sprites);
    }

    public TileBase GetTile(TreeBorderTileKind kind, int localX, int localY)
    {
        if (fill2x2 == null)
        {
            RebuildTiles();
        }

        int index;
        switch (kind)
        {
            case TreeBorderTileKind.Fill:
                index = (localX % 2) + (localY % 2) * 2;
                return GetClamped(fill2x2, index);
            case TreeBorderTileKind.EdgeLeft:
                index = (localX % 3) + (localY % 4) * 3;
                return GetClamped(edgeLeft3x4, index);
            case TreeBorderTileKind.EdgeRight:
                index = (localX % 3) + (localY % 4) * 3;
                return GetClamped(edgeRight3x4, index);
            case TreeBorderTileKind.FringeLeft:
                index = (localX % 2) + (localY % 4) * 2;
                return GetClamped(fringeLeft2x4, index);
            case TreeBorderTileKind.FringeRight:
                index = (localX % 2) + (localY % 4) * 2;
                return GetClamped(fringeRight2x4, index);
            default:
                return null;
        }
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
