using UnityEngine;

// 마나 정수. 함량은 Docs/08-machine-recipes.md 표와 같다.
// 창고가 아니라 마나 저장소·마나 제작기·마법 부여대만 다룬다.
public static class ManaEssence
{
    public const string LowId = "low_monster_mana_essence";
    public const string MidId = "mid_monster_mana_essence";
    public const string HighId = "high_monster_mana_essence";
    public const string DungeonMasterId = "dungeon_master_essence";

    public static bool IsEssence(Item item)
    {
        return IsEssence(item != null ? item.Id : null);
    }

    public static bool IsEssence(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        return itemId == LowId
            || itemId == MidId
            || itemId == HighId
            || itemId == DungeonMasterId;
    }

    public static bool TryGetValue(Item item, out int value)
    {
        return TryGetValue(item != null ? item.Id : null, out value);
    }

    public static bool TryGetValue(string itemId, out int value)
    {
        value = itemId switch
        {
            LowId => 10,
            MidId => 50,
            HighId => 100,
            DungeonMasterId => 500,
            _ => 0,
        };
        return value > 0;
    }

    public static int SumFromInputs(ItemEntryList list)
    {
        if (list?.entries == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < list.entries.Length; i++)
        {
            ItemEntry entry = list.entries[i];
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            if (!TryGetValue(entry.item, out int unit))
            {
                continue;
            }

            total += unit * entry.count;
        }

        return total;
    }
}
