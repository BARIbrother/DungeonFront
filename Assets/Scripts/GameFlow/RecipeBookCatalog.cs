using System;
using System.Collections.Generic;

// 레시피북 챕터·식. Docs/06-recipe.md · 08-machine-recipes.md 기준.
public static class RecipeBookCatalog
{
    public readonly struct Stack
    {
        public readonly string itemId;
        public readonly int count;
        public readonly int level;

        public Stack(string itemId, int count = 1, int level = 0)
        {
            this.itemId = itemId;
            this.count = count;
            this.level = level;
        }
    }

    public sealed class RecipeLine
    {
        public readonly string recipeId;
        public readonly string machineLabel;
        public readonly Stack[] inputs;
        public readonly Stack[] outputs;
        public readonly int manaCost;

        public RecipeLine(string recipeId, string machineLabel, Stack[] inputs, Stack[] outputs)
        {
            this.recipeId = recipeId;
            this.machineLabel = machineLabel;
            this.outputs = outputs ?? Array.Empty<Stack>();
            SplitManaInputs(inputs, out this.inputs, out this.manaCost);
        }
    }

    public sealed class Section
    {
        public readonly string title;
        public readonly List<RecipeLine> recipes;

        public Section(string title, List<RecipeLine> recipes)
        {
            this.title = title;
            this.recipes = recipes;
        }
    }

