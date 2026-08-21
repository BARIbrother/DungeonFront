#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Docs/08-machine-recipes.md 표대로 RecipePool SO를 만들고 기계 Prefab에 연결한다.
public static class RecipePoolBinder
{
    private const string PoolFolder = "Assets/Recipe/RecipePool";
    private const string RecipeFolder = "Assets/Recipe";
    private const string PrefabFolder = "Assets/Prefabs/Machines";

    private sealed class PoolSpec
    {
        public string assetName;
        public string[] recipeIds;
        public string[] prefabPaths;
    }

    // 상위 티어는 하위 레시피를 포함한다. Miner_3·Assembler_3은 직전 티어와 같은 풀, 더 빠르다.
    private static readonly PoolSpec[] Specs =
    {
        new PoolSpec
        {
            assetName = "RecipePool_Drill",
            prefabPaths = new[] { $"{PrefabFolder}/Miner_machine.prefab" },
            recipeIds = new[]
            {
                "drill_iron_ore",
                "drill_wood_log",
                "drill_stone",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Drill_2",
            prefabPaths = new[]
            {
                $"{PrefabFolder}/Miner_2_machine.prefab",
                $"{PrefabFolder}/Miner_3_machine.prefab",
            },
            recipeIds = new[]
            {
                "drill_iron_ore",
                "drill_wood_log",
                "drill_stone",
                "drill_mana_ore",
                "drill_blackstone_ore",
                "drill_whitestone_ore",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Smelter",
            prefabPaths = new[] { $"{PrefabFolder}/Smelter_machine.prefab" },
            recipeIds = new[]
            {
                "smelt_iron_bar",
                "smelt_mana_crystal",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Smelter_2",
            prefabPaths = new[] { $"{PrefabFolder}/Smelter_2_machine.prefab" },
            recipeIds = new[]
            {
                "smelt_iron_bar",
                "smelt_mana_crystal",
                "smelt_blackstone_ingot",
                "smelt_whitestone_ingot",
                "alloy_darksteel_ingot",
                "alloy_brightsteel_ingot",
                "refine_iron_bar_lv2",
                "refine_blackstone_ingot_lv2",
                "refine_whitestone_ingot_lv2",
                "refine_darksteel_ingot_lv2",
                "refine_brightsteel_ingot_lv2",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Smelter_3",
            prefabPaths = new[] { $"{PrefabFolder}/Smelter_3_machine.prefab" },
            recipeIds = new[]
            {
                "smelt_iron_bar",
                "smelt_mana_crystal",
                "smelt_blackstone_ingot",
                "smelt_whitestone_ingot",
                "alloy_darksteel_ingot",
                "alloy_brightsteel_ingot",
                "alloy_greysteel_ingot",
                "refine_iron_bar_lv2",
                "refine_blackstone_ingot_lv2",
                "refine_whitestone_ingot_lv2",
                "refine_darksteel_ingot_lv2",
                "refine_brightsteel_ingot_lv2",
                "refine_iron_bar_lv3",
                "refine_blackstone_ingot_lv3",
                "refine_whitestone_ingot_lv3",
                "refine_darksteel_ingot_lv3",
                "refine_brightsteel_ingot_lv3",
                "refine_greysteel_ingot_lv3",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Assembler",
            prefabPaths = new[]
            {
                $"{PrefabFolder}/Assembler_machine.prefab",
                $"{PrefabFolder}/Handmade_Assembler_Machine.prefab",
            },
            recipeIds = new[]
            {
                "craft_wood_stick",
                "craft_paper",
                "assemble_iron_plate",
                "assemble_iron_rod",
                "craft_iron_helmet",
                "craft_iron_chestplate",
                "craft_iron_leggings",
                "craft_iron_boots",
                "craft_iron_sword",
                "craft_iron_warhammer",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Assembler_2",
            prefabPaths = new[]
            {
                $"{PrefabFolder}/Assembler_2_machine.prefab",
                $"{PrefabFolder}/Assembler_3_machine.prefab",
            },
            recipeIds = new[]
            {
                "craft_wood_stick",
                "craft_paper",
                "assemble_iron_plate",
                "assemble_iron_rod",
                "craft_iron_helmet",
                "craft_iron_chestplate",
                "craft_iron_leggings",
                "craft_iron_boots",
                "craft_iron_sword",
                "craft_iron_warhammer",
                "assemble_darksteel_plate",
                "assemble_darksteel_rod",
                "craft_darksteel_helmet",
                "craft_darksteel_chestplate",
                "craft_darksteel_leggings",
                "craft_darksteel_boots",
                "craft_darksteel_sword",
                "craft_darksteel_warhammer",
                "assemble_brightsteel_plate",
                "assemble_brightsteel_rod",
                "craft_brightsteel_helmet",
                "craft_brightsteel_chestplate",
                "craft_brightsteel_leggings",
                "craft_brightsteel_boots",
                "craft_brightsteel_sword",
                "craft_brightsteel_warhammer",
                "assemble_greysteel_plate",
                "assemble_greysteel_rod",
                "craft_greysteel_helmet",
                "craft_greysteel_chestplate",
                "craft_greysteel_leggings",
                "craft_greysteel_boots",
                "craft_greysteel_sword",
                "craft_greysteel_warhammer",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_ManaExtractor",
            prefabPaths = new[] { $"{PrefabFolder}/ManaExtractor_machine.prefab" },
            recipeIds = new[]
            {
                "extract_mana_low",
                "extract_mana_mid",
                "extract_mana_high",
                "extract_mana_dungeon_master",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_ManaAssembler",
            prefabPaths = new[] { $"{PrefabFolder}/ManaHandmade_machine.prefab" },
            recipeIds = new[]
            {
                "craft_mana_core",
                "craft_mana_wand",
                "craft_manasteel_sword",
                "craft_manasteel_helmet",
                "craft_manasteel_chestplate",
                "craft_manasteel_leggings",
                "craft_manasteel_boots",
                "craft_blank_magic_scroll",
                "craft_element_scroll",
                "craft_element_form_scroll",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_ManaAssembler_2",
            prefabPaths = new[] { $"{PrefabFolder}/ManaAssembler_2_machine.prefab" },
            recipeIds = new[]
            {
                "craft_mana_core",
                "craft_mana_wand",
                "craft_manasteel_sword",
                "craft_manasteel_helmet",
                "craft_manasteel_chestplate",
                "craft_manasteel_leggings",
                "craft_manasteel_boots",
                "craft_blank_magic_scroll",
                "craft_element_scroll",
                "craft_element_form_scroll",
                "craft_blank_tier2_magic_scroll",
                "craft_tier2_element_scroll",
                "craft_tier2_element_form_scroll",
                "craft_mage_robe",
                "craft_dark_magic_core",
                "craft_dark_mage_robe",
                "craft_dark_magic_staff",
                "craft_light_magic_core",
                "craft_light_mage_robe",
                "craft_light_magic_staff",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_ManaAssembler_3",
            prefabPaths = new[] { $"{PrefabFolder}/ManaAssembler_3_machine.prefab" },
            recipeIds = new[]
            {
                "craft_mana_core",
                "craft_mana_wand",
                "craft_manasteel_sword",
                "craft_manasteel_helmet",
                "craft_manasteel_chestplate",
                "craft_manasteel_leggings",
                "craft_manasteel_boots",
                "craft_blank_magic_scroll",
                "craft_element_scroll",
                "craft_element_form_scroll",
                "craft_blank_tier2_magic_scroll",
                "craft_tier2_element_scroll",
                "craft_tier2_element_form_scroll",
                "craft_mage_robe",
                "craft_dark_magic_core",
                "craft_dark_mage_robe",
                "craft_dark_magic_staff",
                "craft_light_magic_core",
                "craft_light_mage_robe",
                "craft_light_magic_staff",
                "craft_ritual_scroll",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Enchanting",
            prefabPaths = new[] { $"{PrefabFolder}/Enchanting_machine.prefab" },
            recipeIds = new[]
            {
                "enchant_element_fire",
                "enchant_element_water",
                "enchant_element_wind",
                "enchant_element_earth",
                "enchant_form_fire",
                "enchant_form_water",
                "enchant_form_wind",
                "enchant_form_earth",
                "enchant_tier2_explosion",
                "enchant_tier2_lava",
                "enchant_tier2_poison",
                "enchant_tier2_nature",
                "enchant_tier2_lightning",
                "enchant_tier2_ice",
                "enchant_tier2_form_explosion",
                "enchant_tier2_form_lava",
                "enchant_tier2_form_poison",
                "enchant_tier2_form_nature",
                "enchant_tier2_form_lightning",
                "enchant_tier2_form_ice",
                "enchant_apply_scroll",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Foundry",
            prefabPaths = new[] { $"{PrefabFolder}/Foundry_machine.prefab" },
            recipeIds = new[]
            {
                "craft_concrete",
                "craft_iron_pillar_frame",
                "craft_iron_beam_frame",
                "craft_iron_roof_frame",
                "craft_structure_pillar",
                "craft_structure_beam",
                "craft_structure_roof",
                "craft_altar",
                "craft_greatsword_blade",
                "craft_ritual_iron_greatsword",
            },
        },
        new PoolSpec
        {
            assetName = "RecipePool_Altar",
            prefabPaths = new[] { $"{PrefabFolder}/Altar_machine.prefab" },
            recipeIds = new[]
            {
                "craft_sword_ritual",
                "craft_war_ritual",
                "craft_executor_greatsword",
                "craft_war_stained_executor_greatsword",
            },
        },
    };

    [MenuItem("DungeonFront/Bind Recipe Pools")]
    public static void Bind()
    {
        EnsureFolder(PoolFolder);

        Dictionary<string, Recipe> recipesById = BuildRecipeLookup();
        int boundPools = 0;
        int boundPrefabs = 0;
        int missingRecipes = 0;

        for (int i = 0; i < Specs.Length; i++)
        {
            PoolSpec spec = Specs[i];
            List<Recipe> resolved = new List<Recipe>(spec.recipeIds.Length);
            for (int r = 0; r < spec.recipeIds.Length; r++)
            {
                string recipeId = spec.recipeIds[r];
                if (!recipesById.TryGetValue(recipeId, out Recipe recipe) || recipe == null)
                {
                    missingRecipes++;
                    continue;
                }

                resolved.Add(recipe);
            }

            RecipePool pool = UpsertPool(spec.assetName, resolved);
            boundPools++;

            for (int p = 0; p < spec.prefabPaths.Length; p++)
            {
                if (AssignPoolToPrefab(spec.prefabPaths[p], pool))
                {
                    boundPrefabs++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[RecipePoolBinder] pools={boundPools}/{Specs.Length}, prefabs={boundPrefabs}, missingRecipes={missingRecipes}");
    }

    private static Dictionary<string, Recipe> BuildRecipeLookup()
    {
        var lookup = new Dictionary<string, Recipe>();
        string[] guids = AssetDatabase.FindAssets("t:Recipe", new[] { RecipeFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Recipe recipe = AssetDatabase.LoadAssetAtPath<Recipe>(path);
            if (recipe == null || string.IsNullOrEmpty(recipe.id))
            {
                continue;
            }

            lookup[recipe.id] = recipe;
        }

        return lookup;
    }

    private static RecipePool UpsertPool(string assetName, List<Recipe> recipes)
    {
        string assetPath = $"{PoolFolder}/{assetName}.asset";
        RecipePool pool = AssetDatabase.LoadAssetAtPath<RecipePool>(assetPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<RecipePool>();
            AssetDatabase.CreateAsset(pool, assetPath);
        }

        SerializedObject so = new SerializedObject(pool);
        SerializedProperty recipesProp = so.FindProperty("recipes");
        recipesProp.arraySize = recipes.Count;
        for (int i = 0; i < recipes.Count; i++)
        {
            recipesProp.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static bool AssignPoolToPrefab(string prefabPath, RecipePool pool)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[RecipePoolBinder] Prefab이 없습니다: {prefabPath}");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Machine machine = root.GetComponent<Machine>();
            if (machine == null)
            {
                Debug.LogWarning($"[RecipePoolBinder] Machine이 없습니다: {prefabPath}");
                return false;
            }

            SerializedObject so = new SerializedObject(machine);
            so.FindProperty("AvailableRecipes").objectReferenceValue = pool;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        if (!string.IsNullOrEmpty(parent))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
