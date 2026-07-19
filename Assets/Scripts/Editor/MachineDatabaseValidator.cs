#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// MachineDatabase MVP id·prefab null 여부를 검증한다.
public static class MachineDatabaseValidator
{
    [MenuItem("DungeonFront/Validate MachineDatabase")]
    public static void Validate()
    {
        MachineDatabase database = AssetDatabase.LoadAssetAtPath<MachineDatabase>(
            "Assets/ItemDefinition/MachineDef/MachineDatabase.asset");
        if (database == null)
        {
            Debug.LogError("[MachineDatabaseValidator] MachineDatabase.asset이 없습니다. Generate Week3 Machine Assets를 실행하세요.");
            return;
        }

        int failCount = database.ValidateRequired(
            MachineDatabase.RequiredMvpMachineDefIds,
            out var failures);
        int required = MachineDatabase.RequiredMvpMachineDefIds.Length;
        int ok = required - failCount;

        for (int i = 0; i < failures.Count; i++)
        {
            Debug.LogError($"[MachineDatabaseValidator] {failures[i]}");
        }

        Debug.Log($"[MachineDatabaseValidator] 결과: OK={ok}, FAIL={failCount}, 필요={required}");
    }
}
#endif
