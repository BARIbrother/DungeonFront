using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 메뉴:
// Tools > DungeonFront > Quest > Rebuild Demo Assets
// 팀 Factory 씬은 수정하지 않고 Dev2 프리팹과 독립 테스트 씬만 다시 만든다.
public static class QuestSystemDemoBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Quest";
    private const string PrefabPath = PrefabFolder + "/QuestSystemRoot.prefab";
    private const string DemoScenePath = "Assets/Scenes/QuestSystemDemo.unity";
    private const string ShopCatalogPath =
        "Assets/Data/Quest/Week3ShopCatalog.asset";

    [MenuItem("Tools/DungeonFront/Quest/Rebuild Demo Assets")]
    public static void RebuildDemoAssets()
    {
        EnsureFolder("Assets/Prefabs", "Quest");
        EnsureFolder("Assets/Data", "Quest");
        ShopCatalog catalog = BuildShopCatalog();
        GameObject prefab = BuildRootPrefab(catalog);
        BuildDemoScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestDemoBuilder] 생성 완료: {PrefabPath}, {DemoScenePath}");
    }

    [MenuItem("Tools/DungeonFront/Quest/Rebuild Integration Prefab Only")]
    public static void RebuildIntegrationPrefabOnly()
    {
        EnsureFolder("Assets/Prefabs", "Quest");
        EnsureFolder("Assets/Data", "Quest");
        ShopCatalog catalog = BuildShopCatalog();
        BuildRootPrefab(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[QuestDemoBuilder] 기존 씬 연결용 프리팹 생성 완료: {PrefabPath}");
    }

    private static GameObject BuildRootPrefab(ShopCatalog catalog)
    {
        var root = new GameObject("QuestSystem_Dev2");
        QuestManager manager = root.AddComponent<QuestManager>();
        Week3EconomyService economy = root.AddComponent<Week3EconomyService>();
        UnlockManager unlockManager = root.AddComponent<UnlockManager>();
        ShopUI shopUI = root.AddComponent<ShopUI>();
        GameOverController gameOver = root.AddComponent<GameOverController>();
        QuestProgressionService progression = root.AddComponent<QuestProgressionService>();
        QuestPool pool = root.AddComponent<QuestPool>();
        QuestDeadlineController deadline = root.AddComponent<QuestDeadlineController>();
        QuestSaveProvider save = root.AddComponent<QuestSaveProvider>();
        PerpetualQuestService perpetual = root.AddComponent<PerpetualQuestService>();
        QuestSystemDebugPanel debugPanel = root.AddComponent<QuestSystemDebugPanel>();

        ConfigurePool(pool, manager, progression);
        SetObject(deadline, "questManager", manager);
        SetObject(deadline, "economy", economy);
        SetObject(deadline, "gameOverController", gameOver);
        SetObject(save, "questManager", manager);
        SetObject(save, "questPool", pool);
        SetObject(progression, "questManager", manager);
        SetObject(perpetual, "economy", economy);
        ConfigureUnlockManager(unlockManager, economy);
        SetObject(shopUI, "catalog", catalog);
        SetObject(shopUI, "economy", economy);
        SetObject(shopUI, "unlockManager", unlockManager);
        SetObject(debugPanel, "questManager", manager);
        SetObject(debugPanel, "questPool", pool);
        SetObject(debugPanel, "economy", economy);
        SetObject(debugPanel, "deadlineController", deadline);
        SetObject(debugPanel, "saveProvider", save);
        SetObject(debugPanel, "perpetualService", perpetual);
        SetObject(debugPanel, "unlockManager", unlockManager);
        SetObject(debugPanel, "shopUI", shopUI);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static ShopCatalog BuildShopCatalog()
    {
        ShopCatalog catalog =
            AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            AssetDatabase.CreateAsset(catalog, ShopCatalogPath);
        }

        catalog.entries = new[]
        {
            new ShopEntry
            {
                entryId = "iron_ore_single",
                displayName = "철광석 x1",
                price = 10,
                count = 1,
                item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(
                    "Assets/ItemDefinition/Iron_ore.asset")
            },
            new ShopEntry
            {
                entryId = "altar_1",
                displayName = "제단",
                price = 400,
                count = 1,
                machineDefId = "Altar_1",
                machineDefinition = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(
                    "Assets/ItemDefinition/MachineDef/Altar_1.asset")
            },
            new ShopEntry
            {
                entryId = "foundry_1",
                displayName = "주조소",
                price = 600,
                count = 1,
                machineDefId = "Foundry_1",
                machineDefinition = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(
                    "Assets/ItemDefinition/MachineDef/Foundry_1.asset")
            }
        };

        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void ConfigureUnlockManager(
        UnlockManager unlockManager,
        Week3EconomyService economy)
    {
        var serialized = new SerializedObject(unlockManager);
        serialized.FindProperty("economy").objectReferenceValue = economy;

        SerializedProperty rules = serialized.FindProperty("rules");
        rules.arraySize = 2;
        SetUnlockRule(rules, 0, "Altar_1", 350);
        SetUnlockRule(rules, 1, "Foundry_1", 500);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetUnlockRule(
        SerializedProperty rules,
        int index,
        string machineDefId,
        int requiredReputation)
    {
        SerializedProperty element = rules.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("machineDefId").stringValue = machineDefId;
        element.FindPropertyRelative("requiredReputation").intValue =
            requiredReputation;
    }

    private static void ConfigurePool(
        QuestPool pool,
        QuestManager manager,
        QuestProgressionService progression)
    {
        var serialized = new SerializedObject(pool);
        serialized.FindProperty("questJson").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Scripts/Quest/examplequests.json");
        serialized.FindProperty("questLineJson").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Data/Quest/questline.json");
        serialized.FindProperty("questManager").objectReferenceValue = manager;
        serialized.FindProperty("progression").objectReferenceValue = progression;

        SerializedProperty mappings = serialized.FindProperty("dict");
        mappings.arraySize = 4;
        SetMapping(mappings, 0, "iron_ore", "Assets/ItemDefinition/Iron_ore.asset");
        SetMapping(mappings, 1, "iron_plate", "Assets/ItemDefinition/Iron_plate.asset");
        SetMapping(mappings, 2, "gold", "Assets/ItemDefinition/Gold.asset");
        SetMapping(mappings, 3, "fame", "Assets/ItemDefinition/Fame.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetMapping(
        SerializedProperty mappings,
        int index,
        string itemId,
        string assetPath)
    {
        SerializedProperty element = mappings.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("itemId").stringValue = itemId;
        element.FindPropertyRelative("item").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
    }

    private static void BuildDemoScene(GameObject prefab)
    {
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
        cameraObject.tag = "MainCamera";

        new GameObject("GameSessionState").AddComponent<GameSessionState>();
        new GameObject("PlayerInventory").AddComponent<PlayerInventory>();
        new GameObject("QuestDemoSceneController").AddComponent<QuestDemoSceneController>();
        PrefabUtility.InstantiatePrefab(prefab, scene);

        EditorSceneManager.SaveScene(scene, DemoScenePath);
    }

    private static void SetObject(
        Object target,
        string propertyName,
        Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
