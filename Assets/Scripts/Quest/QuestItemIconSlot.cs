using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 퀘스트 요구/보상 슬롯. Slot02a 배경 + Select01a 호버 강조 + 커서 옆 이름.
public class QuestItemIconSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject highlightRoot;
    [SerializeField] private string displayName;

    public void Configure(string itemDisplayName, GameObject highlight)
    {
        displayName = itemDisplayName ?? "";
        highlightRoot = highlight;
        SetHighlightVisible(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlightVisible(true);
        QuestItemSlotTooltip.Show(displayName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlightVisible(false);
        QuestItemSlotTooltip.Hide();
    }

    private void OnDisable()
    {
        SetHighlightVisible(false);
        QuestItemSlotTooltip.Hide();
    }

    private void SetHighlightVisible(bool visible)
    {
        if (highlightRoot != null)
        {
            highlightRoot.SetActive(visible);
        }
    }
}

// 마우스 커서 옆에 아이템 이름을 띄운다.
public sealed class QuestItemSlotTooltip : MonoBehaviour
{
    private static QuestItemSlotTooltip instance;

    private RectTransform rect;
    private TMP_Text label;
    private Canvas canvas;
    private readonly Vector2 cursorOffset = new Vector2(18f, -18f);

    public static void Show(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Hide();
            return;
        }

        EnsureInstance();
        instance.label.text = text;
        instance.gameObject.SetActive(true);
        instance.FollowCursor();
    }

    public static void Hide()
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(false);
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        GameObject root = new GameObject(
            "QuestItemSlotTooltip",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(QuestItemSlotTooltip));
        if (canvas != null)
        {
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();
        }

        instance = root.GetComponent<QuestItemSlotTooltip>();
        instance.canvas = canvas;
        instance.Build();
        root.SetActive(false);
    }

    private void Build()
    {
        rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(220f, 36f);

        Image background = gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.06f, 0.05f, 0.92f);
        background.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 4f);
        labelRect.offsetMax = new Vector2(-10f, -4f);

        label = labelObject.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Overflow;
        TmpUiStyle.Apply(label, TmpUiStyle.Role.Caption);
        label.fontSize = 18f;
        label.fontStyle = FontStyles.Bold;
        label.color = TmpUiStyle.TitleColor;
    }

    private void Update()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        FollowCursor();
    }

    private void FollowCursor()
    {
        if (rect == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 screen = mouse.position.ReadValue() + cursorOffset;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Camera cam = canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screen,
                    cam,
                    out Vector2 local))
            {
                rect.anchoredPosition = local;
                return;
            }
        }

        rect.position = screen;
    }
}

// 슬롯 생성·채우기. 배경은 항상 Slot02a.
public static class QuestItemIconSlot
{
    public const float SlotSize = 56f;

    private static Sprite goldPlaceholder;
    private static Sprite famePlaceholder;
    private static Sprite neutralPlaceholder;

    public static void Clear(Transform slotsRoot)
    {
        if (slotsRoot == null)
        {
            return;
        }

        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(slotsRoot.GetChild(i).gameObject);
        }

