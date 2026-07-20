using System.Collections.Generic;
using UnityEngine;

// 의뢰 수락·납품·완료를 관리한다.
public class QuestManager : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    // 현재 수락하여 진행 중인 의뢰 목록
    public List<Quest> currentQuests = new();

    // 수학했던 의뢰 (완료포함)
    public List<int> acceptedQuestIds = new();

    // 오늘 받을 수 있는 의뢰 목록
    public List<Quest> availableQuestsToday = new();

    // availableQuestsToday에서 의뢰를 수락해 currentQuests로 옮긴다.
    public bool acceptQuest(Quest quest)
    {
        if (quest == null || !availableQuestsToday.Contains(quest))
        {
            return false;
        }

        Quest acceptedQuest = CreateQuestInstance(quest);
        availableQuestsToday.Remove(quest);
        currentQuests.Add(acceptedQuest);
        return true;
    }

    // 요구 품목을 전부 보유한 경우에만 한 번에 납품하고 완료 처리한다.
    // 일부 품목만 납품하는 것은 허용하지 않는다.
    public bool progressQuest(Quest quest)
    {
        if (quest == null || !currentQuests.Contains(quest))
        {
            return false;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (inventory == null || quest.requiredItems?.entries == null)
        {
            return false;
        }

        if (!HasAllRequiredItems(quest, inventory))
        {
            return false;
        }

        foreach (ItemEntry entry in quest.requiredItems.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            inventory.Remove(entry.item.id, entry.count);
        }

        finishQuest(quest);
        return true;
    }

    // 의뢰를 완료 처리하고 currentQuests에서 제거한다.
    public void finishQuest(Quest quest)
    {
        if (quest == null || !currentQuests.Contains(quest))
        {
            return;
        }

        givePlayerReward(quest);
        currentQuests.Remove(quest);
        Destroy(quest);
    }

    // 의뢰 보상을 플레이어 인벤토리에 지급한다.
    public void givePlayerReward(Quest quest)
    {
        if (quest?.rewards?.entries == null)
        {
            return;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (inventory == null)
        {
            return;
        }

        foreach (ItemEntry entry in quest.rewards.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            inventory.Add(entry);
        }
    }

    // SO 원본을 변경하지 않도록 런타임 전용 인스턴스를 만든다.
    private static Quest CreateQuestInstance(Quest source)
    {
        Quest instance = ScriptableObject.CreateInstance<Quest>();
        instance.title = source.title;
        instance.clientName = source.clientName;
        instance.content = source.content;
        instance.deadlineDays = source.deadlineDays;
        instance.currentleftDeadlineDays = source.deadlineDays;
        instance.requiredItems = CloneItemEntryList(source.requiredItems);
        instance.rewards = CloneItemEntryList(source.rewards);
        return instance;
    }

    // ItemEntryList를 복사해 수락한 의뢰가 원본 SO와 독립적으로 동작하게 한다.
    private static ItemEntryList CloneItemEntryList(ItemEntryList source)
    {
        if (source == null)
        {
            return null;
        }

        var clone = new ItemEntryList
        {
            length = source.length
        };

        if (source.entries == null || source.entries.Length == 0)
        {
            clone.entries = System.Array.Empty<ItemEntry>();
            return clone;
        }

        clone.entries = new ItemEntry[source.entries.Length];
        for (int i = 0; i < source.entries.Length; i++)
        {
            ItemEntry entry = source.entries[i];
            if (entry == null)
            {
                continue;
            }

            clone.entries[i] = new ItemEntry
            {
                item = entry.item,
                count = entry.count
            };
        }

        return clone;
    }

    // 요구 품목 전체를 충분히 보유했는지 확인한다.
    private static bool HasAllRequiredItems(Quest quest, PlayerInventory inventory)
    {
        foreach (ItemEntry entry in quest.requiredItems.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.item.id))
            {
                return false;
            }

            if (inventory.GetCount(entry.item.id) < entry.count)
            {
                return false;
            }
        }

        return true;
    }

    private PlayerInventory GetPlayerInventory()
    {
        return playerInventory != null
            ? playerInventory
            : FindAnyObjectByType<PlayerInventory>();
    }
}
