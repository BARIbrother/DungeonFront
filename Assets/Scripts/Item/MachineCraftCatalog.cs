using System;

// 기계 1대 구매 비용. 해금(명예)과 별개로, 제작은 직전 티어 재료 + 골드.
public static class MachineCraftCatalog
{
    public readonly struct ItemCost
    {
        public readonly string itemId;
        public readonly int count;

        public ItemCost(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }

    public sealed class Recipe
    {
        public string machineDefId;
        public string requiredTechId;
        public int gold;
        public ItemCost[] items;
    }

    public static readonly Recipe[] All =
    {
        // 시작 기계. 숨은 시작 테크가 이미 열려 있어야 추가 제작 가능.
        RecipeOf("Miner_1", "m_drill_1", 8,
            Item("iron_plate", 4), Item("iron_rod", 2)),
        RecipeOf("Smelter_1", "m_furnace_1", 12,
            Item("iron_plate", 6), Item("iron_rod", 2)),
        RecipeOf("Assembler_1", "m_crafter_1", 20,
            Item("iron_plate", 8), Item("iron_rod", 4)),
        RecipeOf("Warehouse_1", "m_warehouse_1", 10,
            Item("wood_log", 6), Item("iron_plate", 4)),

        RecipeOf("ConveyerBelt_1", "m_conveyor_1", 5,
            Item("iron_plate", 2), Item("iron_rod", 1)),
        RecipeOf("Extractor_1", "m_conveyor_1", 8,
            Item("iron_plate", 4), Item("iron_rod", 2)),

        RecipeOf("Miner_2", "m_drill_2", 30,
            Item("iron_plate", 8), Item("iron_rod", 4)),
        RecipeOf("ManaExtractor_1", "m_manaext_1", 25,
            Item("iron_plate", 6), Item("iron_rod", 2)),
        RecipeOf("ManaStorage_1", "m_manastore_1", 15,
            Item("iron_plate", 4), Item("wood_log", 2)),
        RecipeOf("ManaHandmade_1", "m_manacraft_1", 40,
            Item("iron_plate", 8), Item("iron_rod", 4), Item("mana_ore", 2)),
        RecipeOf("Enchanting_1", "m_enchant_1", 35,
            Item("iron_plate", 6), Item("mana_core", 2)),

        RecipeOf("Smelter_2", "m_furnace_2", 80,
            Item("mana_core", 8), Item("blackstone_ore", 2), Item("whitestone_ore", 2)),
        RecipeOf("Assembler_2", "m_crafter_2", 80,
            Item("mana_core", 10), Item("iron_plate", 6)),

        RecipeOf("ManaAssembler_2", "m_manacraft_2", 70,
            Item("darksteel_plate", 4), Item("brightsteel_plate", 4)),
        RecipeOf("Foundry_1", "m_foundry_1", 80,
            Item("darksteel_ingot", 6), Item("brightsteel_ingot", 6)),
        RecipeOf("Assembler_3", "m_crafter_3", 60,
            Item("darksteel_plate", 6), Item("brightsteel_plate", 4)),
        RecipeOf("Miner_3", "m_drill_3", 50,
            Item("darksteel_ingot", 6), Item("brightsteel_ingot", 6)),
        RecipeOf("Smelter_3", "m_furnace_3", 90,
            Item("darksteel_ingot", 8), Item("brightsteel_ingot", 8), Item("blackstone_ore", 4)),

        RecipeOf("ManaAssembler_3", "m_manacraft_2", 120,
            Item("greysteel_plate", 6), Item("structure_pillar", 2)),
        RecipeOf("Altar_1", "m_crafter_3", 150,
            Item("greysteel_ingot", 8), Item("concrete", 4)),
    };

    // 자동 제작기 1티어는 수동 제작대를 쓴다.
    public static bool IsObtainable(string machineDefId)
    {
        return machineDefId switch
        {
            "Assembler_1" => false,
            _ => !string.IsNullOrEmpty(machineDefId),
        };
    }

    public static Recipe Get(string machineDefId)
    {
        if (string.IsNullOrEmpty(machineDefId))
        {
            return null;
        }

        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].machineDefId == machineDefId)
            {
                return All[i];
            }
        }

        return null;
    }

    public static Recipe GetByTechId(string techId)
    {
        if (string.IsNullOrEmpty(techId))
        {
            return null;
        }

        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].requiredTechId == techId)
            {
                return All[i];
            }
        }

        return null;
    }

    public static string ItemDisplayName(string itemId)
    {
        ItemManager manager = UnityEngine.Object.FindAnyObjectByType<ItemManager>();
        ItemDefinition definition = manager != null ? manager.Get(itemId) : null;
        if (definition != null && !string.IsNullOrEmpty(definition.displayName))
        {
            return definition.displayName;
        }

        return itemId switch
        {
            "iron_plate" => "철 판",
            "iron_rod" => "철 막대",
            "iron_bar" => "철 주괴",
            "wood_log" => "원목",
            "mana_ore" => "마력석",
            "mana_core" => "마나 코어",
            "blackstone_ore" => "칠흑석",
            "whitestone_ore" => "순백석",
            "darksteel_ingot" => "흑강 주괴",
            "darksteel_plate" => "흑강 판",
            "brightsteel_ingot" => "백강 주괴",
            "brightsteel_plate" => "백강 판",
            "greysteel_ingot" => "진강 주괴",
            "greysteel_plate" => "진강 판",
            "concrete" => "콘크리트",
            "structure_pillar" => "구조물 기둥",
            _ => itemId,
        };
    }

    private static Recipe RecipeOf(string machineDefId, string requiredTechId, int gold, params ItemCost[] items)
    {
        return new Recipe
        {
            machineDefId = machineDefId,
            requiredTechId = requiredTechId,
            gold = gold,
            items = items ?? Array.Empty<ItemCost>(),
        };
    }

    private static ItemCost Item(string itemId, int count)
    {
        return new ItemCost(itemId, count);
    }
}
