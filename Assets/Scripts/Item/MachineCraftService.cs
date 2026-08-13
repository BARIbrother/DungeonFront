using UnityEngine;

// 준비 단계에서 인벤 재료 + 골드로 기계 1대를 즉시 제작한다.
public static class MachineCraftService
{
    public static bool IsTechUnlocked(MachineCraftCatalog.Recipe recipe)
    {
        if (recipe == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(recipe.requiredTechId))
        {
            return true;
        }

        return UnlockManager.Instance != null
            && UnlockManager.Instance.IsUnlocked(recipe.requiredTechId);
    }

    public static bool CanAfford(MachineCraftCatalog.Recipe recipe, PlayerInventory inventory, int gold)
    {
        if (recipe == null || inventory == null || gold < recipe.gold)
        {
            return false;
        }

        return HasItems(recipe, inventory);
    }

    public static bool HasItems(MachineCraftCatalog.Recipe recipe, PlayerInventory inventory)
    {
        if (recipe?.items == null || inventory == null)
        {
            return false;
        }

        for (int i = 0; i < recipe.items.Length; i++)
        {
            MachineCraftCatalog.ItemCost cost = recipe.items[i];
            if (string.IsNullOrEmpty(cost.itemId) || cost.count <= 0)
            {
                continue;
            }

            if (inventory.GetCount(cost.itemId) < cost.count)
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryCraft(string machineDefId, out string error, ItemDef_Machine definition = null)
    {
        error = null;
        MachineCraftCatalog.Recipe recipe = MachineCraftCatalog.Get(machineDefId);
        if (recipe == null)
        {
            error = "구매 목록에 없는 기계입니다.";
            return false;
        }

        if (GameSessionState.Instance != null
            && GameSessionState.Instance.phase != GamePhase.Prepare)
        {
            error = "준비 단계에서만 기계를 만들 수 있습니다.";
            return false;
        }

        if (!IsTechUnlocked(recipe))
        {
            error = FormatLockedMessage(recipe);
            return false;
        }

        PlayerInventory inventory = PlayerInventory.GetOrFind();
        if (inventory == null)
        {
            error = "인벤토리를 찾을 수 없습니다.";
            return false;
        }

        if (!HasItems(recipe, inventory))
        {
            error = "재료가 부족합니다.";
            return false;
        }

        Week3EconomyService economy = Object.FindAnyObjectByType<Week3EconomyService>();
        int gold = economy != null
            ? economy.Gold
            : GameSessionState.Instance != null ? GameSessionState.Instance.gold : 0;
        if (gold < recipe.gold)
        {
            error = "골드가 부족합니다.";
            return false;
        }

        ItemDef_Machine machine = definition != null ? definition : FindMachine(machineDefId);
        if (machine == null || machine.machinePrefab == null)
        {
            error = "기계 정의를 찾을 수 없습니다.";
            return false;
        }

        if (!TrySpendGold(economy, recipe.gold))
        {
            error = "골드가 부족합니다.";
            return false;
        }

        if (!TrySpendItems(inventory, recipe))
        {
            RefundGold(economy, recipe.gold);
            error = "재료가 부족합니다.";
            return false;
        }

        int countBefore = inventory.Machines.Count;
        inventory.AddMachine(machine);
        if (inventory.Machines.Count <= countBefore)
        {
            RefundGold(economy, recipe.gold);
            RefundItems(inventory, recipe);
            error = "기계를 지급할 수 없습니다.";
            return false;
        }

        return true;
    }

    public static string FormatLockedMessage(MachineCraftCatalog.Recipe recipe)
    {
        string techName = TechTreeCatalog.DisplayName(recipe?.requiredTechId);
        if (string.IsNullOrEmpty(techName))
        {
            return "테크 트리에서 먼저 해금해야 합니다.";
        }

        return $"{techName}을(를) 테크 트리에서 해금해야 합니다.";
    }

    public static string FormatCost(MachineCraftCatalog.Recipe recipe)
    {
        if (recipe == null)
        {
            return string.Empty;
        }

        var text = new System.Text.StringBuilder();
        text.Append(recipe.gold);
        text.Append("G");
        if (recipe.items == null)
        {
            return text.ToString();
        }

        for (int i = 0; i < recipe.items.Length; i++)
        {
            MachineCraftCatalog.ItemCost cost = recipe.items[i];
            if (string.IsNullOrEmpty(cost.itemId) || cost.count <= 0)
            {
                continue;
            }

            text.Append(" · ");
            text.Append(MachineCraftCatalog.ItemDisplayName(cost.itemId));
            text.Append(" ");
            text.Append(cost.count);
        }

        return text.ToString();
    }

    private static ItemDef_Machine FindMachine(string machineDefId)
    {
        PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
        MachineDatabase database = movement != null ? movement.MachineDatabase : null;
        return database != null ? database.Get(machineDefId) : null;
    }

    private static bool TrySpendGold(Week3EconomyService economy, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (economy != null)
        {
            return economy.TrySpendGold(amount);
        }

        if (GameSessionState.Instance == null || GameSessionState.Instance.gold < amount)
        {
            return false;
        }

        GameSessionState.Instance.AddGold(-amount);
        return true;
    }

    private static void RefundGold(Week3EconomyService economy, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (economy != null)
        {
            economy.AddGold(amount);
            return;
        }

        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.AddGold(amount);
        }
    }

    private static bool TrySpendItems(PlayerInventory inventory, MachineCraftCatalog.Recipe recipe)
    {
        for (int i = 0; i < recipe.items.Length; i++)
        {
            MachineCraftCatalog.ItemCost cost = recipe.items[i];
            if (string.IsNullOrEmpty(cost.itemId) || cost.count <= 0)
            {
                continue;
            }

            int removed = inventory.Remove(cost.itemId, cost.count);
            if (removed >= cost.count)
            {
                continue;
            }

            if (removed > 0)
            {
                RefundItem(inventory, cost.itemId, removed);
            }

            for (int j = 0; j < i; j++)
            {
                MachineCraftCatalog.ItemCost spent = recipe.items[j];
                if (!string.IsNullOrEmpty(spent.itemId) && spent.count > 0)
                {
                    RefundItem(inventory, spent.itemId, spent.count);
                }
            }

            return false;
        }

        return true;
    }

    private static void RefundItems(PlayerInventory inventory, MachineCraftCatalog.Recipe recipe)
    {
        for (int i = 0; i < recipe.items.Length; i++)
        {
            MachineCraftCatalog.ItemCost cost = recipe.items[i];
            if (!string.IsNullOrEmpty(cost.itemId) && cost.count > 0)
            {
                RefundItem(inventory, cost.itemId, cost.count);
            }
        }
    }

    private static void RefundItem(PlayerInventory inventory, string itemId, int count)
    {
        ItemManager manager = Object.FindAnyObjectByType<ItemManager>();
        Item item = manager != null ? manager.CreateItem(itemId) : null;
        if (item == null)
        {
            ItemDefinition owned = inventory.GetDefinition(itemId);
            item = Item.FromDefinition(owned);
        }

        if (item == null)
        {
            return;
        }

        inventory.Add(new ItemEntry
        {
            item = item,
            count = count,
        });
    }
}