    public static readonly List<Section> Sections = new List<Section>
    {
        new Section("나무", new List<RecipeLine>
        {
            Line("craft_wood_stick", "제작기", In(S("wood_log")), S("wood_stick", 8)),
            Line("craft_paper", "제작기", In(S("wood_log")), S("paper", 4)),
        }),
        new Section("철", new List<RecipeLine>
        {
            Line("smelt_iron_bar", "용광로", In(S("iron_ore")), S("iron_bar")),
            Line("assemble_iron_plate", "제작기", In(S("iron_bar")), S("iron_plate")),
            Line("assemble_iron_rod", "제작기", In(S("iron_bar")), S("iron_rod", 2)),
            Line("craft_iron_chestplate", "제작기", In(S("iron_plate", 4), S("iron_bar", 6)), S("iron_chestplate")),
            Line("craft_iron_helmet", "제작기", In(S("iron_plate", 2), S("iron_bar", 4)), S("iron_helmet")),
            Line("craft_iron_leggings", "제작기", In(S("iron_plate", 2), S("iron_bar", 5)), S("iron_leggings")),
            Line("craft_iron_boots", "제작기", In(S("iron_plate", 2), S("iron_bar", 2)), S("iron_boots")),
            Line("craft_iron_sword", "제작기", In(S("iron_rod", 2), S("wood_stick", 2)), S("iron_sword")),
            Line("craft_iron_warhammer", "제작기", In(S("iron_plate", 4), S("iron_rod", 2), S("wood_stick", 2)), S("iron_warhammer")),
        }),
        new Section("기초 마력석", new List<RecipeLine>
        {
            Line("smelt_mana_crystal", "용광로", In(S("mana_ore")), S("mana_crystal")),
            Line("craft_mana_core", "마나 제작기", In(S("mana_crystal"), S("iron_bar")), S("mana_core")),
            Line("craft_mana_wand", "마나 제작기", In(S("mana_core"), S("wood_stick", 2)), S("mana_wand")),
            Line("craft_manasteel_sword", "마나 제작기", In(S("iron_sword"), S("mana_core", 2)), S("manasteel_sword")),
            Line("craft_manasteel_helmet", "마나 제작기", In(S("iron_helmet"), S("mana_core", 2)), S("manasteel_helmet")),
            Line("craft_manasteel_chestplate", "마나 제작기", In(S("iron_chestplate"), S("mana_core", 4)), S("manasteel_chestplate")),
            Line("craft_manasteel_leggings", "마나 제작기", In(S("iron_leggings"), S("mana_core", 3)), S("manasteel_leggings")),
            Line("craft_manasteel_boots", "마나 제작기", In(S("iron_boots"), S("mana_core", 2)), S("manasteel_boots")),
        }),
        new Section("마나 포집", new List<RecipeLine>
        {
            Line("extract_mana_low", "마나 포집기", None, S("low_monster_mana_essence")),
            Line("extract_mana_mid", "마나 포집기", None, S("mid_monster_mana_essence")),
            Line("extract_mana_high", "마나 포집기", None, S("high_monster_mana_essence")),
            Line("extract_mana_dungeon_master", "마나 포집기", None, S("dungeon_master_essence")),
        }),
        new Section("마법 스크롤", new List<RecipeLine>
        {
            Line("craft_blank_magic_scroll", "마나 제작기", In(S("paper"), S("high_monster_mana_essence")), S("blank_magic_scroll")),
            Line("craft_element_scroll", "마나 제작기", In(S("blank_magic_scroll"), S("high_monster_mana_essence", 3)), S("element_scroll")),
            Line("craft_element_form_scroll", "마나 제작기", In(S("element_scroll"), S("high_monster_mana_essence")), S("element_form_scroll")),
            Line("craft_blank_tier2_magic_scroll", "마나 제작기", In(S("blank_magic_scroll"), S("mana_core"), S("high_monster_mana_essence")), S("blank_tier2_magic_scroll")),
            Line("craft_tier2_element_scroll", "마나 제작기", In(S("blank_tier2_magic_scroll"), S("dungeon_master_essence")), S("tier2_element_scroll")),
            Line("craft_tier2_element_form_scroll", "마나 제작기", In(S("tier2_element_scroll"), S("high_monster_mana_essence", 2), S("mid_monster_mana_essence")), S("tier2_element_form_scroll")),
        }),
        new Section("의식", new List<RecipeLine>
        {
            Line("craft_ritual_scroll", "마나 제작기", In(S("blank_tier2_magic_scroll"), S("dungeon_master_essence", 2)), S("ritual_scroll")),
            Line("craft_sword_ritual", "제단", In(S("ritual_scroll"), S("iron_sword", 10), S("darksteel_sword", 10), S("brightsteel_sword", 10), S("greysteel_sword", 10)), S("sword_ritual")),
            Line("craft_war_ritual", "제단", In(S("sword_ritual", 10)), S("war_ritual")),
            Line("craft_executor_greatsword", "제단", In(S("ritual_iron_greatsword"), S("sword_ritual")), S("executor_greatsword")),
            Line("craft_war_stained_executor_greatsword", "제단", In(S("executor_greatsword"), S("war_ritual")), S("war_stained_executor_greatsword")),
        }),
        new Section("칠흑석", new List<RecipeLine>
        {
            Line("smelt_blackstone_ingot", "용광로", In(S("blackstone_ore")), S("blackstone_ingot")),
            Line("alloy_darksteel_ingot", "용광로", In(S("iron_bar"), S("blackstone_ingot")), S("darksteel_ingot")),
            Line("assemble_darksteel_plate", "제작기", In(S("darksteel_ingot")), S("darksteel_plate")),
            Line("assemble_darksteel_rod", "제작기", In(S("darksteel_ingot")), S("darksteel_rod", 2)),
            Line("craft_darksteel_sword", "제작기", In(S("iron_sword"), S("darksteel_ingot", 2), S("darksteel_rod", 2)), S("darksteel_sword")),
            Line("craft_darksteel_warhammer", "제작기", In(S("iron_warhammer"), S("darksteel_plate", 4), S("darksteel_rod", 2)), S("darksteel_warhammer")),
            Line("craft_darksteel_helmet", "제작기", In(S("iron_helmet"), S("darksteel_plate", 3)), S("darksteel_helmet")),
            Line("craft_darksteel_chestplate", "제작기", In(S("iron_chestplate"), S("darksteel_plate", 6)), S("darksteel_chestplate")),
            Line("craft_darksteel_leggings", "제작기", In(S("iron_leggings"), S("darksteel_plate", 5)), S("darksteel_leggings")),
            Line("craft_darksteel_boots", "제작기", In(S("iron_boots"), S("darksteel_plate", 2)), S("darksteel_boots")),
        }),
        new Section("순백석", new List<RecipeLine>
        {
            Line("smelt_whitestone_ingot", "용광로", In(S("whitestone_ore")), S("whitestone_ingot")),
            Line("alloy_brightsteel_ingot", "용광로", In(S("iron_bar"), S("whitestone_ingot")), S("brightsteel_ingot")),
            Line("assemble_brightsteel_plate", "제작기", In(S("brightsteel_ingot")), S("brightsteel_plate")),
            Line("assemble_brightsteel_rod", "제작기", In(S("brightsteel_ingot")), S("brightsteel_rod", 2)),
            Line("craft_brightsteel_sword", "제작기", In(S("iron_sword"), S("brightsteel_ingot", 2), S("brightsteel_rod", 2)), S("brightsteel_sword")),
            Line("craft_brightsteel_warhammer", "제작기", In(S("iron_warhammer"), S("brightsteel_plate", 4), S("brightsteel_rod", 2)), S("brightsteel_warhammer")),
            Line("craft_brightsteel_helmet", "제작기", In(S("iron_helmet"), S("brightsteel_plate", 3)), S("brightsteel_helmet")),
            Line("craft_brightsteel_chestplate", "제작기", In(S("iron_chestplate"), S("brightsteel_plate", 6)), S("brightsteel_chestplate")),
            Line("craft_brightsteel_leggings", "제작기", In(S("iron_leggings"), S("brightsteel_plate", 5)), S("brightsteel_leggings")),
            Line("craft_brightsteel_boots", "제작기", In(S("iron_boots"), S("brightsteel_plate", 2)), S("brightsteel_boots")),
        }),
        new Section("고급 마법", new List<RecipeLine>
        {
            Line("craft_mage_robe", "마나 제작기", In(S("manasteel_chestplate"), S("manasteel_leggings"), S("dungeon_master_essence", 3)), S("mage_robe")),
            Line("craft_dark_magic_core", "마나 제작기", In(S("mana_core", 1, 2), S("blackstone_ingot", 1, 2)), S("dark_magic_core")),
            Line("craft_dark_mage_robe", "마나 제작기", In(S("mage_robe"), S("dark_magic_core")), S("dark_mage_robe")),
            Line("craft_dark_magic_staff", "마나 제작기", In(S("mana_wand"), S("dark_magic_core")), S("dark_magic_staff")),
            Line("craft_light_magic_core", "마나 제작기", In(S("mana_core", 1, 2), S("whitestone_ingot", 1, 2)), S("light_magic_core")),
            Line("craft_light_mage_robe", "마나 제작기", In(S("mage_robe"), S("light_magic_core")), S("light_mage_robe")),
            Line("craft_light_magic_staff", "마나 제작기", In(S("mana_wand"), S("light_magic_core")), S("light_magic_staff")),
        }),
        new Section("진강", new List<RecipeLine>
        {
            Line("alloy_greysteel_ingot", "용광로", In(S("iron_bar", 1, 2), S("blackstone_ingot", 1, 2), S("whitestone_ingot", 1, 2)), S("greysteel_ingot")),
            Line("assemble_greysteel_plate", "제작기", In(S("greysteel_ingot")), S("greysteel_plate")),
            Line("assemble_greysteel_rod", "제작기", In(S("greysteel_ingot")), S("greysteel_rod", 2)),
            Line("craft_greysteel_sword", "제작기", In(S("darksteel_sword"), S("brightsteel_sword"), S("greysteel_rod", 2)), S("greysteel_sword")),
            Line("craft_greysteel_warhammer", "제작기", In(S("darksteel_warhammer"), S("brightsteel_warhammer"), S("greysteel_rod", 2)), S("greysteel_warhammer")),
            Line("craft_greysteel_helmet", "제작기", In(S("darksteel_helmet"), S("brightsteel_helmet"), S("greysteel_plate")), S("greysteel_helmet")),
            Line("craft_greysteel_chestplate", "제작기", In(S("darksteel_chestplate"), S("brightsteel_chestplate"), S("greysteel_plate", 2)), S("greysteel_chestplate")),
            Line("craft_greysteel_leggings", "제작기", In(S("darksteel_leggings"), S("brightsteel_leggings"), S("greysteel_plate", 2)), S("greysteel_leggings")),
            Line("craft_greysteel_boots", "제작기", In(S("darksteel_boots"), S("brightsteel_boots"), S("greysteel_plate")), S("greysteel_boots")),
        }),
        new Section("건축 트리", new List<RecipeLine>
        {
            Line("craft_concrete", "주조소", In(S("stone")), S("concrete")),
            Line("craft_iron_pillar_frame", "주조소", In(S("iron_bar", 10), S("iron_rod", 20)), S("iron_pillar_frame")),
            Line("craft_iron_beam_frame", "주조소", In(S("iron_bar", 10), S("iron_rod", 20)), S("iron_beam_frame")),
            Line("craft_iron_roof_frame", "주조소", In(S("iron_bar", 20), S("iron_rod", 50)), S("iron_roof_frame")),
            Line("craft_structure_roof", "주조소", In(S("iron_roof_frame"), S("concrete", 150)), S("structure_roof")),
            Line("craft_structure_beam", "주조소", In(S("iron_beam_frame"), S("concrete", 50)), S("structure_beam")),
            Line("craft_structure_pillar", "주조소", In(S("iron_pillar_frame"), S("concrete", 50)), S("structure_pillar")),
            Line("craft_altar", "주조소", In(S("structure_pillar", 8), S("structure_beam", 2), S("structure_roof")), S("altar")),
            Line("craft_greatsword_blade", "주조소", In(S("iron_plate", 20, 3), S("iron_rod", 10, 3)), S("greatsword_blade")),
            Line("craft_ritual_iron_greatsword", "주조소", In(S("greatsword_blade"), S("iron_rod", 10, 3), S("iron_bar", 10, 3)), S("ritual_iron_greatsword")),
        }),
    };

