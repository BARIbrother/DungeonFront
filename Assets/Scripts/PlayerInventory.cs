using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly Dictionary<string, int> items = new();

    public void Add(ItemEntry entry)
    {
        if (entry == null || entry.item == null || entry.count <= 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(entry.item.id))
        {
            return;
        }

        items.TryGetValue(entry.item.id, out int existing);
        items[entry.item.id] = existing + entry.count;
    }

    // itemId에 해당하는 보유 수량을 반환한다.
    public int GetCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        items.TryGetValue(itemId, out int count);
        return count;
    }

    // 보유 수량에서 amount만큼 차감하고, 실제로 제거한 수량을 반환한다.
    public int Remove(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
        {
            return 0;
        }

        if (!items.TryGetValue(itemId, out int existing) || existing <= 0)
        {
            return 0;
        }

        int removed = System.Math.Min(existing, amount);
        int remaining = existing - removed;

        if (remaining <= 0)
        {
            items.Remove(itemId);
        }
        else
        {
            items[itemId] = remaining;
        }

        return removed;
    }
}
