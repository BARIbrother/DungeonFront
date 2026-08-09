#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Production 씬에 광석 전용 Tilemap·타일 에셋·GridResourceTilemapRenderer를 연결한다.
public static class GridResourceTilemapSetup
{
    private const string ScenePath = "Assets/Scenes/ProductionScene.unity";
    private const string OreSpritePath = "Assets/Art/ResourceNodes/iron_ore_placeholder.png";
    private const string OreTilePath = "Assets/Art/ResourceNodeTile_IronOre.asset";

    [MenuItem("DungeonFront/Ensure Resource Node Tilemap")]
    public static void EnsureFromMenu()
    {
        Ensure();
    }

    public static void Ensure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Grid grid = Object.FindAnyObjectByType<Grid>();
        GridManager gridManager = Object.FindAnyObjectByType<GridManager>();
        if (grid == null || gridManager == null)
        {
            Debug.LogError("[GridResourceTilemapSetup] Grid 또는 GridManager를 찾을 수 없습니다.");
            return;
        }

        Tilemap resourceTilemap = EnsureResourceTilemap(grid);
        Tile ironOreTile = EnsureIronOreTile();
        GridResourceTilemapRenderer renderer = EnsureRenderer(gridManager);
        WireRenderer(renderer, gridManager, resourceTilemap, ironOreTile);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[GridResourceTilemapSetup] Tilemap_resources + iron_ore 타일 연결 완료");
    }

    private static Tilemap EnsureResourceTilemap(Grid grid)
    {
        Transform existing = grid.transform.Find("Tilemap_resources");
        if (existing != null)
        {
            Tilemap tilemap = existing.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                return tilemap;
            }
        }

        GameObject tilemapObject = new GameObject("Tilemap_resources");
        Undo.RegisterCreatedObjectUndo(tilemapObject, "Create Tilemap_resources");
        tilemapObject.transform.SetParent(grid.transform, false);

        Tilemap tilemapComponent = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 1;

        Tilemap bg = grid.GetComponentInChildren<Tilemap>();
        if (bg != null && bg.name == "Tilemap_bg")
        {
            TilemapRenderer bgRenderer = bg.GetComponent<TilemapRenderer>();
            if (bgRenderer != null)
            {
                renderer.sortingLayerID = bgRenderer.sortingLayerID;
            }
        }

        return tilemapComponent;
    }

    private static Tile EnsureIronOreTile()
    {
        Tile existing = AssetDatabase.LoadAssetAtPath<Tile>(OreTilePath);
        if (existing != null && existing.sprite != null)
        {
            return existing;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OreSpritePath);
        if (sprite == null)
        {
            Debug.LogError($"[GridResourceTilemapSetup] 스프라이트 없음: {OreSpritePath}");
            return null;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;
        AssetDatabase.CreateAsset(tile, OreTilePath);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static GridResourceTilemapRenderer EnsureRenderer(GridManager gridManager)
    {
        GridResourceTilemapRenderer renderer = gridManager.GetComponent<GridResourceTilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<GridResourceTilemapRenderer>(gridManager.gameObject);
        }

        return renderer;
    }

    private static void WireRenderer(
        GridResourceTilemapRenderer renderer,
        GridManager gridManager,
        Tilemap resourceTilemap,
        Tile ironOreTile)
    {
        SerializedObject so = new SerializedObject(renderer);
        so.FindProperty("gridManager").objectReferenceValue = gridManager;
        so.FindProperty("resourceTilemap").objectReferenceValue = resourceTilemap;

        SerializedProperty entries = so.FindProperty("resourceTiles");
        entries.arraySize = 1;
        SerializedProperty entry = entries.GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("ItemId").stringValue = "iron_ore";
        entry.FindPropertyRelative("Tile").objectReferenceValue = ironOreTile;

        Sprite oreSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OreSpritePath);
        so.FindProperty("ironOreFallbackSprite").objectReferenceValue = oreSprite;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