    private static readonly Stack[] None = Array.Empty<Stack>();

    public static bool TryFindOutput(string itemId, out int chapterIndex, out int recipeIndex)
    {
        chapterIndex = -1;
        recipeIndex = -1;
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        for (int c = 0; c < Sections.Count; c++)
        {
            List<RecipeLine> recipes = Sections[c].recipes;
            for (int r = 0; r < recipes.Count; r++)
            {
                Stack[] outputs = recipes[r].outputs;
                for (int o = 0; o < outputs.Length; o++)
                {
                    if (outputs[o].itemId == itemId)
                    {
                        chapterIndex = c;
                        recipeIndex = r;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static string ItemName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return string.Empty;
        }

        ItemManager manager = UnityEngine.Object.FindAnyObjectByType<ItemManager>();
        ItemDefinition definition = manager != null ? manager.Get(itemId) : null;
        if (definition != null && !string.IsNullOrEmpty(definition.displayName))
        {
            return definition.displayName;
        }

        return itemId switch
        {
            "wood_log" => "나무 원목",
            "wood_stick" => "나무 막대기",
            "paper" => "종이",
            "iron_ore" => "철광석",
            "iron_bar" => "철 주괴",
            "iron_plate" => "철 판",
            "iron_rod" => "철 막대기",
            "iron_chestplate" => "철 흉갑",
            "iron_helmet" => "철 투구",
            "iron_leggings" => "철 각반",
            "iron_boots" => "철 부츠",
            "iron_sword" => "철 검",
            "iron_warhammer" => "철 전투 망치",
            "mana_ore" => "마력석 광석",
            "mana_crystal" => "마력석 결정",
            "mana_core" => "마력 코어",
            "mana_wand" => "마나 완드",
            "manasteel_sword" => "마나강 검",
            "manasteel_helmet" => "마나강 투구",
            "manasteel_chestplate" => "마나강 흉갑",
            "manasteel_leggings" => "마나강 각반",
            "manasteel_boots" => "마나강 부츠",
            "low_monster_mana_essence" => "하급 마나 정수",
            "mid_monster_mana_essence" => "중급 마나 정수",
            "high_monster_mana_essence" => "상급 마나 정수",
            "dungeon_master_essence" => "던전의 주인의 정수",
            "blank_magic_scroll" => "빈 마법 스크롤",
            "element_scroll" => "원소 스크롤",
            "element_form_scroll" => "원소 형태 스크롤",
            "blank_tier2_magic_scroll" => "빈 2단계 마법 스크롤",
            "tier2_element_scroll" => "2단계 원소 스크롤",
            "tier2_element_form_scroll" => "2단계 원소 형태 스크롤",
            "ritual_scroll" => "의식 스크롤",
            "sword_ritual" => "검의 의식",
            "war_ritual" => "전쟁의 의식",
            "ritual_iron_greatsword" => "의식용 철제 대검",
            "executor_greatsword" => "집행자의 대검",
            "war_stained_executor_greatsword" => "전쟁에 물든 집행자의 대검",
            "blackstone_ore" => "칠흑석 광석",
            "blackstone_ingot" => "칠흑석 주괴",
            "darksteel_ingot" => "흑강 주괴",
            "darksteel_plate" => "흑강 판",
            "darksteel_rod" => "흑강 막대기",
            "darksteel_sword" => "흑강 검",
            "darksteel_warhammer" => "흑강 전투 망치",
            "darksteel_helmet" => "흑강 투구",
            "darksteel_chestplate" => "흑강 흉갑",
            "darksteel_leggings" => "흑강 각반",
            "darksteel_boots" => "흑강 부츠",
            "whitestone_ore" => "순백석 광석",
            "whitestone_ingot" => "순백석 주괴",
            "brightsteel_ingot" => "백강 주괴",
            "brightsteel_plate" => "백강 판",
            "brightsteel_rod" => "백강 막대기",
            "brightsteel_sword" => "백강 검",
            "brightsteel_warhammer" => "백강 전투 망치",
            "brightsteel_helmet" => "백강 투구",
            "brightsteel_chestplate" => "백강 흉갑",
            "brightsteel_leggings" => "백강 각반",
            "brightsteel_boots" => "백강 부츠",
            "mage_robe" => "마술사의 로브",
            "dark_magic_core" => "흑마법 코어",
            "dark_mage_robe" => "흑마술사의 로브",
            "dark_magic_staff" => "흑마술 지팡이",
            "light_magic_core" => "백마법 코어",
            "light_mage_robe" => "백마술사의 로브",
            "light_magic_staff" => "백마술 지팡이",
            "greysteel_ingot" => "진강 주괴",
            "greysteel_plate" => "진강 판",
            "greysteel_rod" => "진강 막대기",
            "greysteel_sword" => "진강 검",
            "greysteel_warhammer" => "진강 전투 망치",
            "greysteel_helmet" => "진강 투구",
            "greysteel_chestplate" => "진강 흉갑",
            "greysteel_leggings" => "진강 각반",
            "greysteel_boots" => "진강 부츠",
            "stone" => "돌",
            "concrete" => "콘크리트",
            "iron_pillar_frame" => "철제 기둥 뼈대",
            "iron_beam_frame" => "철제 대들보 뼈대",
            "iron_roof_frame" => "철제 지붕 뼈대",
            "structure_roof" => "구조물 지붕",
            "structure_beam" => "구조물 대들보",
            "structure_pillar" => "구조물 기둥",
            "altar" => "제단",
            "greatsword_blade" => "대검 날",
            _ => itemId,
        };
    }

    private static RecipeLine Line(string recipeId, string machineLabel, Stack[] inputs, params Stack[] outputs)
    {
        return new RecipeLine(recipeId, machineLabel, inputs, outputs);
    }

    private static void SplitManaInputs(Stack[] inputs, out Stack[] materials, out int manaCost)
    {
        manaCost = 0;
        if (inputs == null || inputs.Length == 0)
        {
            materials = Array.Empty<Stack>();
            return;
        }

        var kept = new List<Stack>(inputs.Length);
        for (int i = 0; i < inputs.Length; i++)
        {
            Stack stack = inputs[i];
            if (ManaEssence.TryGetValue(stack.itemId, out int unit))
            {
                manaCost += unit * stack.count;
                continue;
            }

            kept.Add(stack);
        }

        materials = kept.Count == 0 ? Array.Empty<Stack>() : kept.ToArray();
    }

    private static Stack[] In(params Stack[] stacks)
    {
        return stacks;
    }

    private static Stack S(string itemId, int count = 1, int level = 0)
    {
        return new Stack(itemId, count, level);
    }
}
