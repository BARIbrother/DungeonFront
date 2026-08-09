using System;
using UnityEngine;

// 상시 의뢰의 배수 계산과 실제 인벤토리 차감·보상만 담당한다.
// UI가 없어도 이 클래스의 GetMaxMultiplier/TryDeliver만 호출하면 테스트할 수 있다.
public class PerpetualQuestService : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Week3EconomyService economy;

    public event Action<Quest, int> OnDelivered;

    public int GetMaxMultiplier(Quest quest)
    {
        PlayerInventory inventory = GetInventory();
        if (quest == null
            || !(QuestRuntimeRegistry.Get(quest)?.IsPerpetual ?? false)
            || inventory == null
            || quest.requiredItems?.entries == null
            || quest.requiredItems.entries.Length == 0)
        {
            return 0;
        }

        int maximum = int.MaxValue;
        foreach (ItemEntry requirement in quest.requiredItems.entries)
        {
            if (requirement?.item == null || requirement.count <= 0)
            {
                continue;
            }

            int possible = inventory.GetCount(requirement.item.Id) / requirement.count;
            maximum = Mathf.Min(maximum, possible);
        }

        return maximum == int.MaxValue ? 0 : Mathf.Max(0, maximum);
    }

    public bool TryDeliver(Quest quest, int multiplier)
    {
        PlayerInventory inventory = GetInventory();
        if (inventory == null
            || quest == null
            || !(QuestRuntimeRegistry.Get(quest)?.IsPerpetual ?? false)
            || multiplier <= 0
            || multiplier > GetMaxMultiplier(quest))
        {
            return false;
        }

        // 먼저 전부 검증했으므로 여기부터는 중간 실패 없이 한 번에 차감한다.
        foreach (ItemEntry requirement in quest.requiredItems.entries)
        {
            if (requirement?.item != null && requirement.count > 0)
            {
                inventory.Remove(requirement.item.Id, requirement.count * multiplier);
            }
        }

        GiveRewards(quest, multiplier, inventory);
        OnDelivered?.Invoke(quest, multiplier);
        Debug.Log($"[PerpetualQuest] {quest.title} x{multiplier} 납품 완료", quest);
        return true;
    }

    private void GiveRewards(Quest quest, int multiplier, PlayerInventory inventory)
    {
        economy ??= FindAnyObjectByType<Week3EconomyService>();

        foreach (ItemEntry reward in quest.rewards?.entries ?? Array.Empty<ItemEntry>())
        {
            if (reward?.item == null || reward.count <= 0)
            {
                continue;
            }

            int total = reward.count * multiplier;
            if (IsId(reward, "gold") && economy != null)
            {
                economy.AddGold(total);
            }
            else if ((IsId(reward, "fame") || IsId(reward, "reputation"))
                && economy != null)
            {
                economy.AddReputation(total);
            }
            else
            {
                inventory.Add(new ItemEntry { item = reward.item.Clone(), count = total });
            }
        }

        int rewardReputation =
            QuestRuntimeRegistry.GetOrCreate(quest).rewardReputation;
        if (rewardReputation > 0 && economy != null)
        {
            economy.AddReputation(rewardReputation * multiplier);
        }
    }

    private static bool IsId(ItemEntry entry, string expected)
    {
        return string.Equals(
            entry.item.Id,
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    private PlayerInventory GetInventory()
    {
        playerInventory ??= PlayerInventory.Instance;
        playerInventory ??= FindAnyObjectByType<PlayerInventory>();
        return playerInventory;
    }
}
