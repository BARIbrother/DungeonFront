using System.Collections.Generic;
using UnityEngine;

public class QuestPool : MonoBehaviour
{
    [System.Serializable]
    public class ItemEntryName
    {
        public string itemId;
        public int count;
    }

    [System.Serializable]
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
    }

    [System.Serializable]
    public class ItemMapping
    {
        public string itemId;
        public ItemDefinition item;
    }

    [SerializeField] private TextAsset questJson;
    [SerializeField] private ItemMapping[] dict;
    [SerializeField] private QuestManager questManager;

    public List<Questplus> allQuests = new();

    private Dictionary<string, ItemDefinition> itemById = new();

    //대충 json으로 만들어주기
    private void Awake()
    {
        
        BuildItemDictionary();
        LoadQuestJson();
    }

    private void BuildItemDictionary()
    {
        itemById.Clear();

        foreach (ItemMapping mapping in dict)
        {
            if (mapping == null || string.IsNullOrEmpty(mapping.itemId) || mapping.item == null)
            {
                continue;
            }

            itemById[mapping.itemId] = mapping.item;
        }
    }

    private void LoadQuestJson()
    {
        allQuests.Clear();

        QuestplusList wrapper = JsonUtility.FromJson<QuestplusList>(questJson.text);

        if (wrapper == null || wrapper.quests == null)
        {
            return;
        }

        allQuests.AddRange(wrapper.quests);
    }

    [System.Serializable]
    private class QuestplusList
    {
        public Questplus[] quests;
    }

    private Quest Plus2Quest(Questplus questplus)
    {
        Quest quest = ScriptableObject.CreateInstance<Quest>();

        quest.title = questplus.title;
        quest.clientName = questplus.clientName;
        quest.content = questplus.content;
        quest.deadlineDays = questplus.deadlineDays;
        quest.currentleftDeadlineDays = questplus.deadlineDays;
        quest.requiredItems = MakeItemEntryList(questplus.requiredItems);
        quest.rewards = MakeItemEntryList(questplus.rewards);

        return quest;
    }

    //대충 제대로 이어주기 위해 있는
    private ItemEntryList MakeItemEntryList(ItemEntryName[] source)
    {
        ItemEntryList result = new ItemEntryList();

        if (source == null)
        {
            result.length = 0;
            result.entries = new ItemEntry[0];
            return result;
        }

        result.length = source.Length;
        result.entries = new ItemEntry[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            ItemEntryName entry = source[i];

            if (entry == null || string.IsNullOrEmpty(entry.itemId))
            {
                continue;
            }

            if (!itemById.TryGetValue(entry.itemId, out ItemDefinition item))
            {
                Debug.LogWarning($"Item id not found: {entry.itemId}");
                continue;
            }

            result.entries[i] = new ItemEntry
            {
                item = item,
                count = entry.count
            };
        }

        return result;
    }

    public void MakeAvailableQuestsToday(int reputation)
    {
        questManager.availableQuestsToday.Clear();

        Debug.Log("Reputation : " + reputation);
        Debug.Log("Quest Count : " + allQuests.Count);

        foreach (Questplus questplus in allQuests)
        {
            Debug.Log("Check Quest : " + questplus.id);

            if (questplus.threshold > reputation)
            {
                Debug.Log("Skip : threshold");
                continue;
            }

            if (questManager.acceptedQuestIds.Contains(questplus.id))
            {
                Debug.Log("Skip : accepted");
                continue;
            }

            Quest quest = Plus2Quest(questplus);

            questManager.availableQuestsToday.Add(quest);

            Debug.Log("Add : " + questplus.id);
        }

        Debug.Log("Available Count : " + questManager.availableQuestsToday.Count);
    }
}