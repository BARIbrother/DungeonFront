using UnityEngine;
using UnityEngine.UI;

// NoteBook 팩 슬롯/셀렉트 스프라이트.
public static class UiNoteBookSlot
{
    public const string SlotResourcesPath = "UI/UI_NoteBook_Slot02a";
    public const string SlotUnlockedResourcesPath = "UI/UI_NoteBook_Slot03a";
    public const string SlotLockedResourcesPath = "UI/UI_NoteBook_Slot04a";
    public const string SelectResourcesPath = "UI/UI_NoteBook_Select01a";
    public const string TechIconResourcesRoot = "UI/TechTree";

    public const float SlotPixelSize = 28f;
    public const float SelectPixelSize = 30f;

    private static Sprite cachedSlot;
    private static Sprite cachedUnlocked;
    private static Sprite cachedLocked;
    private static Sprite cachedSelect;
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> cachedTechIcons =
        new System.Collections.Generic.Dictionary<string, Sprite>();
    private static bool loggedMissingSlot;
    private static bool loggedMissingUnlocked;
    private static bool loggedMissingLocked;
    private static bool loggedMissingSelect;
    private static bool loggedSelectSliced;

    public static Sprite GetSlotSprite()
    {
        return LoadFullSprite(ref cachedSlot, SlotResourcesPath, SlotPixelSize, ref loggedMissingSlot);
    }

    public static Sprite GetUnlockedSlotSprite()
    {
        return LoadFullSprite(
            ref cachedUnlocked,
            SlotUnlockedResourcesPath,
            SlotPixelSize,
            ref loggedMissingUnlocked);
    }

    public static Sprite GetLockedSlotSprite()
    {
        return LoadFullSprite(
            ref cachedLocked,
            SlotLockedResourcesPath,
            SlotPixelSize,
            ref loggedMissingLocked);
    }

    public static Sprite GetSelectSprite()
    {
        return LoadFullSprite(ref cachedSelect, SelectResourcesPath, SelectPixelSize, ref loggedMissingSelect);
    }

    public static Sprite GetTechIcon(string techId)
    {
        if (string.IsNullOrEmpty(techId))
        {
            return null;
        }

        if (cachedTechIcons.TryGetValue(techId, out Sprite cached) && cached != null)
        {
            return cached;
        }

        Sprite loaded = null;
        bool logged = false;
        Sprite sprite = LoadFullSprite(
            ref loaded,
            $"{TechIconResourcesRoot}/{techId}",
            16f,
            ref logged);
        if (sprite != null)
        {
            cachedTechIcons[techId] = sprite;
        }

        return sprite;
    }

    public static void ClearCache()
    {
        cachedSlot = null;
        cachedUnlocked = null;
        cachedLocked = null;
        cachedSelect = null;
        cachedTechIcons.Clear();
        loggedMissingSlot = false;
        loggedMissingUnlocked = false;
        loggedMissingLocked = false;
        loggedMissingSelect = false;
        loggedSelectSliced = false;
    }

    public static void ApplySlot(Image image)
    {
        Apply(image, GetSlotSprite());
    }

    public static void ApplyUnlockedSlot(Image image)
    {
        Apply(image, GetUnlockedSlotSprite());
    }

    public static void ApplyLockedSlot(Image image)
    {
        Apply(image, GetLockedSlotSprite());
    }

    public static void ApplySelect(Image image)
    {
        Apply(image, GetSelectSprite());
    }

    // Select01a가 Multiple 슬라이스(10×10×4)로 임포트된 경우 4코너, 아니면 단일 Image.
    public static GameObject CreateSelectHighlight(Transform parent, float slotSize)
    {
        GameObject root = new GameObject("Highlight", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(slotSize, slotSize);

        Sprite full = GetSelectSprite();
        if (full != null && full.rect.width >= SelectPixelSize - 2f)
        {
            Image image = root.AddComponent<Image>();
            ApplySelect(image);
            image.raycastTarget = false;
            return root;
        }

        if (!loggedSelectSliced)
        {
            loggedSelectSliced = true;
            Debug.LogWarning(
                "[UiNoteBookSlot] Select01a가 조각 스프라이트로 임포트됨. "
                + "DungeonFront/UI/Fix NoteBook Slot Import Settings 실행 권장.");
        }

        Sprite[] slices = Resources.LoadAll<Sprite>(SelectResourcesPath);
        if (slices == null || slices.Length == 0)
        {
            return root;
        }

        float cornerSize = slotSize * (10f / SelectPixelSize);
        foreach (Sprite slice in slices)
        {
            Rect r = slice.rect;
            bool left = r.x < SelectPixelSize * 0.5f;
            bool top = r.y > SelectPixelSize * 0.5f;
            Vector2 anchor = new Vector2(left ? 0f : 1f, top ? 1f : 0f);
            AddCornerImage(root.transform, slice, anchor, cornerSize);
        }

        return root;
    }

    private static void AddCornerImage(
        Transform parent,
        Sprite sprite,
        Vector2 anchor,
        float cornerSize)
    {
        GameObject cornerObject = new GameObject(sprite.name, typeof(RectTransform), typeof(Image));
        cornerObject.transform.SetParent(parent, false);
        RectTransform rect = cornerObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(cornerSize, cornerSize);

        Image image = cornerObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        image.useSpriteMesh = false;
        image.raycastTarget = false;
    }

    private static void Apply(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        image.useSpriteMesh = false;
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier = 1f;
    }

    private static Sprite LoadFullSprite(
        ref Sprite cache,
        string path,
        float expectedPixelSize,
        ref bool loggedMissing)
    {
        if (cache != null)
        {
            return cache;
        }

        Sprite[] all = Resources.LoadAll<Sprite>(path);
        Sprite best = PickLargestSprite(all) ?? Resources.Load<Sprite>(path);
        if (best != null && best.rect.width >= expectedPixelSize - 2f)
        {
            cache = best;
            return cache;
        }

        Texture2D texture = best != null
            ? best.texture
            : Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            if (!loggedMissing)
            {
                loggedMissing = true;
                Debug.LogWarning($"[UiNoteBookSlot] 스프라이트 없음: Resources/{path}");
            }

            return null;
        }

        Rect fullRect = new Rect(0f, 0f, texture.width, texture.height);
        try
        {
            cache = Sprite.Create(
                texture,
                fullRect,
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                Vector4.zero);
            cache.name = path.Replace('/', '_') + "_full";
            return cache;
        }
        catch
        {
            // readable=false 등으로 전체 스프라이트 생성 불가 — 조각 스프라이트는 CreateSelectHighlight에서 처리.
            return null;
        }
    }

    private static Sprite PickLargestSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        Sprite best = sprites[0];
        float bestArea = best.rect.width * best.rect.height;
        for (int i = 1; i < sprites.Length; i++)
        {
            float area = sprites[i].rect.width * sprites[i].rect.height;
            if (area > bestArea)
            {
                bestArea = area;
                best = sprites[i];
            }
        }

        return best;
    }
}
