[System.Serializable]
public class ItemEntryList
{
    public int length;
    public ItemEntry[] entries;

    public void Resize()
    {
        if (entries != null)
        {
            System.Array.Clear(entries, 0, entries.Length);
        }

        entries = new ItemEntry[length];
    }

    // 레시피 입력 슬롯을 모두 충족하는지 확인한다.
    public bool MatchesRecipe(Recipe recipe)
    {
        if (recipe?.inputEntryList == null || recipe.inputEntryList.length == 0)
        {
            return true;
        }

        return HasAtLeast(recipe.inputEntryList);
    }

    // required의 각 슬롯 수량을 이 포트가 보유하고 있는지 확인한다.
    public bool HasAtLeast(ItemEntryList required)
    {
        if (required?.entries == null)
        {
            return true;
        }

        for (int i = 0; i < required.entries.Length; i++)
        {
            ItemEntry requiredEntry = required.entries[i];
            if (requiredEntry == null || requiredEntry.item == null || requiredEntry.count <= 0)
            {
                continue;
            }

            if (i >= entries.Length)
            {
                return false;
            }

            ItemEntry portEntry = entries[i];
            if (portEntry == null || portEntry.item == null || portEntry.count < requiredEntry.count)
            {
                return false;
            }

            if (portEntry.item.id != requiredEntry.item.id)
            {
                return false;
            }
        }

        return true;
    }

    // 레시피 출력을 모두 수용할 슬롯이 있는지 확인한다.
    public bool CanFit(ItemEntryList outputs)
    {
        if (outputs?.entries == null)
        {
            return true;
        }

        for (int i = 0; i < outputs.entries.Length; i++)
        {
            ItemEntry outputEntry = outputs.entries[i];
            if (outputEntry == null || outputEntry.item == null || outputEntry.count <= 0)
            {
                continue;
            }

            if (i >= entries.Length)
            {
                return false;
            }

            ItemEntry portEntry = entries[i];
            if (portEntry == null || portEntry.item == null || portEntry.count <= 0)
            {
                continue;
            }

            if (portEntry.item.id != outputEntry.item.id || portEntry.count > 0)
            {
                return false;
            }
        }

        return true;
    }

    // required 슬롯 수량만큼 차감한다.
    public bool TryConsume(ItemEntryList required)
    {
        if (!HasAtLeast(required))
        {
            return false;
        }

        if (required?.entries == null)
        {
            return true;
        }

        for (int i = 0; i < required.entries.Length; i++)
        {
            ItemEntry requiredEntry = required.entries[i];
            if (requiredEntry == null || requiredEntry.item == null || requiredEntry.count <= 0)
            {
                continue;
            }

            ItemEntry portEntry = entries[i];
            portEntry.count -= requiredEntry.count;
            if (portEntry.count <= 0)
            {
                portEntry.item = null;
                portEntry.count = 0;
            }
        }

        return true;
    }

    // 비어 있는 슬롯 또는 동일 아이템 슬롯에 추가한다.
    public bool TryAdd(ItemEntry item)
    {
        if (item == null || item.item == null || item.count <= 0 || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            ItemEntry portEntry = entries[i];
            if (portEntry == null)
            {
                entries[i] = new ItemEntry { item = item.item, count = item.count };
                return true;
            }

            if (portEntry.item != null && portEntry.item.id == item.item.id)
            {
                portEntry.count += item.count;
                return true;
            }
        }

        for (int i = 0; i < entries.Length; i++)
        {
            ItemEntry portEntry = entries[i];
            if (portEntry == null)
            {
                entries[i] = new ItemEntry { item = item.item, count = item.count };
                return true;
            }

            if (portEntry.item == null || portEntry.count <= 0)
            {
                portEntry.item = item.item;
                portEntry.count = item.count;
                return true;
            }
        }

        return false;
    }

