#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// extractor_side/down/up.png를 추출기 프리팹·정의에 연결한다.
public class ExtractorArtBinder : AssetPostprocessor
{
    private const string DatabasePath = "Assets/ItemDefinition/MachineDef/MachineDatabase.asset";

    [InitializeOnLoadMethod]
    private static void AutoBindAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (LoadSprite(ExtractorArt.SidePath) == null)
            {
                return;
            }

            Bind();
        };
    }

    [MenuItem("DungeonFront/Bind Extractor Art Sprites")]
    public static void Bind()
    {
        for (int i = 0; i < ExtractorArt.ArtPaths.Length; i++)
        {
            ConfigureImporterAtPathIfNeeded(ExtractorArt.ArtPaths[i]);
        }

        Sprite side = LoadSprite(ExtractorArt.SidePath);
        Sprite down = LoadSprite(ExtractorArt.DownPath);
        Sprite up = LoadSprite(ExtractorArt.UpPath);
        if (side == null)
        {
            return;
        }

        bool assignedPrefab = ApplyToPrefab(side, down, up);
        bool assignedDef = ApplyToMachineDef(side);
        bool assignedDatabase = EnsureDatabaseEntry();

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[ExtractorArtBinder] side={side != null} down={down != null} up={up != null} " +
            $"prefab={assignedPrefab} def={assignedDef} db={assignedDatabase}");
    }

    private void OnPreprocessTexture()
    {
        if (!IsExtractorArtPath(assetPath))
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
            if (IsExtractorArtPath(importedAssets[i]))
            {
                Bind();
                return;
            }
        }
    }

    private static bool ApplyToPrefab(Sprite side, Sprite down, Sprite up)
    {
        EnsurePrefabExists();
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ExtractorArt.PrefabPath);
        if (existing == null)
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(ExtractorArt.PrefabPath);
        try
        {
            root.name = "Extractor_machine";

            ConveyerBeltItemView itemView = root.GetComponent<ConveyerBeltItemView>();
            if (itemView != null)
            {
                Object.DestroyImmediate(itemView);
            }

            ConveyerBelt belt = root.GetComponent<ConveyerBelt>();
            if (belt != null)
            {
                Object.DestroyImmediate(belt);
            }

            Extractor extractor = root.GetComponent<Extractor>();
            if (extractor == null)
            {
                extractor = root.AddComponent<Extractor>();
            }

            extractor.SetDirectionalSprites(new[]
            {
                new Extractor.DirectionalSprite { id = "side", sprite = side },
                new Extractor.DirectionalSprite { id = "down", sprite = down },
                new Extractor.DirectionalSprite { id = "up", sprite = up },
            });

            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = root.GetComponentInChildren<SpriteRenderer>();
            }

            if (renderer != null && side != null)
            {
                renderer.sprite = side;
                renderer.color = Color.white;
                renderer.flipX = false;
                renderer.flipY = false;
                renderer.drawMode = SpriteDrawMode.Simple;
                root.transform.localScale = Vector3.one;
                root.transform.rotation = Quaternion.identity;
            }

            PrefabUtility.SaveAsPrefabAsset(root, ExtractorArt.PrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsurePrefabExists()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ExtractorArt.PrefabPath) != null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ConveyorBeltArt.PrefabPath) == null)
        {
            return;
        }

        AssetDatabase.CopyAsset(ConveyorBeltArt.PrefabPath, ExtractorArt.PrefabPath);
    }

    private static bool ApplyToMachineDef(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        ItemDef_Machine def = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(ExtractorArt.MachineDefPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ItemDef_Machine>();
            AssetDatabase.CreateAsset(def, ExtractorArt.MachineDefPath);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExtractorArt.PrefabPath);
        def.id = ExtractorArt.MachineDefId;
        def.machineTypeId = "Extractor";
        def.displayName = "추출기";
        def.requiresManualWork = false;
        def.category = ItemCategory.Material;
        def.machinePrefab = prefab;
        def.icon = sprite;
        EditorUtility.SetDirty(def);
        return true;
    }

    private static bool EnsureDatabaseEntry()
    {
        MachineDatabase database = AssetDatabase.LoadAssetAtPath<MachineDatabase>(DatabasePath);
        ItemDef_Machine def = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(ExtractorArt.MachineDefPath);
        if (database == null || def == null)
        {
            return false;
        }

        IReadOnlyList<ItemDef_Machine> current = database.All;
        var machines = new List<ItemDef_Machine>();
        bool alreadyPresent = false;
        if (current != null)
        {
            for (int i = 0; i < current.Count; i++)
            {
                ItemDef_Machine entry = current[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.id == ExtractorArt.MachineDefId)
                {
                    machines.Add(def);
                    alreadyPresent = true;
                    continue;
                }

                machines.Add(entry);
            }
        }

        if (!alreadyPresent)
        {
            machines.Add(def);
        }

        database.SetMachines(machines.ToArray());
        EditorUtility.SetDirty(database);
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

    private static bool IsExtractorArtPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        for (int i = 0; i < ExtractorArt.ArtPaths.Length; i++)
        {
            if (assetPath == ExtractorArt.ArtPaths[i])
            {
                return true;
            }
        }

        return false;
    }
}
#endif
