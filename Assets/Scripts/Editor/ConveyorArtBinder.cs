#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// conv_belt.png 한 장을 슬라이스해 컨베이어 벨트 프리팹의 방향 텍스처로 연결한다.
public class ConveyorArtBinder : AssetPostprocessor
{
    private const string MachineDefPath = "Assets/ItemDefinition/MachineDef/ConveyerBelt_1.asset";

    [InitializeOnLoadMethod]
    private static void AutoBindAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (LoadNamedSprite(ConveyorBeltArt.DefaultSpriteId) == null)
            {
                return;
            }

            Bind();
        };
    }

    [MenuItem("DungeonFront/Bind Conveyor Art Sprites")]
    public static void Bind()
    {
        ConfigureImporterAtPathIfNeeded(ConveyorBeltArt.SheetPath);

        Sprite defaultSprite = LoadNamedSprite(ConveyorBeltArt.DefaultSpriteId);
        if (defaultSprite == null)
        {
            return;
        }

        int imported = 0;
        Sprite[] sprites = new Sprite[ConveyorBeltArt.SpriteIds.Length];
        for (int i = 0; i < ConveyorBeltArt.SpriteIds.Length; i++)
        {
            sprites[i] = LoadNamedSprite(ConveyorBeltArt.SpriteIds[i]);
            if (sprites[i] != null)
            {
                imported++;
            }
        }

        bool assignedPrefab = ApplyToPrefab(sprites, defaultSprite);
        bool assignedDef = ApplyToMachineDef(defaultSprite);

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[ConveyorArtBinder] sprites={imported}/{ConveyorBeltArt.SpriteIds.Length} " +
            $"prefab={assignedPrefab} def={assignedDef}");
    }

    private void OnPreprocessTexture()
    {
        if (assetPath != ConveyorBeltArt.SheetPath)
        {
            return;
        }

        ConfigureImporter((TextureImporter)assetImporter);
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (importedAssets == null)
        {
            return;
        }

        for (int i = 0; i < importedAssets.Length; i++)
        {
            if (importedAssets[i] == ConveyorBeltArt.SheetPath)
            {
                Bind();
                return;
            }
        }
    }

    private static bool ApplyToPrefab(Sprite[] sprites, Sprite defaultSprite)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorBeltArt.PrefabPath);
        if (existing == null)
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(ConveyorBeltArt.PrefabPath);
        try
        {
            ConveyerBelt belt = root.GetComponent<ConveyerBelt>();
            if (belt == null)
            {
                return false;
            }

            var entries = new ConveyerBelt.DirectionalSprite[ConveyorBeltArt.SpriteIds.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new ConveyerBelt.DirectionalSprite
                {
                    id = ConveyorBeltArt.SpriteIds[i],
                    sprite = sprites[i],
                };
            }

            belt.SetDirectionalSprites(entries);

            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.GetComponentInChildren<SpriteRenderer>();
            }

            if (renderer != null && defaultSprite != null)
            {
                renderer.sprite = defaultSprite;
                renderer.color = Color.white;
                renderer.drawMode = SpriteDrawMode.Simple;
                root.transform.localScale = Vector3.one;
                root.transform.rotation = Quaternion.identity;
            }

            PrefabUtility.SaveAsPrefabAsset(root, ConveyorBeltArt.PrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ApplyToMachineDef(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        ItemDef_Machine def = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(MachineDefPath);
        if (def == null)
        {
            return false;
        }

        def.icon = sprite;
        EditorUtility.SetDirty(def);
        return true;
    }

    private static void ConfigureImporterAtPathIfNeeded(string assetPath)
    {
        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
        {
            return;
        }

        if (!NeedsConfigure(importer))
        {
            return;
        }

        ConfigureImporter(importer);
        importer.SaveAndReimport();
    }

    private static bool NeedsConfigure(TextureImporter importer)
    {
        if (importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Multiple
            || !Mathf.Approximately(importer.spritePixelsPerUnit, 32f)
            || importer.filterMode != FilterMode.Point
            || importer.mipmapEnabled
            || importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            return true;
        }

        SpriteRect[] sheet = SpriteSheetImporterUtil.GetSpriteRects(importer);
        if (sheet.Length != ConveyorBeltArt.SpriteIds.Length)
        {
            return true;
        }

        for (int i = 0; i < sheet.Length; i++)
        {
            if (sheet[i] == null || sheet[i].name != ConveyorBeltArt.SpriteIds[i])
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureImporter(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        SpriteSheetImporterUtil.SetSpriteRects(importer, BuildSpriteSheet(importer));
    }

    private static SpriteRect[] BuildSpriteSheet(TextureImporter importer)
    {
        int size = ConveyorBeltArt.TileSize;
        int cols = ConveyorBeltArt.SheetColumns;
        int rows = ConveyorBeltArt.SheetRows;
        SpriteRect[] existing = SpriteSheetImporterUtil.GetSpriteRects(importer);
        var rects = new SpriteRect[ConveyorBeltArt.SpriteIds.Length];
        for (int i = 0; i < rects.Length; i++)
        {
            int col = i % cols;
            int rowFromTop = i / cols;
            int unityY = (rows - 1 - rowFromTop) * size;
            string name = ConveyorBeltArt.SpriteIds[i];
            rects[i] = SpriteSheetImporterUtil.CreateRect(
                name,
                new Rect(col * size, unityY, size, size),
                new Vector2(0.5f, 0.5f),
                SpriteSheetImporterUtil.FindExistingId(existing, name));
        }

        return rects;
    }

    private static Sprite LoadNamedSprite(string spriteId)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ConveyorBeltArt.SheetPath);
        if (assets == null)
        {
            return null;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteId && sprite.texture != null)
            {
                return sprite;
            }
        }

        return null;
    }
}
#endif
