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

    // definition id 기준 합산 수량. 인챈트가 달라도 같은 id면 합친다.
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

    // 보유량 > 0인 아이템 id·수량을 복사해 반환한다. 기계 UI 등 표시용.
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
}
