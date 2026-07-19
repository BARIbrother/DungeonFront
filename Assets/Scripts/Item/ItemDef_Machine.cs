using UnityEngine;

// 기계 아이템 정의 SO. id는 machineDefId, machinePrefab은 배치용 프리팹 참조.
[CreateAssetMenu(fileName = "ItemDef_Machine", menuName = "DungeonFront/Item/Machine")]
public class ItemDef_Machine : ItemDefinition
{
    // 기계 type 문자열 (예: 채굴기). machineDefId는 보통 {machineTypeId}_{tier}.
    public string machineTypeId;

    // 수작업(클릭) 기계 여부. 실제 동작은 Prefab의 SupportsManualWorkClick이 담당한다.
    public bool requiresManualWork;

    public GameObject machinePrefab;
}
