using System;

// questline.json itemcode → 정규 itemId·레벨·인챈트 해석.
public static class QuestItemCodeResolver
{
    public struct ResolvedItem
    {
        public string itemId;
        public int level;
        public Enchantment[] enchantments;
    }

    private static readonly Enchantment[] BasicFour =
    {
        Attr(EnchantmentId.Fire),
        Attr(EnchantmentId.Water),
        Attr(EnchantmentId.Wind),
        Attr(EnchantmentId.Earth),
    };

    private static readonly Enchantment[][] Tier2Pairs =
    {
        new[] { Attr(EnchantmentId.Fire), Attr(EnchantmentId.Wind) },
        new[] { Attr(EnchantmentId.Fire), Attr(EnchantmentId.Earth) },
        new[] { Attr(EnchantmentId.Fire), Attr(EnchantmentId.Water) },
        new[] { Attr(EnchantmentId.Wind), Attr(EnchantmentId.Water) },
        new[] { Attr(EnchantmentId.Wind), Attr(EnchantmentId.Earth) },
        new[] { Attr(EnchantmentId.Earth), Attr(EnchantmentId.Water) },
    };

    public static ResolvedItem Resolve(string questLineId, string itemCode, int occurrenceIndex)
    {
        ResolvedItem resolved = ResolveExact(itemCode);

        if (string.Equals(questLineId, "Q022", StringComparison.Ordinal)
            && string.Equals(itemCode, "manasteel_leggings", StringComparison.Ordinal))
        {
            resolved.enchantments = occurrenceIndex == 0
                ? new[] { Attr(EnchantmentId.Fire) }
                : new[] { Attr(EnchantmentId.Water) };
            return resolved;
        }

        if ((string.Equals(questLineId, "Q027", StringComparison.Ordinal)
                || string.Equals(questLineId, "Q036", StringComparison.Ordinal))
            && string.Equals(itemCode, "element_scroll", StringComparison.Ordinal)
            && occurrenceIndex >= 0
            && occurrenceIndex < BasicFour.Length)
        {
            resolved.enchantments = new[] { BasicFour[occurrenceIndex] };
            return resolved;
        }

        if (string.Equals(questLineId, "Q047", StringComparison.Ordinal)
            && string.Equals(itemCode, "manasteel_chestplate", StringComparison.Ordinal)
            && occurrenceIndex >= 0
            && occurrenceIndex < Tier2Pairs.Length)
        {
            resolved.enchantments = Tier2Pairs[occurrenceIndex];
            return resolved;
        }

        return resolved;
    }

    private static ResolvedItem ResolveExact(string itemCode)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
        {
            return default;
        }

