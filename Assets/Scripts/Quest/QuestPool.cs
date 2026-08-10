using System;
using System.Collections.Generic;
using UnityEngine;

// JSON에 적힌 퀘스트 중 "지금 목록에 보여 줄 것"을 골라낸다.
// 데이터 읽기 → 조건 검사 → Quest 런타임 객체 생성의 세 단계를 분리했다.
public class QuestPool : MonoBehaviour
{
    [Serializable]
    public class ItemEntryName
    {
        public string itemId;
        public int count;
        public int level = 1;
        public Enchantment[] enchantments;
    }

    [Serializable]
    public class Questplus
    {
        public int id;
        public int threshold;
        public string title;
        public string clientName;

        [TextArea]
        public string content;

        public ItemEntryName[] requiredItems;
        public ItemEntryName[] rewards;
        public int deadlineDays;
        public bool isMandatory;
        public int rewardReputation;

        // Week 4 필드. 기존 JSON에 없으면 각 타입의 기본값으로 읽힌다.
        public bool isPerpetual;
        public string unlockAfterQuestId;
        public bool isMainStoryQuest;
        public bool triggersBackCaveEnding;
    }

    [Serializable]
    public class ItemMapping
    {
        public string itemId;
        public ItemDefinition item;
    }

    [SerializeField] private TextAsset questJson;
    [SerializeField] private TextAsset questLineJson;
    [SerializeField] private ItemMapping[] dict = Array.Empty<ItemMapping>();
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestProgressionService progression;
    [SerializeField] private ItemManager itemManager;

    public List<Questplus> allQuests = new();

