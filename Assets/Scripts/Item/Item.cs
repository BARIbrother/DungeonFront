using System;
using System.Collections.Generic;
using UnityEngine;

// 인벤토리·포트·벨트에 쓰는 런타임 아이템 개체.
// 불변값은 ItemDefinition에 두고, 가변값(인챈트 등)은 이 인스턴스가 가진다.
[Serializable]
public class Item
{
    public ItemDefinition definition;

    // 이후 인챈트 등으로 확장. 지금은 스택 비교용 스텁만 둔다.
    [SerializeField]
    private List<string> enchantmentIds = new();

    public string Id => definition != null ? definition.id : null;

    public string DisplayName
    {
        get
        {
            if (definition == null)
            {
                return null;
            }

            return string.IsNullOrEmpty(definition.displayName)
                ? definition.id
                : definition.displayName;
        }
    }

    public Sprite Icon => definition != null ? definition.icon : null;

    public ItemCategory Category =>
        definition != null ? definition.category : ItemCategory.Material;

    public IReadOnlyList<string> EnchantmentIds => enchantmentIds;

    public static Item FromDefinition(ItemDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return new Item
        {
            definition = definition,
            enchantmentIds = new List<string>(),
        };
    }

    public Item Clone()
    {
        return new Item
        {
            definition = definition,
            enchantmentIds = enchantmentIds != null
                ? new List<string>(enchantmentIds)
                : new List<string>(),
        };
    }

    // 같은 인벤/포트 엔트리에 합칠 수 있는지 판정한다.
    public bool CanStackWith(Item other)
    {
        if (other == null || !MatchesDefinition(other))
        {
            return false;
        }

        return HasSameInstanceState(other);
    }

    public bool MatchesDefinition(ItemDefinition def)
    {
        if (definition == null || def == null)
        {
            return false;
        }

        if (ReferenceEquals(definition, def))
        {
            return true;
        }

        if (string.IsNullOrEmpty(definition.id) || string.IsNullOrEmpty(def.id))
        {
            return false;
        }

        return definition.id == def.id;
    }

    public bool MatchesDefinition(Item other)
    {
        return other != null && MatchesDefinition(other.definition);
    }

    private bool HasSameInstanceState(Item other)
    {
        int count = enchantmentIds != null ? enchantmentIds.Count : 0;
        int otherCount = other.enchantmentIds != null ? other.enchantmentIds.Count : 0;
        if (count != otherCount)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            if (enchantmentIds[i] != other.enchantmentIds[i])
            {
                return false;
            }
        }

        return true;
    }
}
