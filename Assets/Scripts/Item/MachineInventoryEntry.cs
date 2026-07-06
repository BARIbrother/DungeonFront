using System;

// 인벤에 보관되는 기계 런타임 인스턴스. 정의(ItemDef_Machine)와 instanceId를 묶는다.
[Serializable]
public class MachineInventoryEntry
{
    public string instanceId;
    public ItemDef_Machine definition;

    public static MachineInventoryEntry Create(ItemDef_Machine definition)
    {
        if (definition == null)
        {
            return null;
        }

        return new MachineInventoryEntry
        {
            instanceId = Guid.NewGuid().ToString(),
            definition = definition,
        };
    }
}