    private readonly Dictionary<string, ItemDefinition> itemById =
        new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        BuildItemDictionary();
        LoadQuestJson();
        ResolveServices();
    }

    private void ResolveServices()
    {
        questManager ??= QuestManager.Instance;
        questManager ??= FindAnyObjectByType<QuestManager>();
        progression ??= QuestProgressionService.Instance;
        progression ??= FindAnyObjectByType<QuestProgressionService>();
        itemManager ??= FindAnyObjectByType<ItemManager>();
    }

    private void BuildItemDictionary()
    {
        itemById.Clear();

        foreach (ItemMapping mapping in dict ?? Array.Empty<ItemMapping>())
        {
            if (mapping == null
                || string.IsNullOrWhiteSpace(mapping.itemId)
                || mapping.item == null)
            {
                continue;
            }

            RegisterItem(mapping.itemId, mapping.item);
            if (!string.IsNullOrWhiteSpace(mapping.item.id))
            {
                RegisterItem(mapping.item.id, mapping.item);
            }
        }

        ApplyKnownAliases();
    }

    private void RegisterItem(string itemId, ItemDefinition item)
    {
        if (string.IsNullOrWhiteSpace(itemId) || item == null)
        {
            return;
        }

        itemById[itemId] = item;
    }

    // 퀘스트 JSON·SO·아이콘 맵에서 쓰는 서로 다른 id를 같은 ItemDefinition으로 묶는다.
    private void ApplyKnownAliases()
    {
        string[][] aliasGroups =
        {
            new[] { "iron_ingot", "iron_bar", "iron" },
            new[] { "gold", "Gold" },
            new[] { "fame", "Fame" },
            new[] { "manasteel_bar", "Manasteel_ingot", "mana_core" },
            new[] { "magicrobe", "mage_robe" },
            new[] { "dark_mana_wand", "dark_magic_staff" },
            new[] { "bright_mana_wand", "light_magic_staff" },
            new[] { "darkmana_core", "dark_magic_core" },
            new[] { "greysteel_battlehammer", "greysteel_warhammer" },
            new[] { "steel_column_framwork", "iron_pillar_frame" },
            new[] { "structural_column", "structure_pillar" },
            new[] { "structural_girder", "structure_beam" },
            new[] { "structural_roof", "structure_roof" },
            new[] { "warstained_executional_greatsword", "war_stained_executor_greatsword" },
            new[] { "iron_blade", "greatsword_blade" },
        };

        for (int groupIndex = 0; groupIndex < aliasGroups.Length; groupIndex++)
        {
            string[] group = aliasGroups[groupIndex];
            ItemDefinition shared = null;
            for (int i = 0; i < group.Length; i++)
            {
                if (itemById.TryGetValue(group[i], out ItemDefinition found) && found != null)
                {
                    shared = found;
                    break;
                }
            }

            if (shared == null)
            {
                continue;
            }

            for (int i = 0; i < group.Length; i++)
            {
                RegisterItem(group[i], shared);
            }
        }
    }

    public void LoadQuestJson()
    {
        allQuests.Clear();
        bool loadedQuestLine = LoadQuestLineJson();

        // 실제 퀘스트라인이 연결된 게임에서는 Week2 예제/QA 데이터를 섞지 않는다.
        // questJson은 questline.json이 없는 독립 테스트의 fallback으로만 사용한다.
        if (!loadedQuestLine
            && questJson != null
            && !string.IsNullOrWhiteSpace(questJson.text))
        {
            QuestplusList wrapper = JsonUtility.FromJson<QuestplusList>(questJson.text);
            foreach (Questplus quest in wrapper?.quests ?? Array.Empty<Questplus>())
            {
                if (quest == null
                    || allQuests.Exists(existing => existing.id == quest.id))
                {
                    continue;
                }

                allQuests.Add(quest);
            }
        }

        if (allQuests.Count == 0)
        {
            Debug.LogWarning("QuestPool에 읽을 퀘스트 JSON이 없습니다.", this);
        }
    }

    private bool LoadQuestLineJson()
    {
        if (questLineJson == null || string.IsNullOrWhiteSpace(questLineJson.text))
        {
            return false;
        }

        QuestLineFile source =
            JsonUtility.FromJson<QuestLineFile>(questLineJson.text);
        if (source?.quests == null || source.quests.Length == 0)
        {
            return false;
        }

        var mainQuests = new List<QuestLineQuest>();
        foreach (QuestLineQuest quest in source.quests)
        {
            if (quest != null && quest.y < 320f)
            {
                mainQuests.Add(quest);
            }
        }

        mainQuests.Sort((left, right) => left.x.CompareTo(right.x));
        QuestLineQuest finalMain = mainQuests.Count > 0
            ? mainQuests[mainQuests.Count - 1]
            : null;

        foreach (QuestLineQuest sourceQuest in source.quests)
        {
            if (sourceQuest == null || !TryConvertQuestLineId(sourceQuest.id, out int id))
            {
                continue;
            }

            bool isMain = mainQuests.Contains(sourceQuest);
            bool isPerpetual = sourceQuest.deadlineDays <= 0
                || sourceQuest.title.StartsWith("상시", StringComparison.Ordinal);
            allQuests.Add(new Questplus
            {
                id = id,
                threshold = 0,
                title = sourceQuest.title,
                clientName = sourceQuest.clientName,
                content = sourceQuest.content,
                deadlineDays = Mathf.Max(0, sourceQuest.deadlineDays),
                requiredItems = ConvertQuestLineItems(
                    sourceQuest.id,
                    sourceQuest.requiredItems),
                rewards = ConvertQuestLineRewards(
                    sourceQuest.id,
                    sourceQuest.reward,
                    out int reputationReward),
                rewardReputation = reputationReward,
                isMandatory = isMain,
                isPerpetual = isPerpetual,
                unlockAfterQuestId = GetUnlockAfter(sourceQuest, mainQuests),
                isMainStoryQuest = isMain,
                triggersBackCaveEnding = sourceQuest == finalMain
            });
        }

        return true;
    }

    private static ItemEntryName[] ConvertQuestLineItems(
        string questLineId,
        QuestLineItem[] source)
    {
        source ??= Array.Empty<QuestLineItem>();
        var result = new ItemEntryName[source.Length];
        var occurrence = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < source.Length; index++)
        {
            string itemCode = source[index]?.itemcode;
            occurrence.TryGetValue(itemCode ?? string.Empty, out int occ);
            occurrence[itemCode ?? string.Empty] = occ + 1;

            QuestItemCodeResolver.ResolvedItem resolved =
                QuestItemCodeResolver.Resolve(questLineId, itemCode, occ);

            result[index] = new ItemEntryName
            {
                itemId = resolved.itemId,
                count = Mathf.Max(0, source[index]?.number ?? 0),
                level = resolved.level > 0 ? resolved.level : 1,
                enchantments = resolved.enchantments
            };
        }
        return result;
    }

    private static ItemEntryName[] ConvertQuestLineRewards(
        string questLineId,
        QuestLineItem[] source,
        out int reputationReward)
    {
        reputationReward = 0;
        var rewards = new List<ItemEntryName>();
        foreach (QuestLineItem item in source ?? Array.Empty<QuestLineItem>())
        {
            if (item == null)
            {
                continue;
            }

            if (string.Equals(item.itemcode, "fame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.itemcode, "reputation", StringComparison.OrdinalIgnoreCase))
            {
                reputationReward += Mathf.Max(0, item.number);
                continue;
            }

            QuestItemCodeResolver.ResolvedItem resolved =
                QuestItemCodeResolver.Resolve(questLineId, item.itemcode, 0);

            rewards.Add(new ItemEntryName
            {
                itemId = resolved.itemId,
                count = Mathf.Max(0, item.number),
                level = resolved.level > 0 ? resolved.level : 1,
                enchantments = resolved.enchantments
            });
        }
        return rewards.ToArray();
    }

    private static string GetUnlockAfter(
        QuestLineQuest quest,
        List<QuestLineQuest> mainQuests)
    {
        int sameColumn = mainQuests.FindIndex(main =>
            Mathf.Abs(main.x - quest.x) <= 90f);
        if (sameColumn >= 0)
        {
            return sameColumn == 0
                ? null
                : FormatQuestLineId(mainQuests[sameColumn - 1].id);
        }

        QuestLineQuest previous = null;
        foreach (QuestLineQuest main in mainQuests)
        {
            if (main.x >= quest.x)
            {
                break;
            }
            previous = main;
        }
        return previous != null ? FormatQuestLineId(previous.id) : null;
    }

    private static bool TryConvertQuestLineId(string sourceId, out int id)
    {
        id = 0;
        return !string.IsNullOrWhiteSpace(sourceId)
            && sourceId.Length > 1
            && int.TryParse(sourceId.Substring(1), out int number)
            && (id = 100000 + number) > 0;
    }

    private static string FormatQuestLineId(string sourceId)
    {
        return TryConvertQuestLineId(sourceId, out int id)
            ? FormatQuestId(id)
            : null;
    }

    // Prepare 목록을 새로 만든다. 상시 의뢰는 수락 목록에 넣지 않는다.
    public void MakeAvailableQuestsToday(int reputation)
    {
        ResolveServices();
        if (questManager == null)
        {
            Debug.LogWarning("QuestPool이 QuestManager를 찾지 못했습니다.", this);
            return;
        }

        DestroyGeneratedQuests(questManager.availableQuestsToday);
        questManager.availableQuestsToday.Clear();

        foreach (Questplus data in allQuests)
        {
            if (!CanOffer(data, reputation))
            {
                continue;
            }

            questManager.availableQuestsToday.Add(ConvertToQuest(data));
        }

        questManager.NotifyQuestsChanged();
    }

    // 상시 의뢰 UI가 사용할 목록. 수락하지 않으므로 매번 열려 있다.
    public List<Quest> CreatePerpetualQuestList(int reputation)
    {
        var result = new List<Quest>();
        foreach (Questplus data in allQuests)
        {
            if (data != null
                && data.isPerpetual
                && data.threshold <= reputation
                && (progression == null
                    || progression.CanOffer(
                        FormatQuestId(data.id),
                        data.unlockAfterQuestId)))
            {
                result.Add(ConvertToQuest(data));
            }
        }

        return result;
    }

    public Quest CreateQuestById(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        Questplus data = allQuests.Find(candidate =>
            candidate != null && FormatQuestId(candidate.id) == questId);
        return data != null ? ConvertToQuest(data) : null;
    }

    private bool CanOffer(Questplus data, int reputation)
    {
        if (data == null || data.isPerpetual || data.threshold > reputation)
        {
            return false;
        }

        string questId = FormatQuestId(data.id);
        if (questManager.currentQuests.Exists(quest =>
                quest != null
                && QuestRuntimeRegistry.GetStableId(quest) == questId))
        {
            return false;
        }

        if (progression != null)
        {
            return progression.CanOffer(questId, data.unlockAfterQuestId);
        }

        // Week 4 서비스가 없는 Week 2 테스트 씬에서는 기존 규칙을 유지한다.
        return !questManager.acceptedQuestIds.Contains(data.id);
    }

    private Quest ConvertToQuest(Questplus data)
    {
        Quest quest = ScriptableObject.CreateInstance<Quest>();
        quest.title = data.title;
        quest.clientName = data.clientName;
        quest.content = data.content;
        quest.deadlineDays = Mathf.Max(0, data.deadlineDays);
        quest.currentleftDeadlineDays = quest.deadlineDays;
        quest.requiredItems = MakeItemEntryList(data.requiredItems);
        quest.rewards = MakeItemEntryList(data.rewards);

        string questId = FormatQuestId(data.id);
        quest.id = questId;
        QuestRuntimeRegistry.Register(quest, new QuestRuntimeInfo
        {
            questId = questId,
            sourceQuestId = questId,
            minReputation = Mathf.Max(0, data.threshold),
            questKind = data.isPerpetual
                ? QuestKind.Perpetual
                : data.isMandatory
                    ? QuestKind.Story
                    : QuestKind.Standard,
            unlockAfterQuestId = data.unlockAfterQuestId,
            isMandatory = data.isMandatory,
            rewardReputation = Mathf.Max(0, data.rewardReputation),
            isMainStoryQuest = data.isMainStoryQuest,
            triggersBackCaveEnding = data.triggersBackCaveEnding
        });
        return quest;
    }

    private ItemEntryList MakeItemEntryList(ItemEntryName[] source)
    {
        source ??= Array.Empty<ItemEntryName>();
        var result = new ItemEntryList
        {
            length = source.Length,
            entries = new ItemEntry[source.Length]
        };

        for (int index = 0; index < source.Length; index++)
        {
            ItemEntryName data = source[index];
            if (data == null || string.IsNullOrWhiteSpace(data.itemId))
            {
                continue;
            }

            Item item = Item.FromDefinition(
                ResolveItem(data.itemId),
                data.level > 0 ? data.level : 1);

            if (data.enchantments != null)
            {
                for (int enchantIndex = 0; enchantIndex < data.enchantments.Length; enchantIndex++)
                {
                    item.TryAddEnchantment(data.enchantments[enchantIndex]);
                }
            }

            result.entries[index] = new ItemEntry
            {
                item = item,
                count = Mathf.Max(0, data.count)
            };
        }

        return result;
    }

    private ItemDefinition ResolveItem(string itemId)
    {
        if (itemById.TryGetValue(itemId, out ItemDefinition item))
        {
            return item;
        }

        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        if (itemManager != null)
        {
            item = itemManager.Get(itemId);
            if (item != null)
            {
                RegisterItem(itemId, item);
                return item;
            }
        }

        // 독립 테스트용 placeholder. ID 기반 인벤토리이므로 납품 로직은 동일하게 동작한다.
        item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.id = itemId;
        item.displayName = itemId;
        itemById[itemId] = item;
        Debug.LogWarning($"Item mapping이 없어 런타임 placeholder를 사용합니다: {itemId}", this);
        return item;
    }

    private static void DestroyGeneratedQuests(IEnumerable<Quest> quests)
    {
        foreach (Quest quest in quests)
        {
            if (quest != null)
            {
                QuestRuntimeRegistry.Forget(quest);
                Destroy(quest);
            }
        }
    }

    private static string FormatQuestId(int id)
    {
        return id.ToString("D8");
    }

    [Serializable]
    private class QuestplusList
    {
        public Questplus[] quests;
    }

    [Serializable]
    private class QuestLineFile
    {
        public QuestLineQuest[] quests;
    }

    [Serializable]
    private class QuestLineQuest
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
    private class QuestLineItem
    {
        public string itemcode;
        public int number;
    }
}