    // 레시피 입력 슬롯 인덱스에 맞춰 아이템을 넣는다. 동일 아이템이 여러 슬롯에 필요할 때 슬롯별로 분배한다.
    public bool TryAddToRecipeInput(ItemEntry item, Recipe recipe)
    {
        if (item == null || item.item == null || item.count <= 0 || entries == null)
        {
            return false;
        }

        if (recipe?.inputEntryList?.entries == null)
        {
            return TryAdd(item);
        }

        int remaining = item.count;
        for (int i = 0; i < entries.Length && i < recipe.inputEntryList.entries.Length && remaining > 0; i++)
        {
            ItemEntry required = recipe.inputEntryList.entries[i];
            if (required == null || required.item == null || required.item.id != item.item.id)
            {
                continue;
            }

            int currentCount = GetSlotCount(i);
            int capacity = required.count - currentCount;
            if (capacity <= 0)
            {
                continue;
            }

            int addCount = remaining < capacity ? remaining : capacity;
            SetSlotItem(i, item.item, currentCount + addCount);
            remaining -= addCount;
        }

        return remaining == 0;
    }

    // 포트에 들어 있는 모든 항목을 복사해 반환한다. Resize 전 보존용.
    public System.Collections.Generic.List<ItemEntry> CopyAllEntries()
    {
        var copied = new System.Collections.Generic.List<ItemEntry>();
        if (entries == null)
        {
            return copied;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            ItemEntry entry = entries[i];
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            copied.Add(new ItemEntry { item = entry.item, count = entry.count });
        }

        return copied;
    }

    private int GetSlotCount(int index)
    {
        if (entries == null || index < 0 || index >= entries.Length)
        {
            return 0;
        }

        ItemEntry entry = entries[index];
        if (entry == null || entry.item == null || entry.count <= 0)
        {
            return 0;
        }

        return entry.count;
    }

    private void SetSlotItem(int index, ItemDefinition item, int count)
    {
        if (entries == null || index < 0 || index >= entries.Length)
        {
            return;
        }

        if (entries[index] == null)
        {
            entries[index] = new ItemEntry();
        }

        entries[index].item = item;
        entries[index].count = count;
    }

    // 첫 비어 있지 않은 슬롯에서 항목을 꺼낸다.
    public bool TryTakeFirst(out ItemEntry taken)
    {
        taken = null;
        if (entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            ItemEntry portEntry = entries[i];
            if (portEntry == null || portEntry.item == null || portEntry.count <= 0)
            {
                continue;
            }

            taken = new ItemEntry { item = portEntry.item, count = portEntry.count };
            portEntry.item = null;
            portEntry.count = 0;
            return true;
        }

        return false;
    }

    // 요청과 일치하는 슬롯에서 꺼낸다.
    public bool TryTake(ItemEntry request)
    {
        if (request == null || request.item == null || request.count <= 0 || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            ItemEntry portEntry = entries[i];
            if (portEntry == null || portEntry.item == null || portEntry.count <= 0)
            {
                continue;
            }

            if (portEntry.item.id != request.item.id || portEntry.count < request.count)
            {
                continue;
            }

            portEntry.count -= request.count;
            if (portEntry.count <= 0)
            {
                portEntry.item = null;
                portEntry.count = 0;
            }

            return true;
        }

        return false;
    }

    // 레시피 출력을 슬롯에 배치한다.
    public bool TryAddOutputs(ItemEntryList outputs)
    {
        if (outputs?.entries == null)
        {
            return true;
        }

        if (!CanFit(outputs))
        {
            return false;
        }

        for (int i = 0; i < outputs.entries.Length; i++)
        {
            ItemEntry outputEntry = outputs.entries[i];
            if (outputEntry == null || outputEntry.item == null || outputEntry.count <= 0)
            {
                continue;
            }

            if (entries[i] == null)
            {
                entries[i] = new ItemEntry();
            }

            entries[i].item = outputEntry.item;
            entries[i].count = outputEntry.count;
        }

        return true;
    }
}
