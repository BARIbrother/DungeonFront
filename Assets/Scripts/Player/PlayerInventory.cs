using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private readonly List<ItemEntry> itemEntries = new();
    private readonly List<MachineInventoryEntry> machines = new();

    public IReadOnlyList<MachineInventoryEntry> Machines => machines;
    public event Action OnItemsChanged;
    public event Action OnMachinesChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            // 플레이어 오브젝트 전체를 지우지 않도록 컴포넌트만 제거한다.
            Destroy(this);
        }
    }

    // 씬에 있는 단일 인벤을 찾는다. 지급·배치·UI가 같은 인스턴스를 쓰게 한다.
    public static PlayerInventory GetOrFind()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindAnyObjectByType<PlayerInventory>();
        return Instance;
    }

    public void Add(ItemEntry entry)
    {
        if (entry == null || entry.item == null || entry.count <= 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(entry.item.Id))
        {
            return;
        }

        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry existing = itemEntries[i];
            if (existing?.item == null || existing.count <= 0)
            {
                continue;
            }

            if (!existing.item.CanStackWith(entry.item))
            {
                continue;
            }

            existing.count += entry.count;
            OnItemsChanged?.Invoke();
            return;
        }

        itemEntries.Add(new ItemEntry
        {
            item = entry.item.Clone(),
            count = entry.count,
        });
        OnItemsChanged?.Invoke();
    }

    // Dev Mode: 보유 아이템(기계 제외)을 모두 비운다.
    public void ClearItems()
    {
        if (itemEntries.Count == 0)
        {
            return;
        }

        itemEntries.Clear();
        OnItemsChanged?.Invoke();
    }

    // definition id 기준 합산 수량. 퀘스트 등 id만 보는 용도. 인챈트·레벨은 구분하지 않는다.
    // 스택 단위 표시·입출고는 GetOwnedItemEntries / GetCount(Item) / Remove(Item)를 쓴다.
    public int GetCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            if (entry.item.Id == itemId)
            {
                total += entry.count;
            }
        }

        return total;
    }

    public int GetCount(Item item)
    {
        if (item == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            if (entry.item.CanStackWith(item))
            {
                total += entry.count;
            }
        }

        return total;
    }

    // UI 아이콘용. Add로 들어온 Item의 Definition을 id로 다시 찾는다.
    public ItemDefinition GetDefinition(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item?.definition == null || entry.count <= 0)
            {
                continue;
            }

            if (entry.item.Id == itemId)
            {
                return entry.item.definition;
            }
        }

        return null;
    }

    public Item GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            if (entry.item.Id == itemId)
            {
                return entry.item;
            }
        }

        return null;
    }

    // 보유량 > 0인 아이템 id·수량을 id 기준으로 합산해 반환한다. 인챈트 구분이 필요하면 GetOwnedItemEntries.
    public List<KeyValuePair<string, int>> GetOwnedItemCounts()
    {
        var totals = new Dictionary<string, int>();
        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0 || string.IsNullOrEmpty(entry.item.Id))
            {
                continue;
            }

            string id = entry.item.Id;
            totals.TryGetValue(id, out int existing);
            totals[id] = existing + entry.count;
        }

        var owned = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> pair in totals)
        {
            owned.Add(pair);
        }

        return owned;
    }

    // 스택 단위로 복사해 반환한다. 인챈트 차이가 있으면 별도 엔트리로 나온다.
    public List<ItemEntry> GetOwnedItemEntries()
    {
        var owned = new List<ItemEntry>();
        for (int i = 0; i < itemEntries.Count; i++)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            owned.Add(new ItemEntry
            {
                item = entry.item.Clone(),
                count = entry.count,
            });
        }

        return owned;
    }

    // UI 해시용. 클론 없이 id·레벨·인챈트 수·수량만 반영한다.
    public int ComputeOwnedItemsHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + itemEntries.Count;
            for (int i = 0; i < itemEntries.Count; i++)
            {
                ItemEntry entry = itemEntries[i];
                if (entry?.item == null || entry.count <= 0)
                {
                    continue;
                }

                string id = entry.item.Id;
                hash = hash * 31 + (id != null ? id.GetHashCode() : 0);
                hash = hash * 31 + entry.item.ResolvedLevel;
                hash = hash * 31 + entry.item.Enchantments.Count;
                hash = hash * 31 + entry.count;
            }

            return hash;
        }
    }

    public int Remove(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;
        for (int i = 0; i < itemEntries.Count && remaining > 0;)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0 || entry.item.Id != itemId)
            {
                i++;
                continue;
            }

            int removed = Math.Min(entry.count, remaining);
            entry.count -= removed;
            remaining -= removed;

            if (entry.count <= 0)
            {
                itemEntries.RemoveAt(i);
                continue;
            }

            i++;
        }

        int totalRemoved = amount - remaining;
        if (totalRemoved > 0)
        {
            OnItemsChanged?.Invoke();
        }

        return totalRemoved;
    }

    public int Remove(Item item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;
        for (int i = 0; i < itemEntries.Count && remaining > 0;)
        {
            ItemEntry entry = itemEntries[i];
            if (entry?.item == null || entry.count <= 0 || !entry.item.CanStackWith(item))
            {
                i++;
                continue;
            }

            int removed = Math.Min(entry.count, remaining);
            entry.count -= removed;
            remaining -= removed;

            if (entry.count <= 0)
            {
                itemEntries.RemoveAt(i);
                continue;
            }

            i++;
        }

        int totalRemoved = amount - remaining;
        if (totalRemoved > 0)
        {
            OnItemsChanged?.Invoke();
        }

        return totalRemoved;
    }

    public void AddMachine(ItemDef_Machine definition)
    {
        MachineInventoryEntry entry = MachineInventoryEntry.Create(definition);
        if (entry == null)
        {
            return;
        }

        machines.Add(entry);
        OnMachinesChanged?.Invoke();
    }

    public bool TryRemoveMachine(string instanceId, out MachineInventoryEntry removed)
    {
        removed = null;

        if (string.IsNullOrEmpty(instanceId))
        {
            return false;
        }

        for (int i = 0; i < machines.Count; i++)
        {
            if (machines[i].instanceId != instanceId)
            {
                continue;
            }

            removed = machines[i];
            machines.RemoveAt(i);
            OnMachinesChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void ReturnMachine(MachineInventoryEntry entry)
    {
        if (entry == null || entry.definition == null)
        {
            return;
        }

        machines.Add(entry);
        OnMachinesChanged?.Invoke();
    }

    public List<MachineInventoryEntry> GetInInventoryMachines()
    {
        return new List<MachineInventoryEntry>(machines);
    }

    public List<ItemStackSave> ExportItemStacks()
    {
        var result = new List<ItemStackSave>();
        foreach (KeyValuePair<string, int> pair in GetOwnedItemCounts())
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
            {
                continue;
            }

            result.Add(new ItemStackSave { itemId = pair.Key, count = pair.Value });
        }

        return result;
    }

    public void RestoreItemStacks(IEnumerable<ItemStackSave> savedStacks)
    {
        ClearItems();
        if (savedStacks == null)
        {
            return;
        }

        ItemManager itemManager = FindAnyObjectByType<ItemManager>();
        foreach (ItemStackSave stack in savedStacks)
        {
            if (stack == null || string.IsNullOrWhiteSpace(stack.itemId) || stack.count <= 0)
            {
                continue;
            }

            Item item = itemManager != null ? itemManager.CreateItem(stack.itemId) : null;
            if (item == null)
            {
                Debug.LogWarning($"[PlayerInventory] 저장 아이템을 복원하지 못했습니다: {stack.itemId}");
                continue;
            }

            Add(new ItemEntry
            {
                item = item,
                count = stack.count,
            });
        }
    }
}

[Serializable]
public class ItemStackSave
{
    public string itemId;
    public int count;
}
