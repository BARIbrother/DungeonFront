using System.Collections.Generic;
using UnityEngine;

// machineDefId로 ItemDef_Machine을 조회하는 SO 레지스트리.
[CreateAssetMenu(fileName = "MachineDatabase", menuName = "DungeonFront/Item/MachineDatabase")]
public class MachineDatabase : ScriptableObject
{
    // Week3 MVP machineDefId. Validator·에디터 체크리스트가 공유한다.
    public static readonly string[] RequiredMvpMachineDefIds =
    {
        "Miner_1",
        "Smelter_1",
        "Assembler_1",
        "HandmadeAssembler_1",
        "ManaExtractor_1",
        "Enchanting_1",
        "ManaHandmade_1",
        "Altar_1",
        "Foundry_1",
        "Warehouse_1",
        "ManaStorage_1",
        "ConveyerBelt_1",
    };

    [SerializeField] private ItemDef_Machine[] machines;

    private readonly Dictionary<string, ItemDef_Machine> lookup = new();
    private bool isIndexed;

    public IReadOnlyList<ItemDef_Machine> All => machines;

    private void OnEnable()
    {
        RebuildLookup();
    }

    // machines 배열을 id 키로 인덱싱한다. null prefab이면 경고한다.
    public void RebuildLookup()
    {
        lookup.Clear();
        isIndexed = true;

        if (machines == null)
        {
            return;
        }

        for (int i = 0; i < machines.Length; i++)
        {
            ItemDef_Machine machine = machines[i];
            if (machine == null || string.IsNullOrEmpty(machine.id))
            {
                continue;
            }

            if (machine.machinePrefab == null)
            {
                Debug.LogError($"[MachineDatabase] '{machine.id}'의 machinePrefab이 null입니다.", machine);
            }

            lookup[machine.id] = machine;
        }
    }

    // machineDefId로 기계 정의를 반환한다. 없으면 null.
    public ItemDef_Machine Get(string machineDefId)
    {
        if (!isIndexed)
        {
            RebuildLookup();
        }

        if (string.IsNullOrEmpty(machineDefId))
        {
            return null;
        }

        lookup.TryGetValue(machineDefId, out ItemDef_Machine machine);
        return machine;
    }

    // MVP id 목록을 조회해 prefab null 여부를 검사한다. 실패 개수를 반환한다.
    public int ValidateRequired(string[] machineDefIds, out System.Collections.Generic.List<string> failures)
    {
        failures = new System.Collections.Generic.List<string>();
        if (machineDefIds == null)
        {
            return 0;
        }

        RebuildLookup();
        for (int i = 0; i < machineDefIds.Length; i++)
        {
            string id = machineDefIds[i];
            ItemDef_Machine def = Get(id);
            if (def == null)
            {
                failures.Add($"{id}: 조회 실패");
                continue;
            }

            if (def.machinePrefab == null)
            {
                failures.Add($"{id}: machinePrefab null");
                continue;
            }

            if (def.machinePrefab.GetComponent<Machine>() == null)
            {
                failures.Add($"{id}: Prefab에 Machine 없음");
            }
        }

        return failures.Count;
    }

#if UNITY_EDITOR
    // 에디터에서 machines 배열을 일괄 설정한다.
    public void SetMachines(ItemDef_Machine[] newMachines)
    {
        machines = newMachines;
        RebuildLookup();
    }
#endif
}
