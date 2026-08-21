#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// item_icon_map / Art/Items/{id}_icon.png 를 ItemDefinition.icon에 연결한다.
public static class ItemIconBinder
{
    private const string ArtFolder = "Assets/Art/Items";
    private const string MapPath = "Assets/Art/Items/item_icon_map.txt";
    private const string ResourcesFolder = "Assets/Resources/ItemIcons";

    // itemId 별칭 → 아이콘 stem (확장자 제외). ItemIconResolver와 동기.
    private static readonly Dictionary<string, string> IdAliases = new()
    {
        { "iron", "iron_ingot_icon" },
        { "iron_bar", "iron_ingot_icon" },
        { "iron_ingot", "iron_ingot_icon" },
        { "war_stained_executor_greatsword", "war_stained_executioner_greatsword_icon" },
    };

    [MenuItem("DungeonFront/Bind Completed Item Icons")]
    public static void Bind()
    {
        Dictionary<string, string> idToStem = LoadMap();
        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourcesFolder);

        int bound = 0;
        int missingArt = 0;
        int skipped = 0;

        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/ItemDefinition" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null || string.IsNullOrEmpty(item.id))
            {
                continue;
            }

            if (item is ItemDef_Machine machine)
            {
                if (BindMachine(machine))
                {
                    bound++;
                }
                else
                {
                    skipped++;
                }

                continue;
            }

            if (!TryResolveStem(item.id, idToStem, out string stem))
            {
                skipped++;
                continue;
            }

            string artPath = $"{ArtFolder}/{stem}.png";
            Sprite sprite = LoadSprite(artPath);
            if (sprite == null)
            {
                missingArt++;
                Debug.LogWarning($"[ItemIconBinder] 아트 없음: {item.id} → {artPath}");
                continue;
            }

            ConfigurePixelIconImporter(artPath);
            item.icon = sprite;
            EditorUtility.SetDirty(item);

            string resourcesPath = $"{ResourcesFolder}/{stem}.png";
            if (!File.Exists(ToAbsolute(resourcesPath)))
            {
                AssetDatabase.CopyAsset(artPath, resourcesPath);
            }

            ConfigurePixelIconImporter(resourcesPath);
            bound++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[ItemIconBinder] bound={bound}, missingArt={missingArt}, noMapEntry={skipped}");
    }

    private static Dictionary<string, string> LoadMap()
    {
        var map = new Dictionary<string, string>();
        TextAsset mapAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath);
        if (mapAsset == null)
        {
            Debug.LogWarning($"[ItemIconBinder] 매칭표 없음: {MapPath}");
            return map;
        }

        string[] lines = mapAsset.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                continue;
            }

            string[] parts = line.Split('\t');
            if (parts.Length < 3)
            {
                continue;
            }

            string itemId = parts[1].Trim();
            string fileName = parts[2].Trim();
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            string stem = Path.GetFileNameWithoutExtension(fileName);
            map[itemId] = stem;
        }

        foreach (KeyValuePair<string, string> pair in IdAliases)
        {
            map[pair.Key] = pair.Value;
        }

        return map;
    }

    private static bool TryResolveStem(
        string itemId,
        Dictionary<string, string> idToStem,
        out string stem)
    {
        if (idToStem.TryGetValue(itemId, out stem))
        {
            return true;
        }

        if (IdAliases.TryGetValue(itemId, out stem))
        {
            return true;
        }

        stem = null;
        return false;
    }

    // 기계는 Art/Items 맵이 아니라 테크 트리 아이콘·프리팹 스프라이트를 SO.icon에 붙인다.
    private static bool BindMachine(ItemDef_Machine machine)
    {
        Sprite sprite = MachineArtBinder.LoadArtSprite(machine.id);
        if (sprite == null)
        {
            sprite = LoadEditorTechIcon(machine.id);
        }

        if (sprite == null)
        {
            sprite = GetPrefabSprite(machine.machinePrefab);
        }

        if (sprite == null || sprite.texture == null)
        {
            return false;
        }

        machine.icon = sprite;
        EditorUtility.SetDirty(machine);
        return true;
    }

    private static Sprite LoadEditorTechIcon(string machineDefId)
    {
        string techId = MachineIconResolver.ResolveTechIconId(machineDefId);
        if (string.IsNullOrEmpty(techId))
        {
            return null;
        }

        return LoadSprite($"Assets/Resources/UI/TechTree/{techId}.png");
    }

    private static Sprite GetPrefabSprite(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = prefab.GetComponentInChildren<SpriteRenderer>();
        }

        return renderer != null ? renderer.sprite : null;
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

    private static void ConfigurePixelIconImporter(string assetPath)
    {
        if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
        {
            return;
        }

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }

        if (dirty)
        {
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string name = Path.GetFileName(assetPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }

    private static string ToAbsolute(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot ?? string.Empty, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
#endif
