#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Week3 기계 Prefab·ItemDef_Machine·MachineDatabase·Contracts를 생성·갱신한다.
public static class MachineAssetGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/Machines";
    private const string MachineDefFolder = "Assets/ItemDefinition/MachineDef";
    private const string ContractsFolder = "Assets/Data/Contracts/Machines";
    private const string DatabasePath = "Assets/ItemDefinition/MachineDef/MachineDatabase.asset";
    private const string AutoGenerateSessionKey = "DungeonFront.MachineAssetGenerator.AutoDone.Tiers23";

    private static readonly Color AssemblerTint = new Color(0.75f, 0.85f, 0.75f, 1f);
    private static readonly Color ManaTint = new Color(0.55f, 0.7f, 1f, 1f);
    private static readonly Color EnchantTint = new Color(0.85f, 0.55f, 1f, 1f);
    private static readonly Color AltarTint = new Color(1f, 0.85f, 0.45f, 1f);
    private static readonly Color FoundryTint = new Color(0.85f, 0.55f, 0.4f, 1f);
    private static readonly Color StorageTint = new Color(0.7f, 0.65f, 0.55f, 1f);
    private static readonly Color ManaStorageTint = new Color(0.45f, 0.55f, 0.9f, 1f);

    [InitializeOnLoadMethod]
    private static void AutoGenerateOncePerSession()
    {
        if (SessionState.GetBool(AutoGenerateSessionKey, false))
        {
            return;
        }

        // 스크립트 컴파일·메타 생성 직후 한 번 Prefab/SO를 맞춘다.
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(AutoGenerateSessionKey, false))
            {
                return;
            }

            if (!File.Exists(Path.Combine(Application.dataPath, "Prefabs/Machines/Assembler_machine.prefab")))
            {
                return;
            }

            Generate();
        };
    }

    [MenuItem("DungeonFront/Generate Week3 Machine Assets")]
    public static void Generate()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MachineDefFolder);
        EnsureFolder("Assets/Data");
        EnsureFolder("Assets/Data/Contracts");
        EnsureFolder(ContractsFolder);

        // 기존 Prefab을 템플릿으로 쓴다. Machine_placeholder는 Smelter와 중복이므로 쓰지 않는다.
        GameObject assemblerTemplate = LoadPrefab($"{PrefabFolder}/Assembler_machine.prefab");
        GameObject handmadeTemplate = LoadPrefab($"{PrefabFolder}/Handmade_Assembler_Machine.prefab");
        GameObject smelterPrefab = LoadPrefab($"{PrefabFolder}/Smelter_machine.prefab");
        GameObject minerPrefab = LoadPrefab($"{PrefabFolder}/Miner_machine.prefab");
        GameObject beltPrefab = LoadPrefab($"{PrefabFolder}/ConveyerBelt_machine.prefab");

        if (assemblerTemplate == null || handmadeTemplate == null || smelterPrefab == null
            || minerPrefab == null || beltPrefab == null)
        {
            Debug.LogError("[MachineAssetGenerator] 기계 Prefab이 없어 생성할 수 없습니다.");
            return;
        }

        DeleteAssetIfExists($"{PrefabFolder}/Machine_placeholder.prefab");

        smelterPrefab = EnsureClonedPrefab(
            smelterPrefab,
            $"{PrefabFolder}/Smelter_machine.prefab",
            "Smelter_machine",
            typeof(SmelterMachine),
            AssemblerTint);

        GameObject manaExtractorPrefab = EnsureClonedPrefab(
            assemblerTemplate,
            $"{PrefabFolder}/ManaExtractor_machine.prefab",
            "ManaExtractor_machine",
            typeof(ManaExtractorMachine),
            ManaTint);

        GameObject enchantingPrefab = EnsureClonedPrefab(
            assemblerTemplate,
            $"{PrefabFolder}/Enchanting_machine.prefab",
            "Enchanting_machine",
            typeof(EnchantingMachine),
            EnchantTint);

        GameObject manaHandmadePrefab = EnsureClonedPrefab(
            handmadeTemplate,
            $"{PrefabFolder}/ManaHandmade_machine.prefab",
            "ManaHandmade_machine",
            typeof(ManaHandmadeMachine),
            ManaTint);

        GameObject altarPrefab = EnsureClonedPrefab(
            assemblerTemplate,
            $"{PrefabFolder}/Altar_machine.prefab",
            "Altar_machine",
            typeof(AltarMachine),
            AltarTint);

        GameObject foundryPrefab = EnsureClonedPrefab(
            assemblerTemplate,
            $"{PrefabFolder}/Foundry_machine.prefab",
            "Foundry_machine",
            typeof(FoundryMachine),
            FoundryTint);

        GameObject extractorPrefab = EnsureClonedPrefab(
            beltPrefab,
            $"{PrefabFolder}/Extractor_machine.prefab",
            "Extractor_machine",
            typeof(Extractor),
            StorageTint);

        GameObject warehousePrefab = EnsureClonedPrefab(
            assemblerTemplate,
            $"{PrefabFolder}/Warehouse_machine.prefab",
            "Warehouse_machine",
            typeof(WarehouseMachine),
            StorageTint);

        GameObject manaStoragePrefab = EnsureClonedPrefab(
            assemblerTemplate,
            $"{PrefabFolder}/ManaStorage_machine.prefab",
            "ManaStorage_machine",
            typeof(ManaStorageMachine),
            ManaStorageTint);

        const int WorkSpeedT1 = 10;
        const int WorkSpeedT2 = 15;
        const int WorkSpeedT3 = 20;

        SetPrefabWorkSpeed(minerPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(smelterPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(assemblerTemplate, WorkSpeedT1);
        SetPrefabWorkSpeed(handmadeTemplate, WorkSpeedT1);
        SetPrefabWorkSpeed(manaExtractorPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(enchantingPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(manaHandmadePrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(altarPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(foundryPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(extractorPrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(warehousePrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(manaStoragePrefab, WorkSpeedT1);
        SetPrefabWorkSpeed(beltPrefab, WorkSpeedT1);

        GameObject miner2Prefab = CloneTieredPrefab(
            minerPrefab, $"{PrefabFolder}/Miner_2_machine.prefab", "Miner_2_machine",
            typeof(MinerMachine), WorkSpeedT2);
        GameObject miner3Prefab = CloneTieredPrefab(
            minerPrefab, $"{PrefabFolder}/Miner_3_machine.prefab", "Miner_3_machine",
            typeof(MinerMachine), WorkSpeedT3);
        GameObject smelter2Prefab = CloneTieredPrefab(
            smelterPrefab, $"{PrefabFolder}/Smelter_2_machine.prefab", "Smelter_2_machine",
            typeof(SmelterMachine), WorkSpeedT2);
        GameObject smelter3Prefab = CloneTieredPrefab(
            smelterPrefab, $"{PrefabFolder}/Smelter_3_machine.prefab", "Smelter_3_machine",
            typeof(SmelterMachine), WorkSpeedT3);
        GameObject assembler2Prefab = CloneTieredPrefab(
            assemblerTemplate, $"{PrefabFolder}/Assembler_2_machine.prefab", "Assembler_2_machine",
            typeof(AssemblerMachine), WorkSpeedT2);
        GameObject assembler3Prefab = CloneTieredPrefab(
            assemblerTemplate, $"{PrefabFolder}/Assembler_3_machine.prefab", "Assembler_3_machine",
            typeof(AssemblerMachine), WorkSpeedT3);
        GameObject manaAssembler2Prefab = CloneTieredPrefab(
            manaHandmadePrefab, $"{PrefabFolder}/ManaAssembler_2_machine.prefab", "ManaAssembler_2_machine",
            typeof(ManaAssemblerMachine), WorkSpeedT2);
        GameObject manaAssembler3Prefab = CloneTieredPrefab(
            manaHandmadePrefab, $"{PrefabFolder}/ManaAssembler_3_machine.prefab", "ManaAssembler_3_machine",
            typeof(ManaAssemblerMachine), WorkSpeedT3);

        DeleteObsoleteMachineDefs();

        var defs = new List<ItemDef_Machine>
        {
            UpsertMachineDef("Miner_1", "Miner", "채굴기", false, minerPrefab),
            UpsertMachineDef("Smelter_1", "Smelter", "용광로", false, smelterPrefab),
            UpsertMachineDef("Assembler_1", "Assembler", "마나 제작기", false, assemblerTemplate),
            UpsertMachineDef("HandmadeAssembler_1", "HandmadeAssembler", "수동 제작대", true, handmadeTemplate),
            UpsertMachineDef("ManaExtractor_1", "ManaExtractor", "마나 추출기", false, manaExtractorPrefab),
            UpsertMachineDef("Enchanting_1", "Enchanting", "마법 부여대", false, enchantingPrefab),
            UpsertMachineDef("ManaHandmade_1", "ManaAssembler", "수동 마나 제작기", true, manaHandmadePrefab),
            UpsertMachineDef("Altar_1", "Altar", "제단", false, altarPrefab),
            UpsertMachineDef("Foundry_1", "Foundry", "주조소", false, foundryPrefab),
            UpsertMachineDef("Warehouse_1", "Warehouse", "창고", true, warehousePrefab),
            UpsertMachineDef("ManaStorage_1", "ManaStorage", "마나 저장소", false, manaStoragePrefab),
            UpsertMachineDef("ConveyerBelt_1", "ConveyerBelt", "컨베이어 벨트", false, beltPrefab),
            UpsertMachineDef("Extractor_1", "Extractor", "추출기", false, extractorPrefab),
            UpsertMachineDef("Miner_2", "Miner", "심층 채굴기", false, miner2Prefab),
            UpsertMachineDef("Miner_3", "Miner", "암반 채굴기", false, miner3Prefab),
            UpsertMachineDef("Smelter_2", "Smelter", "더 좋은 용광로", false, smelter2Prefab),
            UpsertMachineDef("Smelter_3", "Smelter", "고열 용광로", false, smelter3Prefab),
            UpsertMachineDef("Assembler_2", "Assembler", "고속 제작기", false, assembler2Prefab),
            UpsertMachineDef("Assembler_3", "Assembler", "대형 제작기", false, assembler3Prefab),
            UpsertMachineDef("ManaAssembler_2", "ManaAssembler", "정교한 마나제작기", false, manaAssembler2Prefab),
            UpsertMachineDef("ManaAssembler_3", "ManaAssembler", "의식 세공기", false, manaAssembler3Prefab),
        };

        MachineDatabase database = AssetDatabase.LoadAssetAtPath<MachineDatabase>(DatabasePath);
        if (database == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(DatabasePath) != null)
            {
                AssetDatabase.DeleteAsset(DatabasePath);
            }

            database = ScriptableObject.CreateInstance<MachineDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.SetMachines(defs.ToArray());
        EditorUtility.SetDirty(database);

        // Dev1 NewGame 계약: 시작 4종.
        CopyContract(defs[0], "Miner_1");
        CopyContract(defs[1], "Smelter_1");
        CopyContract(defs[3], "HandmadeAssembler_1");
        CopyContract(defs[9], "Warehouse_1");

        DeleteObsoleteContracts();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SessionState.SetBool(AutoGenerateSessionKey, true);
        MachineArtBinder.Bind();
        ExtractorArtBinder.Bind();
        RecipePoolBinder.Bind();
        Debug.Log($"[MachineAssetGenerator] 완료: Prefab·SO {defs.Count}종, MachineDatabase, Contracts 4종.");
    }

    private static void DeleteObsoleteMachineDefs()
    {
        string[] obsolete =
        {
            "Minor", "Smelter", "Assembler", "Handmade_Assembler", "ConveyerBelt",
            "채굴기_1", "용광로_1", "제작기_1", "마나 추출기_1", "마법 부여대_1",
            "마나 제작기_1", "제단_1", "주조소_1", "창고_1", "마나 저장소_1", "컨베이어_1",
        };

        for (int i = 0; i < obsolete.Length; i++)
        {
            DeleteAssetIfExists($"{MachineDefFolder}/{obsolete[i]}.asset");
        }
    }

    private static void DeleteObsoleteContracts()
    {
        string[] obsolete = { "채굴기_1", "용광로_1", "제작기_1", "창고_1" };
        for (int i = 0; i < obsolete.Length; i++)
        {
            DeleteAssetIfExists($"{ContractsFolder}/{obsolete[i]}.asset");
        }
    }

    private static void DeleteAssetIfExists(string assetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private static GameObject LoadPrefab(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Sprite LoadMachineIcon(string machineDefId, GameObject prefab)
    {
        string techId = MachineIconResolver.ResolveTechIconId(machineDefId);
        if (!string.IsNullOrEmpty(techId))
        {
            Sprite fromTech = LoadSpriteFromPath($"Assets/Resources/UI/TechTree/{techId}.png");
            if (fromTech != null)
            {
                return fromTech;
            }
        }

        if (prefab == null)
        {
            return null;
        }

        SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = prefab.GetComponentInChildren<SpriteRenderer>();
        }

        return renderer != null ? renderer.sprite : null;
    }

    private static Sprite LoadSpriteFromPath(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.texture != null)
                {
                    return sprite;
                }
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static GameObject EnsureClonedPrefab(
        GameObject template,
        string assetPath,
        string objectName,
        System.Type machineType,
        Color tint)
    {
        string templatePath = AssetDatabase.GetAssetPath(template);
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing == null)
        {
            AssetDatabase.CopyAsset(templatePath, assetPath);
            existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            root.name = objectName;

            Machine oldMachine = root.GetComponent<Machine>();
            if (oldMachine != null && oldMachine.GetType() != machineType)
            {
                Object.DestroyImmediate(oldMachine);
            }

            if (root.GetComponent<Machine>() == null)
            {
                root.AddComponent(machineType);
            }

            SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (!MachineArtBinder.TryApplyArt(spriteRenderer, objectName))
                {
                    spriteRenderer.color = tint;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }

    // 상위 티어 Prefab. 하위 템플릿을 복사한 뒤 workSpeed와 스프라이트만 맞춘다.
    private static GameObject CloneTieredPrefab(
        GameObject template,
        string assetPath,
        string objectName,
        System.Type machineType,
        int workSpeed)
    {
        if (template == null)
        {
            Debug.LogError($"[MachineAssetGenerator] 티어 Prefab 템플릿이 없습니다: {objectName}");
            return null;
        }

        string templatePath = AssetDatabase.GetAssetPath(template);
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (existing == null)
        {
            AssetDatabase.CopyAsset(templatePath, assetPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            root.name = objectName;

            Machine oldMachine = root.GetComponent<Machine>();
            if (oldMachine != null && oldMachine.GetType() != machineType)
            {
                Object.DestroyImmediate(oldMachine);
            }

            if (root.GetComponent<Machine>() == null)
            {
                root.AddComponent(machineType);
            }

            Machine machine = root.GetComponent<Machine>();
            if (machine != null)
            {
                SerializedObject so = new SerializedObject(machine);
                so.FindProperty("workSpeed").intValue = workSpeed;
                // 자동 제작기 2·3은 가로 2 × 세로 1. 템플릿(티어 1)의 2×2를 덮어쓴다.
                if (machineType == typeof(AssemblerMachine) && workSpeed > 10)
                {
                    SerializedProperty sizeProperty = so.FindProperty("size");
                    sizeProperty.FindPropertyRelative("x").intValue = 2;
                    sizeProperty.FindPropertyRelative("y").intValue = 1;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                MachineArtBinder.TryApplyArt(spriteRenderer, objectName);
            }

            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }

    private static void SetPrefabWorkSpeed(GameObject prefab, int workSpeed)
    {
        if (prefab == null)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            Machine machine = root.GetComponent<Machine>();
            if (machine == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(machine);
            so.FindProperty("workSpeed").intValue = workSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static ItemDef_Machine UpsertMachineDef(
        string machineDefId,
        string machineTypeId,
        string displayName,
        bool requiresManualWork,
        GameObject prefab)
    {
        string assetPath = $"{MachineDefFolder}/{machineDefId}.asset";
        ItemDef_Machine def = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(assetPath);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ItemDef_Machine>();
            AssetDatabase.CreateAsset(def, assetPath);
        }

        def.id = machineDefId;
        def.machineTypeId = machineTypeId;
        def.displayName = displayName;
        def.requiresManualWork = requiresManualWork;
        def.category = ItemCategory.Material;
        def.machinePrefab = prefab;
        if (def.icon == null || def.icon.texture == null)
        {
            def.icon = MachineArtBinder.LoadArtSprite(machineDefId)
                ?? LoadMachineIcon(machineDefId, prefab);
        }

        EditorUtility.SetDirty(def);
        return def;
    }

    private static void CopyContract(ItemDef_Machine source, string fileName)
    {
        string destPath = $"{ContractsFolder}/{fileName}.asset";
        ItemDef_Machine contract = AssetDatabase.LoadAssetAtPath<ItemDef_Machine>(destPath);
        if (contract == null)
        {
            contract = ScriptableObject.CreateInstance<ItemDef_Machine>();
            AssetDatabase.CreateAsset(contract, destPath);
        }

        EditorUtility.CopySerialized(source, contract);
        contract.name = fileName;
        EditorUtility.SetDirty(contract);
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
