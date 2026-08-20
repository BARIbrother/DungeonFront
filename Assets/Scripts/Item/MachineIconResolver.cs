using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 기계 인벤 아이콘은 별도 아이콘 에셋이 아니라 프리팹 SpriteRenderer 텍스처를 그대로 쓴다.
public static class MachineIconResolver
{
    public const float InventoryIconMaxSize = 32f;

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

        Sprite sprite = GetPrefabSprite(definition.machinePrefab);
        if (ItemIconResolver.IsUsable(sprite))
        {
            cache[definition.id] = sprite;
        }

        return sprite;
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
}
