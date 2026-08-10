using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// 인벤토리·포트·벨트에 쓰는 런타임 아이템 개체.
// 불변값은 ItemDefinition에 두고, 가변값(레벨·인챈트 등)은 이 인스턴스가 가진다.
[Serializable]
public class Item
{
    public ItemDefinition definition;

    // 재료·장비 레벨. 1 미만(미지정 직렬화 등)은 1로 취급한다.
    public int level = 1;

    // 인스턴스에 부여된 인챈트 목록. 추가 순서를 유지하고, 스택 판정은 집합 비교.
    [FormerlySerializedAs("enchantmentIds")]
    [SerializeField]
    private List<Enchantment> enchantments = new();

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

    public Sprite Icon => ItemIconResolver.Resolve(definition);

    public ItemCategory Category =>
        definition != null ? definition.category : ItemCategory.Material;

    public IReadOnlyList<Enchantment> Enchantments => enchantments;

    // ItemDefinition에 정의된 인챈트 슬롯 수. definition이 없으면 0.
    public int EnchantmentSlotCount =>
        definition != null ? Mathf.Max(0, definition.enchantmentSlotCount) : 0;

    // 직렬화 공백·구에셋에서 0이 들어온 경우에도 최소 1로 본다.
    public int ResolvedLevel => level > 0 ? level : 1;

    public static Item FromDefinition(ItemDefinition definition, int level = 1)
    {
        if (definition == null)
        {
            return null;
        }

        return new Item
        {
            definition = definition,
            level = level > 0 ? level : 1,
            enchantments = new List<Enchantment>(),
        };
    }

    public Item Clone()
    {
        return new Item
        {
            definition = definition,
            level = ResolvedLevel,
            enchantments = enchantments != null
                ? new List<Enchantment>(enchantments)
                : new List<Enchantment>(),
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

    // 레시피 입력 요구 충족: 같은 정의이고, 이 아이템 레벨이 요구 레벨 이상.
    public bool SatisfiesRecipeRequirement(Item required)
    {
        if (required == null || !MatchesDefinition(required))
        {
            return false;
        }

        return ResolvedLevel >= required.ResolvedLevel;
    }

    // 남은 슬롯이 있고, 같은 속성이 아직 없으면 목록 끝에 추가한다. 기존 순서는 바꾸지 않는다.
    public bool TryAddEnchantment(
        EnchantmentId attribute,
        EnchantmentForm form = EnchantmentForm.None)
    {
        return TryAddEnchantment(new Enchantment(attribute, form));
    }

    public bool TryAddEnchantment(Enchantment enchantment)
    {
        if (!Enum.IsDefined(typeof(EnchantmentId), enchantment.attribute)
            || !Enum.IsDefined(typeof(EnchantmentForm), enchantment.form))
        {
            return false;
        }

        EnsureEnchantmentList();
        if (enchantments.Count >= EnchantmentSlotCount)
        {
            return false;
        }

        // 같은 속성은 형태가 달라도 한 아이템에 두 번 붙일 수 없다.
        if (HasAttribute(enchantment.attribute))
        {
            return false;
        }

        enchantments.Add(enchantment);
        return true;
    }

    public bool HasEnchantment(Enchantment enchantment)
    {
        if (enchantments == null)
        {
            return false;
        }

        for (int i = 0; i < enchantments.Count; i++)
        {
            if (enchantments[i] == enchantment)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAttribute(EnchantmentId attribute)
    {
        if (enchantments == null)
        {
            return false;
        }

        for (int i = 0; i < enchantments.Count; i++)
        {
            if (enchantments[i].attribute == attribute)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureEnchantmentList()
    {
        if (enchantments == null)
        {
            enchantments = new List<Enchantment>();
        }
    }

    private bool HasSameInstanceState(Item other)
    {
        if (ResolvedLevel != other.ResolvedLevel)
        {
            return false;
        }

        return HasSameEnchantments(other);
    }

    // 같은 인챈트 집합이면 동일. 순서는 스택 판정에 쓰지 않고, 목록 자체는 추가 순서를 유지한다.
    private bool HasSameEnchantments(Item other)
    {
        int count = enchantments != null ? enchantments.Count : 0;
        int otherCount = other.enchantments != null ? other.enchantments.Count : 0;
        if (count != otherCount)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            if (!other.HasEnchantment(enchantments[i]))
            {
                return false;
            }
        }

        return true;
    }
}
