using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private readonly Dictionary<string, int> items = new();
    private readonly List<MachineInventoryEntry> machines = new();

    public IReadOnlyList<MachineInventoryEntry> Machines => machines;
    public event Action OnItemsChanged;
    public event Action OnMachinesChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

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
        OnItemsChanged?.Invoke();
    }

    public int GetCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return 0;
        }

        items.TryGetValue(itemId, out int count);
        return count;
    }

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

        OnItemsChanged?.Invoke();

        return removed;
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