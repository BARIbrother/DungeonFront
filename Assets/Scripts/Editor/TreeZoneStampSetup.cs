#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ZoneTemplates/stamp_XX.keys.txt → TreeZoneStampSet 스프라이트 배열·씬 연결.
public static class TreeZoneStampSetup
{
    private const string ScenePath = "Assets/Scenes/ProductionScene.unity";
    private const string StampSetPath = "Assets/Data/TreeZoneStampSet.asset";
    private const string TemplateFolder = "Assets/Art/Background/Tiles/Tree/ZoneTemplates";
    private const string TileFolder = "Assets/Art/Background/Tiles/Tree";
    private const float Ppu = 32f;

    [MenuItem("DungeonFront/Ensure Tree Zone Stamps")]
    public static void EnsureFromMenu()
    {
        Ensure();
    }

    public static void RunBatchSetup()
    {
        Ensure();
    }

    public static void Ensure()
    {
        ConfigureTileFolderSprites();

        TreeZoneStampSet set = AssetDatabase.LoadAssetAtPath<TreeZoneStampSet>(StampSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<TreeZoneStampSet>();
            AssetDatabase.CreateAsset(set, StampSetPath);
        }

        int total = TreeZoneStampSet.MaskCount * TreeZoneStampSet.CellsPerStamp;
        if (set.stampSprites == null || set.stampSprites.Length != total)
        {
            set.stampSprites = new Sprite[total];
        }

        int missing = 0;
        for (int mask = 0; mask < TreeZoneStampSet.MaskCount; mask++)
        {
            string keyPath = $"{TemplateFolder}/stamp_{mask:00}.keys.txt";
            if (!File.Exists(keyPath))
            {
                Debug.LogWarning($"[TreeZoneStampSetup] 키 파일 없음: {keyPath} (Tools/build_zone_stamp.py 실행)");
                missing++;
                continue;
            }

            string[] lines = File.ReadAllLines(keyPath);
            for (int ly = 0; ly < TreeZoneStampSet.ZoneSize; ly++)
            {
                string[] parts = ly < lines.Length
                    ? lines[ly].Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                    : System.Array.Empty<string>();
                for (int lx = 0; lx < TreeZoneStampSet.ZoneSize; lx++)
                {
                    int index = mask * TreeZoneStampSet.CellsPerStamp + ly * TreeZoneStampSet.ZoneSize + lx;
                    string key = lx < parts.Length ? parts[lx].Trim() : "mid_0_0";
                    string pngPath = $"{TileFolder}/{key}.png";
                    ConfigureSliceImporter(pngPath);
                    set.stampSprites[index] = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                    if (set.stampSprites[index] == null)
                    {
                        missing++;
                    }
                }
            }
        }

        set.RebuildTiles();
        EditorUtility.SetDirty(set);
        WireScene(set);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TreeZoneStampSetup] TreeZoneStampSet 연결 완료 (missing refs≈{missing})");
    }

    private static void ConfigureTileFolderSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TileFolder });
        if (guids == null)
        {
            return;
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".png"))
            {
                ConfigureSliceImporter(path);
            }
        }
    }

    private static void ConfigureSliceImporter(string pngPath)
    {
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void WireScene(TreeZoneStampSet set)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GridTilemapRenderer renderer = Object.FindAnyObjectByType<GridTilemapRenderer>();
        if (renderer == null)
        {
            Debug.LogError("[TreeZoneStampSetup] GridTilemapRenderer를 찾을 수 없습니다.");
            return;
        }

        SerializedObject so = new SerializedObject(renderer);
        so.FindProperty("treeZoneStampSet").objectReferenceValue = set;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
