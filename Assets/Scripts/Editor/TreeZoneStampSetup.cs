#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// stamp_XX.png를 16×16 슬라이스로 잘라 TreeZoneStampSet에 연결한다.
public static class TreeZoneStampSetup
{
    private const string ScenePath = "Assets/Scenes/ProductionScene.unity";
    private const string StampSetPath = "Assets/Data/TreeZoneStampSet.asset";
    private const string TemplateFolder = "Assets/Art/Background/Tiles/Tree/ZoneTemplates";
    private const string TileFolder = "Assets/Art/Background/Tiles/Tree";
    private const float Ppu = 32f;
    private const int StampCellPixels = 32;

    private const string LockedZonePath = "Assets/Art/Background/Tiles/Tree/ZoneTemplates/locked_zone.png";

    [InitializeOnLoadMethod]
    private static void ConfigureLockedZoneSprite()
    {
        EditorApplication.delayCall += () =>
        {
            var importer = AssetImporter.GetAtPath(LockedZonePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && Mathf.Approximately(importer.spritePixelsPerUnit, Ppu)
                && importer.filterMode == FilterMode.Point)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        };
    }

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
        BindStampSheets(wireScene: true);
    }

    // stamp_XX.png를 16×16칸으로 잘라 TreeZoneStampSet에 연결한다.
    public static void BindStampSheets(bool wireScene)
    {
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
            string stampPath = $"{TemplateFolder}/stamp_{mask:00}.png";
            ConfigureStampSheetImporter(stampPath);
            if (!TryAssignStampSheet(set, mask, stampPath))
            {
                missing += TreeZoneStampSet.CellsPerStamp;
            }
        }

        set.RebuildTiles();
        EditorUtility.SetDirty(set);
        if (wireScene)
        {
            WireScene(set);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TreeZoneStampSetup] TreeZoneStampSet 연결 완료 (missing cells≈{missing})");
    }

    private static bool TryAssignStampSheet(TreeZoneStampSet set, int mask, string stampPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(stampPath);
        if (assets == null || assets.Length == 0)
        {
            return false;
        }

        int assigned = 0;
        for (int ly = 0; ly < TreeZoneStampSet.ZoneSize; ly++)
        {
            for (int lx = 0; lx < TreeZoneStampSet.ZoneSize; lx++)
            {
                int index = mask * TreeZoneStampSet.CellsPerStamp + ly * TreeZoneStampSet.ZoneSize + lx;
                string sliceName = StampSliceName(mask, lx, ly);
                Sprite sprite = FindSprite(assets, sliceName);
                set.stampSprites[index] = sprite;
                if (sprite != null)
                {
                    assigned++;
                }
            }
        }

        return assigned > 0;
    }

    private static Sprite FindSprite(Object[] assets, string sliceName)
    {
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == sliceName)
            {
                return sprite;
            }
        }

        return null;
    }

    private static string StampSliceName(int mask, int localX, int localY)
    {
        return $"stamp_{mask:00}_{localX}_{localY}";
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
            if (!path.EndsWith(".png") || path.Replace('\\', '/').Contains("/ZoneTemplates/"))
            {
                continue;
            }

            ConfigureSliceImporter(path);
        }
    }

    private static void ConfigureStampSheetImporter(string pngPath)
    {
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        SpriteRect[] sheet = BuildStampSpriteSheet(pngPath, importer);
        if (!NeedsStampConfigure(importer, sheet))
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Ppu;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        SpriteSheetImporterUtil.SetSpriteRects(importer, sheet);
        importer.SaveAndReimport();
    }

    private static bool NeedsStampConfigure(TextureImporter importer, SpriteRect[] sheet)
    {
        if (importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Multiple
            || !Mathf.Approximately(importer.spritePixelsPerUnit, Ppu)
            || importer.filterMode != FilterMode.Point)
        {
            return true;
        }

        SpriteRect[] current = SpriteSheetImporterUtil.GetSpriteRects(importer);
        if (current.Length != sheet.Length)
        {
            return true;
        }

        for (int i = 0; i < sheet.Length; i++)
        {
            if (current[i] == null || current[i].name != sheet[i].name)
            {
                return true;
            }
        }

        return false;
    }

    private static SpriteRect[] BuildStampSpriteSheet(string pngPath, TextureImporter importer)
    {
        int mask = 0;
        string fileName = Path.GetFileNameWithoutExtension(pngPath);
        if (fileName.StartsWith("stamp_") && int.TryParse(fileName.Substring("stamp_".Length), out int parsed))
        {
            mask = parsed;
        }

        int zone = TreeZoneStampSet.ZoneSize;
        SpriteRect[] existing = SpriteSheetImporterUtil.GetSpriteRects(importer);
        var rects = new SpriteRect[zone * zone];
        int i = 0;
        for (int ly = 0; ly < zone; ly++)
        {
            for (int lx = 0; lx < zone; lx++)
            {
                string sliceName = StampSliceName(mask, lx, ly);
                rects[i++] = SpriteSheetImporterUtil.CreateRect(
                    sliceName,
                    new Rect(
                        lx * StampCellPixels,
                        ly * StampCellPixels,
                        StampCellPixels,
                        StampCellPixels),
                    new Vector2(0.5f, 0.5f),
                    SpriteSheetImporterUtil.FindExistingId(existing, sliceName));
            }
        }

        return rects;
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
