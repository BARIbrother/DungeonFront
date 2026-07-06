using UnityEngine;

// 기계 아이템 정의 SO. id는 machineDefId, machinePrefab은 배치용 프리팹 참조.
[CreateAssetMenu(fileName = "ItemDef_Machine", menuName = "DungeonFront/Item/Machine")]
public class ItemDef_Machine : ItemDefinition
{
    public GameObject machinePrefab;
}
