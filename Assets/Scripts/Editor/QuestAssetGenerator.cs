#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Docs/quest · questline.json 기준으로 Quest SO·QuestDatabase를 생성·갱신한다.
public static class QuestAssetGenerator
{
    private const string QuestRoot = "Assets/Quest";
    private const string QuestLinePath = "Assets/Data/Quest/questline.json";
    private const string DatabasePath = "Assets/Resources/Data/QuestDatabase.asset";
    private const string QuestScriptGuid = "b8d8b808bf67ae24fb23a7fd2b79d659";

    private static readonly string[][] ColumnGroups =
    {
        new[] { "Q001" },
        new[] { "Q002", "Q034", "Q012", "Q014", "Q013", "Q035" },
        new[] { "Q003", "Q036", "Q017", "Q015", "Q018", "Q019", "Q016" },
        new[] { "Q005", "Q025", "Q037", "Q038", "Q026", "Q039", "Q027", "Q024", "Q021", "Q040", "Q022", "Q020", "Q023" },
        new[] { "Q010", "Q028" },
        new[] { "Q006", "Q041", "Q033", "Q029", "Q030", "Q031", "Q042" },
        new[] { "Q007", "Q044", "Q043", "Q045", "Q046", "Q047", "Q048" },
        new[] { "Q011", "Q049" },
        new[] { "Q008" },
        new[] { "Q009" },
    };

