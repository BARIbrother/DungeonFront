using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 맵 기계 클릭 시 현재 레시피·포트·인벤 UI.
// 레시피 교체 목록은 현재 레시피를 눌렀을 때만 오른쪽에 연다.
public class MachineRecipeUI : MonoBehaviour
{
    private static MachineRecipeUI instance;

    // 가로·세로 각 √2배 → 면적 약 2배
    private const float UiScale = 1.41421356f;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform panelRect;
    private RectTransform contentListRect;
    private RectTransform recipePickerPanel;
    private RectTransform recipePickerListRect;
    private readonly List<GameObject> dynamicRows = new();
    private readonly List<GameObject> recipePickerRows = new();
    private Machine targetMachine;
    private Font uiFont;
    private ItemManager itemManager;
    private PlayerInventory subscribedInventory;
    private bool recipePickerOpen;
    private Image progressFillImage;
    private Text progressLabelText;
    private int lastMachineViewHash;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<MachineRecipeUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineRecipeUISystem");
        systemObject.AddComponent<MachineRecipeUI>();
    }

    public static void ShowFor(Machine machine)
    {
        if (machine == null)
        {
            return;
        }

        if (!machine.SupportsRecipeSelectionUi() && !machine.SupportsInventoryTransferUi())
        {
            return;
        }

        EnsureInstance();
        instance.Open(machine);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<MachineRecipeUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineRecipeUISystem");
        instance = systemObject.AddComponent<MachineRecipeUI>();
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

    private void Open(Machine machine)
    {
        targetMachine = machine;
        itemManager = FindAnyObjectByType<ItemManager>();
        recipePickerOpen = false;
        SetRecipePickerVisible(false);
        SubscribeInventory();
        // 비활성 상태에서 레이아웃을 만들면 슬롯 크기가 안 잡히는 경우가 있다.
        modalRoot.SetActive(true);
        RebuildContent();
    }

    private void Update()
    {
        if (modalRoot == null || !modalRoot.activeSelf || targetMachine == null)
        {
            return;
        }

        RefreshProgressUi();

        int viewHash = ComputeMachineViewHash();
        if (viewHash != lastMachineViewHash)
        {
            RebuildContent();
        }
    }

    public void Hide()
    {
        UnsubscribeInventory();
        targetMachine = null;
        lastMachineViewHash = 0;
        recipePickerOpen = false;
        if (recipePickerPanel != null)
        {
            recipePickerPanel.gameObject.SetActive(false);
        }

        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void SubscribeInventory()
    {
        UnsubscribeInventory();
        subscribedInventory = PlayerInventory.Instance != null
            ? PlayerInventory.Instance
            : FindAnyObjectByType<PlayerInventory>();
        if (subscribedInventory != null)
        {
            subscribedInventory.OnItemsChanged += HandleInventoryChanged;
        }
    }

    private void UnsubscribeInventory()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory.OnItemsChanged -= HandleInventoryChanged;
            subscribedInventory = null;
        }
    }

    private void HandleInventoryChanged()
    {
        if (modalRoot != null && modalRoot.activeSelf && targetMachine != null)
        {
            RebuildContent();
        }
    }

    private void RebuildContent()
    {
        ClearDynamicRows();
        progressFillImage = null;
        progressLabelText = null;

        if (targetMachine == null || contentListRect == null)
        {
            lastMachineViewHash = 0;
            return;
        }

        WarmItemCacheFromMachine();

        if (targetMachine.SupportsRecipeSelectionUi())
        {
            CreateCurrentRecipeButton();
            CreateProgressBar();
        }

        if (targetMachine.SupportsInventoryTransferUi())
        {
            CreatePortRow();
            RebuildInventoryButtons();
        }

        if (recipePickerOpen)
        {
            RebuildRecipePicker();
        }

        lastMachineViewHash = ComputeMachineViewHash();
        Canvas.ForceUpdateCanvases();
    }

    // 포트·레시피·WIP 여부 등, 연 채로 바뀔 수 있는 표시 상태를 해시한다.
    private int ComputeMachineViewHash()
    {
        if (targetMachine == null)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;
            Recipe selected = targetMachine.GetSelectedRecipe();
            hash = hash * 31 + (selected != null && selected.id != null
                ? selected.id.GetHashCode()
                : 0);
            hash = hash * 31 + (targetMachine.HasActiveWip ? 1 : 0);
            hash = HashPort(hash, targetMachine.inputPort);
            hash = HashPort(hash, targetMachine.outputPort);

            PlayerInventory inventory = GetInventory();
            if (inventory != null)
            {
                hash = hash * 31 + inventory.ComputeOwnedItemsHash();
            }

            return hash;
        }
    }

    private static int HashPort(int hash, ItemEntryList port)
    {
        if (port?.entries == null)
        {
            return hash * 31;
        }

        hash = hash * 31 + port.entries.Length;
        for (int i = 0; i < port.entries.Length; i++)
        {
            ItemEntry entry = port.entries[i];
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                hash = hash * 31;
                continue;
            }

            hash = hash * 31 + HashItemState(entry.item);
            hash = hash * 31 + entry.count;
        }

        return hash;
    }

    private static int HashItemState(Item item)
    {
        if (item == null)
        {
            return 0;
        }

        unchecked
        {
            int hash = item.Id != null ? item.Id.GetHashCode() : 0;
            hash = hash * 31 + item.ResolvedLevel;
            IReadOnlyList<Enchantment> enchantments = item.Enchantments;
            int count = enchantments != null ? enchantments.Count : 0;
            hash = hash * 31 + count;
            for (int i = 0; i < count; i++)
            {
                Enchantment enchantment = enchantments[i];
                hash = hash * 31 + (int)enchantment.attribute;
                hash = hash * 31 + (int)enchantment.form;
            }

            return hash;
        }
    }

    // 레시피·포트 ItemDefinition을 ItemManager에 올려 아이콘 조회가 되게 한다.
    private void WarmItemCacheFromMachine()
    {
        if (targetMachine == null)
        {
            return;
        }

        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        RegisterPortItems(targetMachine.inputPort);
        RegisterPortItems(targetMachine.outputPort);

        Recipe selected = targetMachine.GetSelectedRecipe();
        RegisterRecipeItems(selected);

        RecipePool pool = targetMachine.GetAvailableRecipes();
        if (pool?.recipes == null)
        {
            return;
        }

        for (int i = 0; i < pool.recipes.Length; i++)
        {
            RegisterRecipeItems(pool.recipes[i]);
        }
    }

    private void RegisterRecipeItems(Recipe recipe)
    {
        if (recipe == null)
        {
            return;
        }

        RegisterPortItems(recipe.inputEntryList);
        RegisterPortItems(recipe.outputEntryList);
    }

    private void RegisterPortItems(ItemEntryList port)
    {
        if (itemManager == null || port?.entries == null)
        {
            return;
        }

        for (int i = 0; i < port.entries.Length; i++)
        {
            ItemEntry entry = port.entries[i];
            if (entry?.item?.definition != null)
            {
                itemManager.Register(entry.item.definition);
            }
        }
    }

    private void CreateCurrentRecipeButton()
    {
        Recipe selected = targetMachine.GetSelectedRecipe();
        Color color = selected != null
            ? new Color(0.28f, 0.42f, 0.62f, 1f)
            : new Color(0.32f, 0.28f, 0.28f, 1f);

        var buttonObject = new GameObject("CurrentRecipe");
        buttonObject.transform.SetParent(contentListRect, false);
        float recipeRowHeight = 88f * UiScale;
        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = recipeRowHeight;
        layoutElement.preferredHeight = recipeRowHeight;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = color;

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(ToggleRecipePicker);

        var row = new GameObject("RecipeIcons");
        row.transform.SetParent(buttonObject.transform, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = Vector2.zero;
        rowRect.anchorMax = Vector2.one;
        rowRect.offsetMin = new Vector2(10f, 8f);
        rowRect.offsetMax = new Vector2(-10f, -8f);

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        if (selected == null)
        {
            CreateInlineLabel(row.transform, "—");
        }
        else
        {
            AddRecipeEntryIcons(row.transform, selected.inputEntryList);
            CreateInlineLabel(row.transform, "→");
            AddRecipeEntryIcons(row.transform, selected.outputEntryList);
        }

        dynamicRows.Add(buttonObject);
    }

    private void CreateProgressBar()
    {
        var progressObject = new GameObject("Progress");
        progressObject.transform.SetParent(contentListRect, false);
        float progressHeight = 28f * UiScale;
        var layoutElement = progressObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = progressHeight;
        layoutElement.preferredHeight = progressHeight;

        var background = progressObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(progressObject.transform, false);
        var fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        progressFillImage = fillObject.AddComponent<Image>();
        progressFillImage.color = new Color(0.35f, 0.75f, 0.45f, 1f);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(progressObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        progressLabelText = labelObject.AddComponent<Text>();
        progressLabelText.font = uiFont;
        progressLabelText.fontSize = Mathf.RoundToInt(15f * UiScale);
        progressLabelText.alignment = TextAnchor.MiddleCenter;
        progressLabelText.color = Color.white;
        progressLabelText.raycastTarget = false;

        dynamicRows.Add(progressObject);
        RefreshProgressUi();
    }

    private void RefreshProgressUi()
    {
        if (progressFillImage == null || progressLabelText == null || targetMachine == null)
        {
            return;
        }

        float normalized = targetMachine.GetProductionProgressNormalized();
        int current = targetMachine.ProgressTicks;
        int total = targetMachine.GetRecipeTime();

        RectTransform fillRect = progressFillImage.rectTransform;
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(normalized), 1f);

        if (total <= 0)
        {
            progressLabelText.text = "—";
            return;
        }

        if (!targetMachine.HasActiveWip)
        {
            progressLabelText.text = $"0 / {total}";
            return;
        }

        progressLabelText.text = $"{current} / {total}";
    }

    private void ToggleRecipePicker()
    {
        if (!targetMachine.SupportsRecipeSelectionUi())
        {
            return;
        }

        recipePickerOpen = !recipePickerOpen;
        SetRecipePickerVisible(recipePickerOpen);
        if (recipePickerOpen)
        {
            RebuildRecipePicker();
        }
    }

    private void SetRecipePickerVisible(bool visible)
    {
        if (recipePickerPanel == null)
        {
            return;
        }

        recipePickerPanel.gameObject.SetActive(visible);
    }

    private void RebuildRecipePicker()
    {
        ClearRecipePickerRows();
        if (recipePickerListRect == null || targetMachine == null)
        {
            return;
        }

        RecipePool pool = targetMachine.GetAvailableRecipes();
        Recipe[] recipes = pool != null ? pool.recipes : null;
        if (recipes == null || recipes.Length == 0)
        {
            CreatePickerInfoLabel("사용 가능한 레시피가 없습니다.");
            return;
        }

        Recipe selectedRecipe = targetMachine.GetSelectedRecipe();
        foreach (Recipe recipe in recipes)
        {
            if (recipe == null)
            {
                continue;
            }

            bool selected = recipe == selectedRecipe;
            CreateRecipePickerButton(recipe, selected);
        }
    }

    private void CreateRecipePickerButton(Recipe recipe, bool selected)
    {
        Color color = selected
            ? new Color(0.35f, 0.55f, 0.85f, 1f)
            : new Color(0.2f, 0.22f, 0.28f, 1f);

        var buttonObject = new GameObject("PickerRecipe");
        buttonObject.transform.SetParent(recipePickerListRect, false);
        float pickerRowHeight = 80f * UiScale;
        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = pickerRowHeight;
        layoutElement.preferredHeight = pickerRowHeight;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = color;

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => OnRecipePicked(recipe));

        var row = new GameObject("Icons");
        row.transform.SetParent(buttonObject.transform, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = Vector2.zero;
        rowRect.anchorMax = Vector2.one;
        rowRect.offsetMin = new Vector2(10f, 8f);
        rowRect.offsetMax = new Vector2(-10f, -8f);

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 4f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        AddRecipeEntryIcons(row.transform, recipe.inputEntryList);
        CreateInlineLabel(row.transform, "→");
        AddRecipeEntryIcons(row.transform, recipe.outputEntryList);

        recipePickerRows.Add(buttonObject);
    }

    private void OnRecipePicked(Recipe recipe)
    {
        if (targetMachine == null || recipe == null)
        {
            return;
        }

        targetMachine.SelectRecipe(recipe);
        recipePickerOpen = false;
        SetRecipePickerVisible(false);
        RebuildContent();
    }

    // 입력 포트는 왼쪽, 출력 포트는 오른쪽. 레시피 슬롯마다 아이콘을 항상 그린다.
    private void CreatePortRow()
    {
        var rowObject = new GameObject("PortRow", typeof(RectTransform));
        rowObject.transform.SetParent(contentListRect, false);

        float slotSize = 84f * UiScale;
        float portRowHeight = slotSize + 12f;
        var layoutElement = rowObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = portRowHeight;
        layoutElement.preferredHeight = portRowHeight;

        var rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.padding = new RectOffset(4, 4, 4, 4);
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        Recipe recipe = targetMachine.GetSelectedRecipe();
        CreatePortSideButtons(
            rowObject.transform,
            targetMachine.inputPort,
            recipe?.inputEntryList,
            isInput: true,
            slotSize);

        var arrowObject = new GameObject("Arrow", typeof(RectTransform));
        var arrowRect = (RectTransform)arrowObject.transform;
        arrowRect.SetParent(rowObject.transform, false);
        var arrowLayout = arrowObject.AddComponent<LayoutElement>();
        arrowLayout.minWidth = 40f * UiScale;
        arrowLayout.preferredWidth = 40f * UiScale;
        arrowLayout.minHeight = slotSize;
        arrowLayout.preferredHeight = slotSize;
        arrowLayout.flexibleWidth = 0f;
        var arrowText = arrowObject.AddComponent<Text>();
        arrowText.font = uiFont;
        arrowText.fontSize = Mathf.RoundToInt(24f * UiScale);
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        arrowText.text = "→";

        CreatePortSideButtons(
            rowObject.transform,
            targetMachine.outputPort,
            recipe?.outputEntryList,
            isInput: false,
            slotSize);

        InsertPortSpacer(rowObject.transform, 0);
        InsertPortSpacer(rowObject.transform, rowObject.transform.childCount);

        dynamicRows.Add(rowObject);
    }

    private static void InsertPortSpacer(Transform row, int siblingIndex)
    {
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(row, false);
        spacer.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, row.childCount - 1));
        var layout = spacer.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minWidth = 0f;
        layout.preferredWidth = 0f;
    }

    private void CreatePortSideButtons(
        Transform parent,
        ItemEntryList port,
        ItemEntryList recipeSide,
        bool isInput,
        float slotSize)
    {
        int slotCount = 0;
        if (port?.entries != null)
        {
            slotCount = port.entries.Length;
        }

        if (slotCount <= 0 && recipeSide?.entries != null)
        {
            slotCount = recipeSide.entries.Length;
        }

        if (slotCount <= 0)
        {
            CreatePortSlot(
                parent,
                expectedItem: null,
                storedItem: null,
                count: 0,
                isInput,
                slotSize);
            return;
        }

        for (int i = 0; i < slotCount; i++)
        {
            Item expected = GetEntryItem(recipeSide, i);
            ItemEntry stored = GetPortEntry(port, i);
            Item storedItem = stored != null && stored.count > 0 ? stored.item : null;
            int count = storedItem != null ? stored.count : 0;

            CreatePortSlot(parent, expected, storedItem, count, isInput, slotSize);
        }
    }

    private static ItemEntry GetPortEntry(ItemEntryList port, int index)
    {
        if (port?.entries == null || index < 0 || index >= port.entries.Length)
        {
            return null;
        }

        return port.entries[index];
    }

    private static Item GetEntryItem(ItemEntryList list, int index)
    {
        if (list?.entries == null || index < 0 || index >= list.entries.Length)
        {
            return null;
        }

        ItemEntry entry = list.entries[index];
        return entry != null ? entry.item : null;
    }

    private void CreatePortSlot(
        Transform parent,
        Item expectedItem,
        Item storedItem,
        int count,
        bool isInput,
        float slotSize)
    {
        Item displayItem = storedItem != null ? storedItem : expectedItem;
        bool hasStored = storedItem != null && count > 0;
        Color slotColor = isInput
            ? new Color(0.22f, 0.32f, 0.28f, 1f)
            : new Color(0.32f, 0.28f, 0.22f, 1f);

        if (!hasStored)
        {
            slotColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        }

        var buttonObject = new GameObject("PortSlot", typeof(RectTransform));
        var rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(slotSize, slotSize);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = slotSize;
        layoutElement.preferredWidth = slotSize;
        layoutElement.minHeight = slotSize;
        layoutElement.preferredHeight = slotSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = slotColor;

        if (hasStored)
        {
            Item withdrawItem = storedItem;
            int withdrawCount = count;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() =>
            {
                if (isInput)
                {
                    TryWithdrawInput(withdrawItem, withdrawCount);
                }
                else
                {
                    TryWithdrawOutput(withdrawItem, withdrawCount);
                }
            });
        }

        CreateItemIconVisual(
            rect,
            displayItem,
            count,
            slotSize,
            iconAlpha: hasStored ? 1f : 0.45f);
    }

    private void CreateItemIconButton(
        Transform parent,
        Item item,
        int count,
        Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        CreateItemIconButton(parent, item, count, color, 84f * UiScale, onClick);
    }

    private void CreateItemIconButton(
        Transform parent,
        Item item,
        int count,
        Color color,
        float slotSize,
        UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject("ItemIcon", typeof(RectTransform));
        var rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(slotSize, slotSize);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = slotSize;
        layoutElement.preferredWidth = slotSize;
        layoutElement.minHeight = slotSize;
        layoutElement.preferredHeight = slotSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = color;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        CreateItemIconVisual(buttonObject.transform, item, count, slotSize);
    }

    private void CreateItemIconVisual(Transform parent, Item item, int count)
    {
        CreateItemIconVisual(parent, item, count, 84f * UiScale, iconAlpha: 1f);
    }

    private void CreateItemIconVisual(Transform parent, Item item, int count, float slotSize)
    {
        CreateItemIconVisual(parent, item, count, slotSize, iconAlpha: 1f);
    }

    private void CreateItemIconVisual(
        Transform parent,
        Item item,
        int count,
        float slotSize,
        float iconAlpha)
    {
        string itemId = item != null ? item.Id : null;
        Sprite icon = ResolveItemIcon(item);
        if (icon == null && !string.IsNullOrEmpty(itemId))
        {
            icon = ItemIconResolver.ResolveById(itemId);
        }

        float iconSize = Mathf.Max(32f, slotSize * 0.62f);

        var iconObject = new GameObject("Icon", typeof(RectTransform));
        var iconRect = (RectTransform)iconObject.transform;
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.58f);
        iconRect.anchorMax = new Vector2(0.5f, 0.58f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.localScale = Vector3.one;

        var iconImage = iconObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.type = Image.Type.Simple;
        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(iconAlpha));
            iconImage.enabled = true;
        }
        else
        {
            iconImage.color = new Color(0.35f, 0.35f, 0.4f, Mathf.Clamp01(iconAlpha));
        }

        var countObject = new GameObject("Count", typeof(RectTransform));
        var countRect = (RectTransform)countObject.transform;
        countRect.SetParent(parent, false);
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0.3f);
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;
        countRect.localScale = Vector3.one;

        var countText = countObject.AddComponent<Text>();
        countText.font = uiFont;
        countText.fontSize = Mathf.RoundToInt(Mathf.Max(12f, 14f * UiScale));
        countText.alignment = TextAnchor.MiddleCenter;
        countText.color = new Color(1f, 1f, 1f, Mathf.Clamp01(iconAlpha + 0.2f));
        countText.raycastTarget = false;
        countText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countText.verticalOverflow = VerticalWrapMode.Overflow;
        countText.text = $"x{count}";
    }

    private void AddRecipeEntryIcons(Transform parent, ItemEntryList list)
    {
        if (list?.entries == null || list.entries.Length == 0)
        {
            CreateInlineLabel(parent, "-");
            return;
        }

        bool any = false;
        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            any = true;
            float slotWidth = 64f * UiScale;
            float slotHeight = 72f * UiScale;
            var slot = new GameObject("RecipeItem", typeof(RectTransform));
            var slotRect = (RectTransform)slot.transform;
            slotRect.SetParent(parent, false);
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotHeight);

            var layout = slot.AddComponent<LayoutElement>();
            layout.minWidth = slotWidth;
            layout.preferredWidth = slotWidth;
            layout.minHeight = slotHeight;
            layout.preferredHeight = slotHeight;
            layout.flexibleWidth = 0f;
            CreateItemIconVisual(slot.transform, entry.item, entry.count, slotHeight);
        }

        if (!any)
        {
            CreateInlineLabel(parent, "-");
        }
    }

    private void CreateInlineLabel(Transform parent, string message)
    {
        var labelObject = new GameObject("InlineLabel");
        labelObject.transform.SetParent(parent, false);
        var layout = labelObject.AddComponent<LayoutElement>();
        layout.minWidth = 28f * UiScale;
        layout.preferredWidth = 32f * UiScale;
        layout.minHeight = 48f * UiScale;
        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = Mathf.RoundToInt(20f * UiScale);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = message;
    }

    private void RebuildInventoryButtons()
    {
        PlayerInventory inventory = GetInventory();
        if (inventory == null)
        {
            return;
        }

        List<ItemEntry> owned = inventory.GetOwnedItemEntries();
        if (owned.Count == 0)
        {
            return;
        }

        var rowObject = new GameObject("InventoryGrid");
        rowObject.transform.SetParent(contentListRect, false);
        var layoutElement = rowObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 100f * UiScale;

        float cell = 88f * UiScale;
        var grid = rowObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperLeft;

        bool any = false;
        for (int i = 0; i < owned.Count; i++)
        {
            ItemEntry ownedEntry = owned[i];
            Item item = ownedEntry?.item;
            if (item == null || item.Category == ItemCategory.Currency || ownedEntry.count <= 0)
            {
                continue;
            }

            any = true;
            Item depositItem = item;
            int displayCount = ownedEntry.count;
            CreateItemIconButton(
                rowObject.transform,
                depositItem,
                displayCount,
                new Color(0.28f, 0.3f, 0.4f, 1f),
                () => TryDepositOne(depositItem));
        }

        if (!any)
        {
            Destroy(rowObject);
            return;
        }

        int rows = Mathf.CeilToInt(rowObject.transform.childCount / 5f);
        layoutElement.minHeight = rows * (cell + 8f);
        layoutElement.preferredHeight = layoutElement.minHeight;
        dynamicRows.Add(rowObject);
    }

    private void TryDepositOne(Item item)
    {
        if (targetMachine == null || item == null || string.IsNullOrEmpty(item.Id))
        {
            return;
        }

        PlayerInventory inventory = GetInventory();
        if (inventory == null || inventory.GetCount(item) <= 0)
        {
            return;
        }

        var entry = new ItemEntry { item = item.Clone(), count = 1 };
        if (!targetMachine.PutintoInputPort(entry))
        {
            return;
        }

        inventory.Remove(item, 1);
        RebuildContent();
    }

    private void TryWithdrawInput(Item item, int count)
    {
        if (targetMachine == null || item == null || count <= 0)
        {
            return;
        }

        PlayerInventory inventory = GetInventory();
        if (inventory == null)
        {
            return;
        }

        var entry = new ItemEntry { item = item.Clone(), count = count };
        if (!targetMachine.TakeoutInputPort(entry))
        {
            return;
        }

        inventory.Add(new ItemEntry { item = item.Clone(), count = count });
        RebuildContent();
    }

    private void TryWithdrawOutput(Item item, int count)
    {
        if (targetMachine == null || item == null || count <= 0)
        {
            return;
        }

        PlayerInventory inventory = GetInventory();
        if (inventory == null)
        {
            return;
        }

        var entry = new ItemEntry { item = item.Clone(), count = count };
        if (!targetMachine.TakeoutOutputPort(entry))
        {
            return;
        }

        inventory.Add(new ItemEntry { item = item.Clone(), count = count });
        RebuildContent();
    }

    private PlayerInventory GetInventory()
    {
        if (PlayerInventory.Instance != null)
        {
            return PlayerInventory.Instance;
        }

        return FindAnyObjectByType<PlayerInventory>();
    }

    private Item ResolveItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        PlayerInventory inventory = GetInventory();
        Item fromInventory = inventory != null ? inventory.GetItem(itemId) : null;
        if (fromInventory != null)
        {
            return fromInventory.Clone();
        }

        ItemDefinition definition = ResolveItemDefinition(itemId);
        return Item.FromDefinition(definition);
    }

    private ItemDefinition ResolveItemDefinition(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        ItemDefinition fromManager = itemManager != null ? itemManager.Get(itemId) : null;
        if (fromManager != null)
        {
            return fromManager;
        }

        PlayerInventory inventory = GetInventory();
        ItemDefinition found = inventory != null ? inventory.GetDefinition(itemId) : null;
        if (found == null)
        {
            found = FindItemDefinitionOnTargetMachine(itemId);
        }

        if (found != null && itemManager != null)
        {
            itemManager.Register(found);
        }

        return found;
    }

    private ItemDefinition ResolveItemDefinition(Item item)
    {
        if (item?.definition == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(item.Id))
        {
            ItemDefinition resolved = ResolveItemDefinition(item.Id);
            if (resolved != null)
            {
                return resolved;
            }
        }

        return item.definition;
    }

    // SO에 아이콘이 없으면 Art/Items·ItemManager·인벤 캐시에서 다시 찾는다.
    private Sprite ResolveItemIcon(Item item)
    {
        return ItemIconResolver.Resolve(ResolveItemDefinition(item));
    }

    private ItemDefinition FindItemDefinitionOnTargetMachine(string itemId)
    {
        if (targetMachine == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        Item found = FindItemInPort(targetMachine.inputPort, itemId)
            ?? FindItemInPort(targetMachine.outputPort, itemId);
        if (found?.definition != null)
        {
            return found.definition;
        }

        Recipe selected = targetMachine.GetSelectedRecipe();
        found = FindItemInRecipe(selected, itemId);
        if (found?.definition != null)
        {
            return found.definition;
        }

        RecipePool pool = targetMachine.GetAvailableRecipes();
        if (pool?.recipes == null)
        {
            return null;
        }

        for (int i = 0; i < pool.recipes.Length; i++)
        {
            found = FindItemInRecipe(pool.recipes[i], itemId);
            if (found?.definition != null)
            {
                return found.definition;
            }
        }

        return null;
    }

    private static Item FindItemInPort(ItemEntryList port, string itemId)
    {
        if (port?.entries == null)
        {
            return null;
        }

        for (int i = 0; i < port.entries.Length; i++)
        {
            ItemEntry entry = port.entries[i];
            if (entry?.item != null && entry.item.Id == itemId)
            {
                return entry.item;
            }
        }

        return null;
    }

    private static Item FindItemInRecipe(Recipe recipe, string itemId)
    {
        if (recipe == null)
        {
            return null;
        }

        return FindItemInPort(recipe.inputEntryList, itemId)
            ?? FindItemInPort(recipe.outputEntryList, itemId);
    }

    private void CreateInfoLabel(string message)
    {
        var labelObject = new GameObject("InfoLabel");
        labelObject.transform.SetParent(contentListRect, false);

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 36f;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        label.text = message;

        dynamicRows.Add(labelObject);
    }

    private void CreatePickerInfoLabel(string message)
    {
        var labelObject = new GameObject("PickerInfo");
        labelObject.transform.SetParent(recipePickerListRect, false);
        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 36f;
        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        label.text = message;
        recipePickerRows.Add(labelObject);
    }

    private void ClearDynamicRows()
    {
        foreach (GameObject row in dynamicRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        dynamicRows.Clear();
    }

    private void ClearRecipePickerRows()
    {
        foreach (GameObject row in recipePickerRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }

        recipePickerRows.Clear();
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("MachineRecipeCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("RecipeModal");
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

        var backdropButton = backdropObject.AddComponent<Button>();
        backdropButton.onClick.AddListener(Hide);

        var panelObject = new GameObject("RecipePanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-8f, 0f);
        panelRect.sizeDelta = new Vector2(460f * UiScale, 520f * UiScale);

        var panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        CreateCloseButton(panelObject.transform);

        contentListRect = CreateScrollContent(
            panelObject.transform,
            "ContentScroll",
            new Vector2(12f, 12f),
            new Vector2(-12f, -52f));

        CreateRecipePickerPanel();
    }

    private void CreateRecipePickerPanel()
    {
        var pickerObject = new GameObject("RecipePickerPanel");
        pickerObject.transform.SetParent(modalRoot.transform, false);
        recipePickerPanel = pickerObject.AddComponent<RectTransform>();
        recipePickerPanel.anchorMin = new Vector2(0.5f, 0.5f);
        recipePickerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        recipePickerPanel.pivot = new Vector2(0f, 0.5f);
        recipePickerPanel.anchoredPosition = new Vector2(8f, 0f);
        recipePickerPanel.sizeDelta = new Vector2(360f * UiScale, 520f * UiScale);

        var pickerImage = pickerObject.AddComponent<Image>();
        pickerImage.color = new Color(0.12f, 0.13f, 0.16f, 0.97f);

        recipePickerListRect = CreateScrollContent(
            pickerObject.transform,
            "PickerScroll",
            new Vector2(12f, 12f),
            new Vector2(-12f, -12f));

        pickerObject.SetActive(false);
    }

    private RectTransform CreateScrollContent(
        Transform parent,
        string scrollName,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var scrollObject = new GameObject(scrollName);
        scrollObject.transform.SetParent(parent, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = offsetMin;
        scrollRectTransform.offsetMax = offsetMax;

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        // Mask는 중첩 슬롯 아이콘을 잘라먹을 수 있어 RectMask2D만 쓴다.
        viewportObject.AddComponent<RectMask2D>();

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        var contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return contentRect;
    }

    private void CreateCloseButton(Transform parent)
    {
        var closeObject = new GameObject("CloseButton");
        closeObject.transform.SetParent(parent, false);
        var closeRect = closeObject.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-8f, -8f);
        closeRect.sizeDelta = new Vector2(40f, 40f);

        var closeImage = closeObject.AddComponent<Image>();
        closeImage.color = new Color(0.28f, 0.3f, 0.36f, 1f);

        var closeButton = closeObject.AddComponent<Button>();
        closeButton.onClick.AddListener(Hide);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(closeObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "×";
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
