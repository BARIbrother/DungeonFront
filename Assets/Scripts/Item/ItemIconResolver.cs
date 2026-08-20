using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ItemDefinition.icon이 비었거나 깨져 있어도 Art/Items 파일명 규칙으로 스프라이트를 찾는다.
public static class ItemIconResolver
{
    private static readonly Dictionary<string, Sprite> cache = new();

    // itemId → Assets/Art/Items/{stem}.png (확장자 제외)
    private static readonly Dictionary<string, string> idToIconStem = new()
    {
        { "wood_log", "wood_log_icon" },
        { "wood_stick", "wood_stick_icon" },
        { "paper", "paper_icon" },
        { "iron_ore", "iron_ore_icon" },
        { "iron", "iron_ingot_icon" },
        { "iron_bar", "iron_ingot_icon" },
        { "iron_ingot", "iron_ingot_icon" },
        { "iron_plate", "iron_plate_icon" },
        { "iron_rod", "iron_rod_icon" },
        { "iron_chestplate", "iron_chestplate_icon" },
        { "iron_helmet", "iron_helmet_icon" },
        { "iron_leggings", "iron_leggings_icon" },
        { "iron_boots", "iron_boots_icon" },
        { "iron_sword", "iron_sword_icon" },
        { "iron_warhammer", "iron_warhammer_icon" },
        { "blackstone_ore", "blackstone_ore_icon" },
        { "blackstone_ingot", "blackstone_ingot_icon" },
        { "darksteel_ingot", "darksteel_ingot_icon" },
        { "darksteel_plate", "darksteel_plate_icon" },
        { "darksteel_rod", "darksteel_rod_icon" },
        { "darksteel_sword", "darksteel_sword_icon" },
        { "darksteel_warhammer", "darksteel_warhammer_icon" },
        { "darksteel_helmet", "darksteel_helmet_icon" },
        { "darksteel_chestplate", "darksteel_chestplate_icon" },
        { "darksteel_leggings", "darksteel_leggings_icon" },
        { "darksteel_boots", "darksteel_boots_icon" },
        { "whitestone_ore", "whitestone_ore_icon" },
        { "whitestone_ingot", "whitestone_ingot_icon" },
        { "brightsteel_ingot", "brightsteel_ingot_icon" },
        { "brightsteel_plate", "brightsteel_plate_icon" },
        { "brightsteel_rod", "brightsteel_rod_icon" },
        { "brightsteel_sword", "brightsteel_sword_icon" },
        { "brightsteel_warhammer", "brightsteel_warhammer_icon" },
        { "brightsteel_helmet", "brightsteel_helmet_icon" },
        { "brightsteel_chestplate", "brightsteel_chestplate_icon" },
        { "brightsteel_leggings", "brightsteel_leggings_icon" },
        { "brightsteel_boots", "brightsteel_boots_icon" },
        { "greysteel_ingot", "greysteel_ingot_icon" },
        { "greysteel_plate", "greysteel_plate_icon" },
        { "greysteel_rod", "greysteel_rod_icon" },
        { "greysteel_sword", "greysteel_sword_icon" },
        { "greysteel_warhammer", "greysteel_warhammer_icon" },
        { "greysteel_helmet", "greysteel_helmet_icon" },
        { "greysteel_chestplate", "greysteel_chestplate_icon" },
        { "greysteel_leggings", "greysteel_leggings_icon" },
        { "greysteel_boots", "greysteel_boots_icon" },
        { "mana_ore", "mana_ore_icon" },
        { "mana_crystal", "mana_crystal_icon" },
        { "mana_core", "mana_core_icon" },
        { "mana_wand", "mana_wand_icon" },
        { "manasteel_sword", "manasteel_sword_icon" },
        { "manasteel_helmet", "manasteel_helmet_icon" },
        { "manasteel_chestplate", "manasteel_chestplate_icon" },
        { "manasteel_leggings", "manasteel_leggings_icon" },
        { "manasteel_boots", "manasteel_boots_icon" },
        { "blank_magic_scroll", "blank_magic_scroll_icon" },
        { "element_scroll", "element_scroll_icon" },
        { "element_form_scroll", "element_form_scroll_icon" },
        { "blank_tier2_magic_scroll", "blank_tier2_magic_scroll_icon" },
        { "tier2_element_scroll", "tier2_element_scroll_icon" },
        { "tier2_element_form_scroll", "tier2_element_form_scroll_icon" },
        { "mage_robe", "mage_robe_icon" },
        { "dark_magic_core", "dark_magic_core_icon" },
        { "dark_mage_robe", "dark_mage_robe_icon" },
        { "dark_magic_staff", "dark_magic_staff_icon" },
        { "light_magic_core", "light_magic_core_icon" },
        { "light_mage_robe", "light_mage_robe_icon" },
        { "light_magic_staff", "light_magic_staff_icon" },
    };