        switch (itemCode)
        {
            case "iron_ore":
                return Base("iron_ore");
            case "iron_ingot":
                return Base("iron_bar");
            case "iron_plate":
                return Base("iron_plate");
            case "iron_bar":
                // JSON bar = 막대기. 런타임 iron_bar는 주괴.
                return Base("iron_rod");
            case "iron_ingot_lv2":
                return WithLevel("iron_bar", 2);
            case "iron_sword":
                return Base("iron_sword");
            case "iron_helmet":
                return Base("iron_helmet");
            case "iron_chestplate":
                return Base("iron_chestplate");
            case "iron_leggings":
                return Base("iron_leggings");
            case "iron_boots":
                return Base("iron_boots");
            case "iron_blade":
                return Base("greatsword_blade");
            case "mana_wand":
                return Base("mana_wand");
            case "manasteel_bar":
            case "Manasteel_ingot":
                return Base("mana_core");
            case "manasteel_sword":
                return Base("manasteel_sword");
            case "manasteel_helmet":
                return Base("manasteel_helmet");
            case "manasteel_chestplate":
                return Base("manasteel_chestplate");
            case "manasteel_leggings":
                return Base("manasteel_leggings");
            case "manasteel_boots":
                return Base("manasteel_boots");
            case "manasteel_sword_fire":
                return WithEnchants("manasteel_sword", Attr(EnchantmentId.Fire));
            case "manasteel_sword_wind":
                return WithEnchants("manasteel_sword", Attr(EnchantmentId.Wind));
            case "manasteel_sword_earth":
                return WithEnchants("manasteel_sword", Attr(EnchantmentId.Earth));
            case "manasteel_sword_water":
                return WithEnchants("manasteel_sword", Attr(EnchantmentId.Water));
            case "manasteel_chestplate_fire_proof":
                return WithEnchants(
                    "manasteel_chestplate",
                    new Enchantment(EnchantmentId.Fire, EnchantmentForm.Defense));
            case "element_scroll":
                return Base("element_scroll");
            case "scroll_explosion":
                return WithEnchants(
                    "tier2_element_scroll",
                    Attr(EnchantmentId.Fire),
                    Attr(EnchantmentId.Wind));
            case "scroll_lava":
                return WithEnchants(
                    "tier2_element_scroll",
                    Attr(EnchantmentId.Fire),
                    Attr(EnchantmentId.Earth));
            case "scroll_poison":
                return WithEnchants(
                    "tier2_element_scroll",
                    Attr(EnchantmentId.Fire),
                    Attr(EnchantmentId.Water));
            case "scroll_lightning":
                return WithEnchants(
                    "tier2_element_scroll",
                    Attr(EnchantmentId.Wind),
                    Attr(EnchantmentId.Water));
            case "scroll_nature":
                return WithEnchants(
                    "tier2_element_scroll",
                    Attr(EnchantmentId.Wind),
                    Attr(EnchantmentId.Earth));
            case "scroll_ice":
                return WithEnchants(
                    "tier2_element_scroll",
                    Attr(EnchantmentId.Earth),
                    Attr(EnchantmentId.Water));
            case "darksteel_sword":
                return Base("darksteel_sword");
            case "darksteel_helmet":
                return Base("darksteel_helmet");
            case "darksteel_chestplate":
                return Base("darksteel_chestplate");
            case "darksteel_leggings":
                return Base("darksteel_leggings");
            case "darksteel_boots":
                return Base("darksteel_boots");
            case "darksteel_ingot":
                return Base("darksteel_ingot");
            case "brightsteel_sword":
                return Base("brightsteel_sword");
            case "brightsteel_helmet":
                return Base("brightsteel_helmet");
            case "brightsteel_chestplate":
                return Base("brightsteel_chestplate");
            case "brightsteel_leggings":
                return Base("brightsteel_leggings");
            case "brightsteel_boots":
                return Base("brightsteel_boots");
            case "brightsteel_ingot":
                return Base("brightsteel_ingot");
            case "greysteel_sword":
                return Base("greysteel_sword");
            case "greysteel_helmet":
                return Base("greysteel_helmet");
            case "greysteel_chestplate":
                return Base("greysteel_chestplate");
            case "greysteel_leggings":
                return Base("greysteel_leggings");
            case "greysteel_boots":
                return Base("greysteel_boots");
            case "greysteel_ingot":
                return Base("greysteel_ingot");
            case "greysteel_battlehammer":
                return Base("greysteel_warhammer");
            case "steel_column_framwork":
                return Base("iron_pillar_frame");
            case "structural_column":
                return Base("structure_pillar");
            case "structural_girder":
                return Base("structure_beam");
            case "structural_roof":
                return Base("structure_roof");
            case "warstained_executional_greatsword":
                return Base("war_stained_executor_greatsword");
            case "magicrobe":
                return Base("mage_robe");
            case "dark_mana_wand":
                return Base("dark_magic_staff");
            case "bright_mana_wand":
                return Base("light_magic_staff");
            case "darkmana_core":
                return Base("dark_magic_core");
            case "concrete":
                return Base("concrete");
            case "gold":
            case "Gold":
                return Base("gold");
            case "fame":
            case "Fame":
                return Base("fame");
            default:
                return Base(itemCode);
        }
    }

    private static Enchantment Attr(EnchantmentId attribute)
    {
        return new Enchantment(attribute, EnchantmentForm.None);
    }

    private static ResolvedItem Base(string itemId)
    {
        return new ResolvedItem
        {
            itemId = itemId,
            level = 1,
            enchantments = Array.Empty<Enchantment>(),
        };
    }

    private static ResolvedItem WithLevel(string itemId, int level)
    {
        return new ResolvedItem
        {
            itemId = itemId,
            level = level,
            enchantments = Array.Empty<Enchantment>(),
        };
    }

    private static ResolvedItem WithEnchants(string itemId, params Enchantment[] enchantments)
    {
        return new ResolvedItem
        {
            itemId = itemId,
            level = 1,
            enchantments = enchantments ?? Array.Empty<Enchantment>(),
        };
    }
}