    [MenuItem("DungeonFront/Generate Quest Assets From Questline")]
    public static void Generate()
    {
        TextAsset questLine = AssetDatabase.LoadAssetAtPath<TextAsset>(QuestLinePath);
        if (questLine == null)
        {
            Debug.LogError($"[QuestAssetGenerator] Missing {QuestLinePath}");
            return;
        }

        QuestLineFile source = JsonUtility.FromJson<QuestLineFile>(questLine.text);
        if (source?.quests == null || source.quests.Length == 0)
        {
            Debug.LogError("[QuestAssetGenerator] questline.json has no quests.");
            return;
        }

        Dictionary<string, ItemDefinition> items = BuildItemLookup();
        Dictionary<string, string> folderByQuest = BuildFolderMap();
        EnsureFolder(QuestRoot);
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Data");

        var created = new List<Quest>();
        foreach (QuestLineFile.QuestLineQuest lineQuest in source.quests)
        {
            if (lineQuest == null || string.IsNullOrWhiteSpace(lineQuest.id))
            {
                continue;
            }

            if (!folderByQuest.TryGetValue(lineQuest.id, out string folderName))
            {
                folderName = lineQuest.id;
            }

            string folder = $"{QuestRoot}/{folderName}";
            EnsureFolder(folder);
            string assetPath = $"{folder}/{lineQuest.id}.asset";

            Quest quest = AssetDatabase.LoadAssetAtPath<Quest>(assetPath);
            if (quest == null)
            {
                quest = ScriptableObject.CreateInstance<Quest>();
                AssetDatabase.CreateAsset(quest, assetPath);
            }

            FillQuest(quest, lineQuest, items);
            EditorUtility.SetDirty(quest);
            created.Add(quest);
        }

        QuestDatabase database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<QuestDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        SerializedObject so = new SerializedObject(database);
        SerializedProperty questsProp = so.FindProperty("quests");
        questsProp.arraySize = created.Count;
        for (int i = 0; i < created.Count; i++)
        {
            questsProp.GetArrayElementAtIndex(i).objectReferenceValue = created[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestAssetGenerator] Generated {created.Count} quests → {DatabasePath}");
    }

    private static void FillQuest(
        Quest quest,
        QuestLineFile.QuestLineQuest lineQuest,
        Dictionary<string, ItemDefinition> items)
    {
        if (!TryConvertQuestLineId(lineQuest.id, out int numericId))
        {
            numericId = 0;
        }

        quest.id = numericId > 0 ? numericId.ToString("D8") : lineQuest.id;
        quest.name = lineQuest.id;
        quest.title = lineQuest.title ?? string.Empty;
        quest.clientName = lineQuest.clientName ?? string.Empty;
        quest.content = lineQuest.content ?? string.Empty;
        quest.deadlineDays = Mathf.Max(0, lineQuest.deadlineDays);
        quest.currentleftDeadlineDays = 0;
        quest.requiredItems = MakeEntryList(
            lineQuest.id,
            lineQuest.requiredItems,
            items,
            includeFame: true);
        quest.rewards = MakeEntryList(
            lineQuest.id,
            lineQuest.reward,
            items,
            includeFame: true);
    }

    private static ItemEntryList MakeEntryList(
        string questLineId,
        QuestLineFile.QuestLineItem[] source,
        Dictionary<string, ItemDefinition> items,
        bool includeFame)
    {
        source ??= Array.Empty<QuestLineFile.QuestLineItem>();
        var entries = new List<ItemEntry>();
        var occurrence = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int index = 0; index < source.Length; index++)
        {
            QuestLineFile.QuestLineItem row = source[index];
            if (row == null || string.IsNullOrWhiteSpace(row.itemcode))
            {
                continue;
            }

            if (!includeFame
                && (string.Equals(row.itemcode, "fame", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(row.itemcode, "reputation", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            occurrence.TryGetValue(row.itemcode, out int occ);
            occurrence[row.itemcode] = occ + 1;

            QuestItemCodeResolver.ResolvedItem resolved =
                QuestItemCodeResolver.Resolve(questLineId, row.itemcode, occ);

            string lookupId = resolved.itemId;
            if (string.Equals(lookupId, "gold", StringComparison.OrdinalIgnoreCase))
            {
                lookupId = "Gold";
            }
            else if (string.Equals(lookupId, "fame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(lookupId, "reputation", StringComparison.OrdinalIgnoreCase))
            {
                lookupId = "Fame";
            }

            if (!items.TryGetValue(lookupId, out ItemDefinition definition)
                && !items.TryGetValue(resolved.itemId, out definition))
            {
                Debug.LogWarning(
                    $"[QuestAssetGenerator] Missing ItemDefinition for '{resolved.itemId}' ({questLineId})");
                continue;
            }

            Item item = Item.FromDefinition(
                definition,
                resolved.level > 0 ? resolved.level : 1);
            if (resolved.enchantments != null)
            {
                for (int e = 0; e < resolved.enchantments.Length; e++)
                {
                    item.TryAddEnchantment(resolved.enchantments[e]);
                }
            }

            entries.Add(new ItemEntry
            {
                item = item,
                count = Mathf.Max(0, row.number)
            });
        }

        return new ItemEntryList
        {
            length = entries.Count,
            entries = entries.ToArray()
        };
    }

    private static Dictionary<string, ItemDefinition> BuildItemLookup()
    {
        var map = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item == null || string.IsNullOrWhiteSpace(item.id))
            {
                continue;
            }

            map[item.id] = item;
        }

        return map;
    }

    private static Dictionary<string, string> BuildFolderMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string[] group in ColumnGroups)
        {
            string folder = group[0];
            foreach (string questId in group)
            {
                map[questId] = folder;
            }
        }

        return map;
    }

    private static bool TryConvertQuestLineId(string sourceId, out int id)
    {
        id = 0;
        return !string.IsNullOrWhiteSpace(sourceId)
            && sourceId.Length > 1
            && int.TryParse(sourceId.Substring(1), out int number)
            && (id = 100000 + number) > 0;
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

    // JsonUtility용 로컬 DTO (QuestPool 내부 타입과 동일 스키마).
    [Serializable]
    private class QuestLineFile
    {
        public QuestLineQuest[] quests;

        [Serializable]
        public class QuestLineQuest
        {
            public string id;
            public string title;
            public string clientName;
            public string content;
            public int deadlineDays;
            public QuestLineItem[] requiredItems;
            public QuestLineItem[] reward;
            public float x;
            public float y;
        }

        [Serializable]
        public class QuestLineItem
        {
            public string itemcode;
            public int number;
        }
    }
}
#endif
