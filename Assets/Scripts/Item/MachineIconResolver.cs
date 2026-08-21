using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 기계 인벤 아이콘. SO icon → 프리팹 스프라이트 → 테크 트리 아이콘 순으로 찾는다.
public static class MachineIconResolver
{
    public const float InventoryIconMaxSize = 32f;
    private const string TechIconResourcesRoot = "UI/TechTree";

    private static readonly Dictionary<string, Sprite> cache = new();

    public static Sprite Resolve(ItemDef_Machine definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
        {
            return null;
        }

        if (cache.TryGetValue(definition.id, out Sprite cached)
            && ItemIconResolver.IsUsable(cached))
        {
            return cached;
        }

        Sprite sprite = ItemIconResolver.IsUsable(definition.icon)
            ? definition.icon
            : null;
        if (sprite == null)
        {
            sprite = GetPrefabSprite(definition.machinePrefab);
        }

        if (sprite == null)
        {
            sprite = LoadTechIcon(definition.id);
        }

        if (ItemIconResolver.IsUsable(sprite))
        {
            cache[definition.id] = sprite;
            if (!ItemIconResolver.IsUsable(definition.icon))
            {
                definition.icon = sprite;
            }
        }

        return sprite;
    }

    // machineDefId에 대응하는 테크 트리 아이콘 리소스 id.
    public static string ResolveTechIconId(string machineDefId)
    {
        if (string.IsNullOrEmpty(machineDefId))
        {
            return null;
        }

        // 제단은 대형 조립(m_crafter_3)으로 해금되지만 전용 아이콘이 있다.
        if (machineDefId == "Altar_1")
        {
            return "m_altar_1";
        }

        if (machineDefId == "HandmadeAssembler_1")
        {
            return "m_crafter_1";
        }

        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node node = TechTreeCatalog.All[i];
            if (node != null
                && node.machineDefId == machineDefId
                && !string.IsNullOrEmpty(node.id))
            {
                return node.id;
            }
        }

        MachineCraftCatalog.Recipe recipe = MachineCraftCatalog.Get(machineDefId);
        return recipe != null ? recipe.requiredTechId : null;
    }

    // 32×32 박스 안에 가로·세로 동일 배율로 맞춘 UI 크기를 반환한다.
    public static Vector2 GetInventoryIconSize(Sprite sprite, float maxSize = InventoryIconMaxSize)
    {
        if (!ItemIconResolver.IsUsable(sprite))
        {
            return new Vector2(maxSize, maxSize);
        }

        float width = sprite.rect.width;
        float height = sprite.rect.height;
        if (width <= 0f || height <= 0f)
        {
            return new Vector2(maxSize, maxSize);
        }

        float scale = Mathf.Min(maxSize / width, maxSize / height);
        return new Vector2(width * scale, height * scale);
    }

    public static void ConfigureInventoryImage(
        Image image,
        ItemDef_Machine definition,
        float maxSize = InventoryIconMaxSize)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = Resolve(definition);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        if (!ItemIconResolver.IsUsable(sprite))
        {
            image.sprite = null;
            image.color = new Color(0.4f, 0.55f, 0.75f, 1f);
            rect.sizeDelta = new Vector2(maxSize, maxSize);
            return;
        }

        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        rect.sizeDelta = GetInventoryIconSize(sprite, maxSize);
    }

    private static Sprite GetPrefabSprite(GameObject prefab)
    {
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

    private static Sprite LoadTechIcon(string machineDefId)
    {
        string techId = ResolveTechIconId(machineDefId);
        if (string.IsNullOrEmpty(techId))
        {
            return null;
        }

        Sprite fromSlot = UiNoteBookSlot.GetTechIcon(techId);
        if (ItemIconResolver.IsUsable(fromSlot))
        {
            return fromSlot;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>($"{TechIconResourcesRoot}/{techId}");
        if (sprites == null)
        {
            return null;
        }

        for (int i = 0; i < sprites.Length; i++)
        {
            if (ItemIconResolver.IsUsable(sprites[i]))
            {
                return sprites[i];
            }
        }

        return null;
    }
}