        QuestItemSlotTooltip.Hide();
    }

    public static void Populate(Transform slotsRoot, ItemEntryList list)
    {
        Clear(slotsRoot);
        UiNoteBookSlot.ClearCache();
        if (slotsRoot == null || list?.entries == null)
        {
            return;
        }

        foreach (ItemEntry entry in list.entries)
        {
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            Create(slotsRoot, entry);
        }
    }

    public static void PopulateRewards(Transform slotsRoot, Quest quest)
    {
        Populate(slotsRoot, quest?.rewards);
        if (slotsRoot == null || quest == null)
        {
            return;
        }

        QuestRuntimeInfo info = QuestRuntimeRegistry.Get(quest);
        int reputation = info != null ? info.rewardReputation : 0;
        if (reputation <= 0)
        {
            return;
        }

        if (ListContainsCurrency(quest.rewards, "fame")
            || ListContainsCurrency(quest.rewards, "reputation"))
        {
            return;
        }

        CreateCurrency(slotsRoot, "명예", reputation, isGold: false);
    }

    public static GameObject Create(Transform parent, ItemEntry entry)
    {
        string id = entry.item.Id ?? "";
        string name = string.IsNullOrWhiteSpace(entry.item.DisplayName)
            ? id
            : entry.item.DisplayName;

        if (IsCurrencyId(id, "gold"))
        {
            return CreateCurrency(parent, "골드", entry.count, isGold: true);
        }

        if (IsCurrencyId(id, "fame") || IsCurrencyId(id, "reputation"))
        {
            return CreateCurrency(parent, "명예", entry.count, isGold: false);
        }

        Sprite icon = ItemIconResolver.Resolve(entry.item);
        if (icon == null)
        {
            return CreateSlot(
                parent,
                name,
                entry.count,
                GetNeutralPlaceholder(),
                "?",
                new Color(0.35f, 0.28f, 0.22f, 1f));
        }

        return CreateSlot(parent, name, entry.count, icon, null, default);
    }

    public static GameObject CreateCurrency(
        Transform parent,
        string displayName,
        int count,
        bool isGold)
    {
        if (isGold)
        {
            return CreateSlot(
                parent,
                displayName,
                count,
                GetGoldPlaceholder(),
                "G",
                new Color(0.45f, 0.3f, 0.08f, 1f));
        }

        return CreateSlot(
            parent,
            displayName,
            count,
            GetFamePlaceholder(),
            "名",
            new Color(0.35f, 0.2f, 0.5f, 1f));
    }

    private static GameObject CreateSlot(
        Transform parent,
        string displayName,
        int count,
        Sprite icon,
        string placeholderGlyph,
        Color glyphColor)
    {
        // 루트는 레이아웃·레이캐스트만 담당. 슬롯/강조 그래픽은 자식으로 동일 크기를 쓴다.
        GameObject root = new GameObject(
            "ItemSlot",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement),
            typeof(QuestItemIconSlotView));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(SlotSize, SlotSize);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minWidth = SlotSize;
        layout.minHeight = SlotSize;
        layout.preferredWidth = SlotSize;
        layout.preferredHeight = SlotSize;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;

        // 루트 Image는 투명 히트 영역만.
        Image hitArea = root.GetComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0f);
        hitArea.raycastTarget = true;
        hitArea.sprite = null;

        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(root.transform, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        // 슬롯 그래픽도 고정 크기로 중앙 배치해 레이아웃과 어긋나지 않게 한다.
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        Image frame = frameObject.GetComponent<Image>();
        UiNoteBookSlot.ApplySlot(frame);
        frame.type = Image.Type.Simple;
        frame.preserveAspect = false;
        frame.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(SlotSize * 0.58f, SlotSize * 0.58f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.sprite = icon;
        iconImage.color = Color.white;

        if (!string.IsNullOrEmpty(placeholderGlyph))
        {
            GameObject glyphObject = new GameObject(
                "Glyph",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            glyphObject.transform.SetParent(root.transform, false);
            RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;
            TextMeshProUGUI glyphText = glyphObject.GetComponent<TextMeshProUGUI>();
            glyphText.text = placeholderGlyph;
            glyphText.alignment = TextAlignmentOptions.Center;
            glyphText.raycastTarget = false;
            TmpUiStyle.Apply(glyphText, TmpUiStyle.Role.Title);
            glyphText.fontSize = 22f;
            glyphText.color = glyphColor.a > 0f
                ? glyphColor
                : new Color(0.2f, 0.15f, 0.1f, 1f);
        }

        GameObject countObject = new GameObject(
            "Count",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        countObject.transform.SetParent(root.transform, false);
        RectTransform countRect = countObject.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-2f, 2f);
        countRect.sizeDelta = new Vector2(40f, 20f);

        TextMeshProUGUI countText = countObject.GetComponent<TextMeshProUGUI>();
        countText.text = count > 0 ? $"x{count}" : "";
        countText.alignment = TextAlignmentOptions.BottomRight;
        countText.raycastTarget = false;
        countText.overflowMode = TextOverflowModes.Overflow;
        TmpUiStyle.Apply(countText, TmpUiStyle.Role.Caption);
        countText.fontSize = 14f;
        countText.fontStyle = FontStyles.Bold;

        GameObject highlightObject = UiNoteBookSlot.CreateSelectHighlight(root.transform, SlotSize);
        highlightObject.SetActive(false);

        root.GetComponent<QuestItemIconSlotView>().Configure(displayName, highlightObject);
        highlightObject.transform.SetAsLastSibling();
        return root;
    }

    private static bool ListContainsCurrency(ItemEntryList list, string id)
    {
        if (list?.entries == null)
        {
            return false;
        }

        foreach (ItemEntry entry in list.entries)
        {
            if (entry?.item != null && IsCurrencyId(entry.item.Id, id))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCurrencyId(string itemId, string expected)
    {
        return !string.IsNullOrEmpty(itemId)
            && string.Equals(itemId, expected, System.StringComparison.OrdinalIgnoreCase);
    }

    private static Sprite GetGoldPlaceholder()
    {
        if (goldPlaceholder == null)
        {
            goldPlaceholder = CreatePlaceholderSprite(new Color(0.92f, 0.78f, 0.28f, 1f));
        }

        return goldPlaceholder;
    }

    private static Sprite GetFamePlaceholder()
    {
        if (famePlaceholder == null)
        {
            famePlaceholder = CreatePlaceholderSprite(new Color(0.72f, 0.55f, 0.92f, 1f));
        }

        return famePlaceholder;
    }

    private static Sprite GetNeutralPlaceholder()
    {
        if (neutralPlaceholder == null)
        {
            neutralPlaceholder = CreatePlaceholderSprite(new Color(0.55f, 0.48f, 0.4f, 1f));
        }

        return neutralPlaceholder;
    }

    private static Sprite CreatePlaceholderSprite(Color color)
    {
        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
    }
}
