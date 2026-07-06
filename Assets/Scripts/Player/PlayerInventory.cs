using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly Dictionary<string, int> items = new();
    private readonly List<MachineInventoryEntry> machines = new();

    public IReadOnlyList<MachineInventoryEntry> Machines => machines;
    public event Action OnMachinesChanged;

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

    // ItemDef_Machine 정의로 인벤에 기계 인스턴스 1개를 추가한다.
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

    // instanceId에 해당하는 기계를 인벤에서 제거한다. 성공 시 true.
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

    // 맵에서 회수한 기계 인스턴스를 인벤에 되돌린다.
    public void ReturnMachine(MachineInventoryEntry entry)
    {
        if (entry == null || entry.definition == null)
        {
            return;
        }

        machines.Add(entry);
        OnMachinesChanged?.Invoke();
    }
}
