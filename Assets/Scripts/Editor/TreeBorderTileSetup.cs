#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 숲 경계 32px 슬라이스 PNG → TreeBorderTileSet 스프라이트 배열·씬 연결.
public static class TreeBorderTileSetup
{
    private const string ScenePath = "Assets/Scenes/ProductionScene.unity";
    private const string TileSetPath = "Assets/Data/TreeBorderTileSet.asset";
    private const string TileFolder = "Assets/Art/Background/Tiles/Tree";
    private const float Ppu = 32f;

    [MenuItem("DungeonFront/Ensure Tree Border Tiles")]
    public static void EnsureFromMenu()
    {
        Ensure();
    }

    // Unity -batchmode -executeMethod TreeBorderTileSetup.RunBatchSetup
    public static void RunBatchSetup()
    {
        Ensure();
    }

    public static void Ensure()
    {
        EnsureFolder(TileFolder);
        EnsureFolder("Assets/Data");

        // SIDE / MID / BOTTOM 슬라이스는 Tools/slice_tree_parts.py로 생성한다.
        ConfigureExistingSlices(1, 4, "side_left");
        ConfigureExistingSlices(1, 4, "side_right");
        ConfigureExistingSlices(2, 2, "mid");
        ConfigureExistingSlices(16, 4, "bottom");

        TreeBorderTileSet set = AssetDatabase.LoadAssetAtPath<TreeBorderTileSet>(TileSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<TreeBorderTileSet>();
            AssetDatabase.CreateAsset(set, TileSetPath);
        }

        set.mid2x2Sprites = LoadSliceSprites(2, 2, "mid");
        set.sideLeft1x4Sprites = LoadSliceSprites(1, 4, "side_left");
        set.sideRight1x4Sprites = LoadSliceSprites(1, 4, "side_right");
        set.bottom16x4Sprites = LoadSliceSprites(16, 4, "bottom");
        set.RebuildTiles();
        EditorUtility.SetDirty(set);

        WireScene(set);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TreeBorderTileSetup] TreeBorderTileSet 생성·씬 연결 완료");
    }

    private static void ConfigureExistingSlices(int cols, int rows, string prefix)
    {
        for (int ty = 0; ty < rows; ty++)
        {
            for (int tx = 0; tx < cols; tx++)
            {
                string pngPath = $"{TileFolder}/{prefix}_{tx}_{ty}.png";
                if (AssetDatabase.LoadAssetAtPath<Sprite>(pngPath) == null)
                {
                    Debug.LogWarning($"[TreeBorderTileSetup] 슬라이스 없음: {pngPath} (Tools/slice_tree_parts.py 실행)");
                    continue;
                }

                ConfigureSliceImporter(pngPath);
            }
        }
    }

    private static Sprite[] LoadSliceSprites(int cols, int rows, string prefix)
    {
        var sprites = new Sprite[cols * rows];
        for (int ty = 0; ty < rows; ty++)
        {
            for (int tx = 0; tx < cols; tx++)
            {
                string pngPath = $"{TileFolder}/{prefix}_{tx}_{ty}.png";
                sprites[tx + ty * cols] = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            }
        }

        return sprites;
    }

    private static void EnsureSliceSprites(string texturePath, int cols, int rows, string prefix)
    {
        bool anyMissing = false;
        for (int ty = 0; ty < rows && !anyMissing; ty++)
        {
            for (int tx = 0; tx < cols; tx++)
            {
                string pngPath = $"{TileFolder}/{prefix}_{tx}_{ty}.png";
                if (AssetDatabase.LoadAssetAtPath<Sprite>(pngPath) == null)
                {
                    anyMissing = true;
                    break;
                }
            }
        }

        if (!anyMissing)
        {
            for (int ty = 0; ty < rows; ty++)
            {
                for (int tx = 0; tx < cols; tx++)
                {
                    ConfigureSliceImporter($"{TileFolder}/{prefix}_{tx}_{ty}.png");
                }
            }

            return;
        }

        EnsureReadable(texturePath);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            Debug.LogError($"[TreeBorderTileSetup] 텍스처 없음: {texturePath}");
            return;
        }

        for (int ty = 0; ty < rows; ty++)
        {
            for (int tx = 0; tx < cols; tx++)
            {
                int px = tx * 32;
                int py = texture.height - (ty + 1) * 32;
                SaveSliceSprite(texture, prefix, tx, ty, px, py);
            }
        }
    }

    private static void SaveSliceSprite(Texture2D texture, string prefix, int tx, int ty, int px, int py)
    {
        Color[] pixels = texture.GetPixels(px, py, 32, 32);
        var sliceTex = new Texture2D(32, 32, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        sliceTex.SetPixels(pixels);
        sliceTex.Apply();

        string pngPath = $"{TileFolder}/{prefix}_{tx}_{ty}.png";
        File.WriteAllBytes(pngPath, sliceTex.EncodeToPNG());
        Object.DestroyImmediate(sliceTex);

        AssetDatabase.ImportAsset(pngPath);
        ConfigureSliceImporter(pngPath);
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

    private static void EnsureReadable(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null || importer.isReadable)
        {
            return;
        }

        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    private static void WireScene(TreeBorderTileSet set)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GridTilemapRenderer renderer = Object.FindAnyObjectByType<GridTilemapRenderer>();
        if (renderer == null)
        {
            Debug.LogError("[TreeBorderTileSetup] GridTilemapRenderer를 찾을 수 없습니다.");
            return;
        }

        SerializedObject so = new SerializedObject(renderer);
        so.FindProperty("treeBorderTileSet").objectReferenceValue = set;

        Sprite floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Background/floor_grass_32.png");
        if (floorSprite != null)
        {
            so.FindProperty("floorSprite").objectReferenceValue = floorSprite;
            ConfigureSliceImporter("Assets/Art/Background/floor_grass_32.png");
        }

        string[] decoGuids = AssetDatabase.FindAssets(
            "floor_deco_ t:Texture2D",
            new[] { "Assets/Art/Background/Tiles/Floor" });
        if (decoGuids != null && decoGuids.Length > 0)
        {
            SerializedProperty decoProp = so.FindProperty("floorDecorationSprites");
            decoProp.arraySize = decoGuids.Length;
            for (int i = 0; i < decoGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(decoGuids[i]);
                ConfigureSliceImporter(path);
                Sprite deco = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                decoProp.GetArrayElementAtIndex(i).objectReferenceValue = deco;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
