using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// E키로 여는 플레이어 인벤토리. 아이템 그리드 + 좌하단 골드·명성.
public class InventoryUI : MonoBehaviour
{
    private static InventoryUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform itemGridRect;
    private Text goldText;
    private Text reputationText;
    private Font uiFont;
    private ItemManager itemManager;
    private PlayerInventory subscribedInventory;
    private readonly List<GameObject> itemSlots = new();
    private readonly List<GameObject> itemSlotPool = new();
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<InventoryUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("PlayerInventoryUISystem");
        systemObject.AddComponent<InventoryUI>();
    }

    public static void Toggle()
    {
        EnsureInstance();
        if (instance.isOpen)
        {
            instance.Hide();
        }
        else
        {
            instance.Open();
        }
    }

    public static void Show()
    {
        EnsureInstance();
        instance.Open();
    }

    public static void Close()
    {
        if (instance != null)
        {
            instance.Hide();
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<InventoryUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("PlayerInventoryUISystem");
        instance = systemObject.AddComponent<InventoryUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureUiHierarchy();
        Hide();
    }

    private void OnDestroy()
    {
        UnsubscribeInventory();
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Open()
    {
        itemManager = FindAnyObjectByType<ItemManager>();
        SubscribeInventory();
        Refresh();
        modalRoot.SetActive(true);
        isOpen = true;
    }

    public void Hide()
    {
        isOpen = false;
        UnsubscribeInventory();
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void SubscribeInventory()
    {
        PlayerInventory inventory = PlayerInventory.GetOrFind();

        if (subscribedInventory == inventory)
        {
            return;
        }

        UnsubscribeInventory();
        subscribedInventory = inventory;
        if (subscribedInventory != null)
        {
            subscribedInventory.OnItemsChanged += Refresh;
            subscribedInventory.OnMachinesChanged += Refresh;
        }
    }

    private void UnsubscribeInventory()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory.OnItemsChanged -= Refresh;
            subscribedInventory.OnMachinesChanged -= Refresh;
            subscribedInventory = null;
        }
    }

    private void Refresh()
    {
        RefreshCurrency();
        RebuildItemSlots();
    }

    private void RefreshCurrency()
    {
        GameSessionState session = GameSessionState.Instance;
        int gold = session != null ? session.gold : 0;
        int reputation = session != null ? session.reputation : 0;

        if (goldText != null)
        {
            goldText.text = $"골드 {gold}";
        }

        if (reputationText != null)
        {
            reputationText.text = $"명성 {reputation}";
        }
    }

    private void RebuildItemSlots()
    {
        ClearItemSlots();

        if (itemGridRect == null)
        {
            return;
        }

        PlayerInventory inventory = subscribedInventory != null
            ? subscribedInventory
            : PlayerInventory.GetOrFind();
        if (inventory == null)
        {
            return;
        }

        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        List<ItemEntry> owned = inventory.GetOwnedItemEntries();
        owned.Sort((a, b) =>
        {
            string idA = a?.item?.Id ?? string.Empty;
            string idB = b?.item?.Id ?? string.Empty;
            int byId = string.CompareOrdinal(idA, idB);
            if (byId != 0)
            {
                return byId;
            }

            return a.count.CompareTo(b.count);
        });

        for (int i = 0; i < owned.Count; i++)
        {
            ItemEntry entry = owned[i];
            if (entry?.item == null || entry.count <= 0 || IsCurrencyItem(entry.item.Id))
            {
                continue;
            }

            CreateItemSlot(entry.item, entry.count);
        }

        // 기계는 종류별로 묶어 같은 그리드에 표시한다. (B키 배치 소모 대상)
        var machineCounts = new Dictionary<string, (ItemDef_Machine definition, int count)>();
        foreach (MachineInventoryEntry machine in inventory.Machines)
        {
            if (machine?.definition == null || string.IsNullOrEmpty(machine.definition.id))
            {
                continue;
            }

            string id = machine.definition.id;
            if (machineCounts.TryGetValue(id, out var existing))
            {
                machineCounts[id] = (existing.definition, existing.count + 1);
            }
            else
            {
                machineCounts[id] = (machine.definition, 1);
            }
        }

        var machineIds = new List<string>(machineCounts.Keys);
        machineIds.Sort(string.CompareOrdinal);
        for (int i = 0; i < machineIds.Count; i++)
        {
            (ItemDef_Machine definition, int count) group = machineCounts[machineIds[i]];
            CreateMachineSlot(group.definition, group.count);
        }
    }

    private void CreateMachineSlot(ItemDef_Machine definition, int count)
    {
        if (definition == null)
        {
            return;
        }

        string label = !string.IsNullOrEmpty(definition.displayName)
            ? definition.displayName
            : definition.id;

        GameObject slotObject = RentItemSlot($"Machine_{definition.id}");
        Image slotImage = slotObject.GetComponent<Image>();
        slotImage.color = new Color(0.16f, 0.2f, 0.28f, 1f);

        Image iconImage = slotObject.transform.Find("Icon").GetComponent<Image>();
        MachineIconResolver.ConfigureInventoryImage(iconImage, definition);
        bool hasIcon = iconImage.sprite != null;

        Text countText = slotObject.transform.Find("Count").GetComponent<Text>();
        countText.font = uiFont;
        countText.fontSize = 14;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.color = Color.white;
        countText.text = hasIcon ? $"x{count}" : $"{label}\nx{count}";
        countText.horizontalOverflow = HorizontalWrapMode.Wrap;
        countText.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void CreateItemSlot(Item item, int count)
    {
        if (item?.definition == null || count <= 0)
        {
            return;
        }

        if (item.Category == ItemCategory.Currency)
        {
            return;
        }

        ItemDefinition definition = item.definition;
        string itemId = item.Id;

        GameObject slotObject = RentItemSlot($"Item_{itemId}");
        Image slotImage = slotObject.GetComponent<Image>();
        slotImage.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        Image iconImage = slotObject.transform.Find("Icon").GetComponent<Image>();
        Sprite icon = ItemIconResolver.Resolve(definition);
        if (icon == null && !string.IsNullOrEmpty(itemId))
        {
            icon = ItemIconResolver.ResolveById(itemId);
        }

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = new Color(0.35f, 0.35f, 0.4f, 1f);
        }

        Text countText = slotObject.transform.Find("Count").GetComponent<Text>();
        countText.font = uiFont;
        countText.fontSize = 14;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.color = Color.white;
        countText.text = $"x{count}";
        countText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private GameObject RentItemSlot(string slotName)
    {
        GameObject slotObject;
        if (itemSlotPool.Count > 0)
        {
            int last = itemSlotPool.Count - 1;
            slotObject = itemSlotPool[last];
            itemSlotPool.RemoveAt(last);
            slotObject.SetActive(true);
        }
        else
        {
            slotObject = BuildItemSlotShell();
        }

        slotObject.name = slotName;
        slotObject.transform.SetParent(itemGridRect, false);
        itemSlots.Add(slotObject);
        return slotObject;
    }

    private GameObject BuildItemSlotShell()
    {
        var slotObject = new GameObject("Slot");
        slotObject.AddComponent<Image>();

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.55f);
        iconRect.anchorMax = new Vector2(0.5f, 0.55f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(48f, 48f);
        var iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var countObject = new GameObject("Count");
        countObject.transform.SetParent(slotObject.transform, false);
        var countRect = countObject.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0.35f);
        countRect.offsetMin = new Vector2(4f, 2f);
        countRect.offsetMax = new Vector2(-4f, -2f);
        countObject.AddComponent<Text>();

        return slotObject;
    }

    private static bool IsCurrencyItem(string itemId)
    {
        return string.Equals(itemId, "gold", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemId, "fame", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemId, "reputation", StringComparison.OrdinalIgnoreCase);
    }

    private void ClearItemSlots()
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            GameObject slot = itemSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.SetActive(false);
            itemSlotPool.Add(slot);
        }

        itemSlots.Clear();
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("PlayerInventoryCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 65;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("InventoryModal");
        modalRoot.transform.SetParent(canvasObject.transform, false);
        var modalRect = modalRoot.AddComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        var backdropObject = new GameObject("Backdrop");
        backdropObject.transform.SetParent(modalRoot.transform, false);
        var backdropRect = backdropObject.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        var backdropImage = backdropObject.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.45f);
        backdropImage.raycastTarget = true;

        var panelObject = new GameObject("InventoryPanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        // 화면의 약 절반 크기
        panelRect.anchorMin = new Vector2(0.25f, 0.25f);
        panelRect.anchorMax = new Vector2(0.75f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panelObject.AddComponent<Image>();
        UiPanelFrame.Apply(panelImage);

        var gridScrollObject = new GameObject("ItemScroll");
        gridScrollObject.transform.SetParent(panelObject.transform, false);
        var scrollRectTransform = gridScrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(36f, 88f);
        scrollRectTransform.offsetMax = new Vector2(-36f, -36f);

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(gridScrollObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;
        viewportObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        itemGridRect = contentObject.AddComponent<RectTransform>();
        itemGridRect.anchorMin = new Vector2(0f, 1f);
        itemGridRect.anchorMax = new Vector2(1f, 1f);
        itemGridRect.pivot = new Vector2(0.5f, 1f);
        itemGridRect.anchoredPosition = Vector2.zero;
        itemGridRect.sizeDelta = new Vector2(0f, 0f);

        var grid = contentObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96f, 112f);
        grid.spacing = new Vector2(12f, 12f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = gridScrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = itemGridRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var currencyObject = new GameObject("Currency");
        currencyObject.transform.SetParent(panelObject.transform, false);
        var currencyRect = currencyObject.AddComponent<RectTransform>();
        currencyRect.anchorMin = new Vector2(0f, 0f);
        currencyRect.anchorMax = new Vector2(0.5f, 0f);
        currencyRect.pivot = new Vector2(0f, 0f);
        currencyRect.anchoredPosition = new Vector2(16f, 12f);
        currencyRect.sizeDelta = new Vector2(360f, 48f);

        goldText = CreateCurrencyLine(currencyObject.transform, "GoldText", new Vector2(0f, 0.5f), new Vector2(1f, 1f));
        reputationText = CreateCurrencyLine(currencyObject.transform, "ReputationText", new Vector2(0f, 0f), new Vector2(1f, 0.5f));
        goldText.text = "골드 0";
        reputationText.text = "명성 0";
    }

    private Text CreateCurrencyLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        var lineRect = lineObject.AddComponent<RectTransform>();
        lineRect.anchorMin = anchorMin;
        lineRect.anchorMax = anchorMax;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;

        var text = lineObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        return text;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
