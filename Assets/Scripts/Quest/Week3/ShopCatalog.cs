using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "DungeonFront/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    public ShopEntry[] entries = Array.Empty<ShopEntry>();

    public ShopEntry Get(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return null;
        }

        return Array.Find(entries, entry => entry != null && entry.entryId == entryId);
    }
}

[Serializable]
public class ShopEntry
{
    public string entryId;
    public string displayName;
    [Min(0)] public int price;
    [Min(1)] public int count = 1;
    public ItemDefinition item;
    public string machineDefId;
    public ItemDef_Machine machineDefinition;

    public bool IsMachine => machineDefinition != null
        || !string.IsNullOrWhiteSpace(machineDefId);
}