    public static Sprite Resolve(Item item)
    {
        return Resolve(item?.definition);
    }

    public static Sprite Resolve(ItemDefinition item)
    {
        if (item == null)
        {
            return null;
        }

        if (item is ItemDef_Machine machineDefinition)
        {
            Sprite fromPrefab = MachineIconResolver.Resolve(machineDefinition);
            if (IsUsable(fromPrefab))
            {
                return fromPrefab;
            }
        }

        // Art 경로를 최우선으로 쓴다. SO icon 참조가 깨져 있어도 동일 아이디면 표시된다.
        if (!string.IsNullOrEmpty(item.id))
        {
            Sprite fromArt = LoadByItemId(item.id);
            if (IsUsable(fromArt))
            {
                item.icon = fromArt;
                return fromArt;
            }
        }

        if (IsUsable(item.icon))
        {
            return item.icon;
        }

        ItemManager manager = Object.FindAnyObjectByType<ItemManager>();
        if (manager != null && !string.IsNullOrEmpty(item.id))
        {
            ItemDefinition registered = manager.Get(item.id);
            if (IsUsable(registered?.icon))
            {
                return registered.icon;
            }
        }

        PlayerInventory inventory = PlayerInventory.Instance != null
            ? PlayerInventory.Instance
            : Object.FindAnyObjectByType<PlayerInventory>();
        if (inventory != null && !string.IsNullOrEmpty(item.id))
        {
            ItemDefinition cached = inventory.GetDefinition(item.id);
            if (cached != null && cached != item && IsUsable(cached.icon))
            {
                return cached.icon;
            }
        }

        return null;
    }

    public static Sprite ResolveById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        Sprite fromArt = LoadByItemId(itemId);
        if (IsUsable(fromArt))
        {
            return fromArt;
        }

        ItemManager manager = Object.FindAnyObjectByType<ItemManager>();
        if (manager != null)
        {
            ItemDefinition registered = manager.Get(itemId);
            if (IsUsable(registered?.icon))
            {
                return registered.icon;
            }
        }

        return null;
    }

    public static bool IsUsable(Sprite sprite)
    {
        return sprite != null
            && sprite.texture != null
            && sprite.rect.width > 0.5f
            && sprite.rect.height > 0.5f;
    }

    private static Sprite LoadByItemId(string itemId)
    {
        if (cache.TryGetValue(itemId, out Sprite cached) && IsUsable(cached))
        {
            return cached;
        }

        string stem = ResolveIconStem(itemId);
        Sprite sprite = LoadSprite(stem);
        if (sprite != null)
        {
            cache[itemId] = sprite;
        }

        return sprite;
    }

    private static string ResolveIconStem(string itemId)
    {
        if (idToIconStem.TryGetValue(itemId, out string stem))
        {
            return stem;
        }

        if (itemId.EndsWith("_icon"))
        {
            return itemId;
        }

        return itemId + "_icon";
    }

    private static Sprite LoadSprite(string stem)
    {
        if (string.IsNullOrEmpty(stem))
        {
            return null;
        }

#if UNITY_EDITOR
        Sprite fromArt = LoadSpriteAtPath($"Assets/Art/Items/{stem}.png");
        if (IsUsable(fromArt))
        {
            return fromArt;
        }
#endif

        Sprite fromResources = Resources.Load<Sprite>($"ItemIcons/{stem}");
        return IsUsable(fromResources) ? fromResources : null;
    }

#if UNITY_EDITOR
    private static Sprite LoadSpriteAtPath(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null)
        {
            return null;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && IsUsable(sprite))
            {
                return sprite;
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
#endif
}
