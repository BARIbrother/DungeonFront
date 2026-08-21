#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 올린 기계 스프라이트를 PPU 32로 임포트하고, 프리팹·ItemDef_Machine.icon에 연결한다.
public class MachineArtBinder : AssetPostprocessor
{
    private const string ArtFolder = "Assets/Art/Machines";
    private const string PrefabFolder = "Assets/Prefabs/Machines";
    private const string MachineDefFolder = "Assets/ItemDefinition/MachineDef";

    private static readonly string[] ArtPaths =
    {
        $"{ArtFolder}/mana_storage.png",
        $"{ArtFolder}/warehouse.png",
        $"{ArtFolder}/altar.png",
        $"{ArtFolder}/foundry.png",
        $"{ArtFolder}/enchanting.png",
        $"{ArtFolder}/mana_handmade.png",
        $"{ArtFolder}/mana_extractor.png",
        $"{ArtFolder}/assembler_placeholder.png",
    };

    private static readonly Dictionary<string, string> PrefabByArtPath = new()
    {
        { $"{ArtFolder}/mana_storage.png", $"{PrefabFolder}/ManaStorage_machine.prefab" },
        { $"{ArtFolder}/warehouse.png", $"{PrefabFolder}/Warehouse_machine.prefab" },
        { $"{ArtFolder}/altar.png", $"{PrefabFolder}/Altar_machine.prefab" },
        { $"{ArtFolder}/foundry.png", $"{PrefabFolder}/Foundry_machine.prefab" },
        { $"{ArtFolder}/enchanting.png", $"{PrefabFolder}/Enchanting_machine.prefab" },
        { $"{ArtFolder}/mana_handmade.png", $"{PrefabFolder}/ManaHandmade_machine.prefab" },
        { $"{ArtFolder}/mana_extractor.png", $"{PrefabFolder}/ManaExtractor_machine.prefab" },
        { $"{ArtFolder}/assembler_placeholder.png", $"{PrefabFolder}/Assembler_machine.prefab" },
    };

    private static readonly Dictionary<string, string> DefIdByArtPath = new()
    {
        { $"{ArtFolder}/mana_storage.png", "ManaStorage_1" },
        { $"{ArtFolder}/warehouse.png", "Warehouse_1" },
        { $"{ArtFolder}/altar.png", "Altar_1" },
        { $"{ArtFolder}/foundry.png", "Foundry_1" },
        { $"{ArtFolder}/enchanting.png", "Enchanting_1" },
        { $"{ArtFolder}/mana_handmade.png", "ManaHandmade_1" },
        { $"{ArtFolder}/mana_extractor.png", "ManaExtractor_1" },
        { $"{ArtFolder}/assembler_placeholder.png", "Assembler_1" },
    };

    [InitializeOnLoadMethod]
    private static void AutoBindAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/mana_extractor.png") == null
                || AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/mana_handmade.png") == null)
            {
                return;
            }

            Bind();
        };
    }

    [MenuItem("DungeonFront/Bind Machine Art Sprites")]
    public static void Bind()
    {
        int bound = 0;
        for (int i = 0; i < ArtPaths.Length; i++)
        {
            if (BindArt(ArtPaths[i]))
            {
                bound++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[MachineArtBinder] bound={bound}/{ArtPaths.Length}");
    }

    public static bool TryApplyArt(SpriteRenderer renderer, string prefabObjectName)
    {
        string artPath = ArtPathForPrefabName(prefabObjectName);
        if (string.IsNullOrEmpty(artPath) || renderer == null)
        {
            return false;
        }

        Sprite sprite = LoadSprite(artPath);
        if (sprite == null)
        {
            return false;
        }

        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.transform.localScale = Vector3.one;
        return true;
    }

    public static Sprite LoadArtSprite(string machineDefId)
    {
        foreach (KeyValuePair<string, string> pair in DefIdByArtPath)
        {
            if (pair.Value == machineDefId)
            {
                return LoadSprite(pair.Key);
            }
        }

        return null;
    }

    private void OnPreprocessTexture()
    {
        if (!IsMachineArtPath(assetPath))
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
        bool touched = false;
        if (importedAssets != null)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (IsMachineArtPath(importedAssets[i]))
                {
                    touched = true;
                    break;
                }
            }
        }

        if (touched)
        {
            Bind();
        }
    }

    private static bool BindArt(string artPath)
    {
        ConfigureImporterAtPathIfNeeded(artPath);
        Sprite sprite = LoadSprite(artPath);
        if (sprite == null)
        {
            return false;
        }

        bool applied = false;
        if (PrefabByArtPath.TryGetValue(artPath, out string prefabPath))
        {
            applied = ApplySpriteToPrefab(prefabPath, sprite);
        }

        if (DefIdByArtPath.TryGetValue(artPath, out string defId))
        {
            applied |= ApplySpriteToMachineDef(defId, sprite);
        }

        return applied;
    }

    private static bool ApplySpriteToPrefab(string prefabPath, Sprite sprite)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing == null)
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.GetComponentInChildren<SpriteRenderer>();
            }

            if (renderer == null)
            {
                return false;
            }

            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Simple;
            root.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool ApplySpriteToMachineDef(string machineDefId, Sprite sprite)
    {
        ItemDef_Machine def = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(
            $"{MachineDefFolder}/{machineDefId}.asset");
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
        return importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || !Mathf.Approximately(importer.spritePixelsPerUnit, 32f)
            || importer.filterMode != FilterMode.Point
            || importer.mipmapEnabled
            || importer.textureCompression != TextureImporterCompression.Uncompressed;
    }

    private static void ConfigureImporter(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.texture != null)
                {
                    return sprite;
                }
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static string ArtPathForPrefabName(string prefabObjectName)
    {
        foreach (KeyValuePair<string, string> pair in PrefabByArtPath)
        {
            if (pair.Value.EndsWith($"/{prefabObjectName}.prefab"))
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static bool IsMachineArtPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        for (int i = 0; i < ArtPaths.Length; i++)
        {
            if (assetPath == ArtPaths[i])
            {
                return true;
            }
        }

        return false;
    }
}
#endif
