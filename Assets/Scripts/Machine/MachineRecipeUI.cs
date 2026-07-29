using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 맵 기계 클릭 시 레시피 선택·인벤 ↔ 포트 넣고 빼기 UI.
public class MachineRecipeUI : MonoBehaviour
{
    private static MachineRecipeUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform panelRect;
    private RectTransform contentListRect;
    private Text titleText;
    private readonly List<GameObject> dynamicRows = new();
    private Machine targetMachine;
    private Font uiFont;
    private ItemManager itemManager;
    private PlayerInventory subscribedInventory;

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
        titleText.text = machine.GetMachineDisplayName();
        SubscribeInventory();
        RebuildContent();
        modalRoot.SetActive(true);
    }

    public void Hide()
    {
        UnsubscribeInventory();
        targetMachine = null;
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
            subscribedInventory.OnItemsChanged += RebuildContent;
        }
    }

    private void UnsubscribeInventory()
    {
        if (subscribedInventory != null)
        {
            subscribedInventory.OnItemsChanged -= RebuildContent;
            subscribedInventory = null;
        }
    }

    private void RebuildContent()
    {
        ClearDynamicRows();

        if (targetMachine == null || contentListRect == null)
        {
            return;
        }

        if (targetMachine.SupportsRecipeSelectionUi())
        {
            CreateSectionLabel("레시피");
            RebuildRecipeButtons();
        }

        if (targetMachine.SupportsInventoryTransferUi())
        {
            CreateSectionLabel("입력 포트 (클릭: 인벤으로)");
            RebuildPortButtons(targetMachine.inputPort, isInput: true);

            CreateSectionLabel("출력 포트 (클릭: 인벤으로)");
            RebuildPortButtons(targetMachine.outputPort, isInput: false);

            CreateSectionLabel("인벤토리 (클릭: 입력에 넣기)");
            RebuildInventoryButtons();
        }
    }

    private void RebuildRecipeButtons()
    {
        RecipePool pool = targetMachine.GetAvailableRecipes();
        Recipe[] recipes = pool != null ? pool.recipes : null;
        if (recipes == null || recipes.Length == 0)
        {
            CreateInfoLabel("사용 가능한 레시피가 없습니다.");
            return;
        }

        Recipe selectedRecipe = targetMachine.GetSelectedRecipe();
        foreach (Recipe recipe in recipes)
        {
            if (recipe == null)
            {
                continue;
            }

            CreateRecipeButton(recipe, recipe == selectedRecipe);
        }
    }

    private void RebuildPortButtons(ItemEntryList port, bool isInput)
    {
        if (port?.entries == null)
        {
            CreateInfoLabel("포트 없음");
            return;
        }

        bool any = false;
        for (int i = 0; i < port.entries.Length; i++)
        {
            ItemEntry entry = port.entries[i];
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            any = true;
            ItemDefinition item = entry.item;
            int count = entry.count;
            string label = $"{DescribeItem(item)} x{count}";
            CreateActionButton(label, new Color(0.22f, 0.32f, 0.28f, 1f), () =>
            {
                if (isInput)
                {
                    TryWithdrawInput(item, count);
                }
                else
                {
                    TryWithdrawOutput(item, count);
                }
            });
        }

        if (!any)
        {
            CreateInfoLabel("(비어 있음)");
        }
    }

    private void RebuildInventoryButtons()
    {
        PlayerInventory inventory = GetInventory();
        if (inventory == null)
        {
            CreateInfoLabel("PlayerInventory 없음");
            return;
        }

        List<KeyValuePair<string, int>> owned = inventory.GetOwnedItemCounts();
        if (owned.Count == 0)
        {
            CreateInfoLabel("(인벤 비어 있음)");
            return;
        }

        bool any = false;
        foreach (KeyValuePair<string, int> pair in owned)
        {
            ItemDefinition item = ResolveItem(pair.Key);
            if (item == null)
            {
                continue;
            }

            any = true;
            int count = pair.Value;
            string label = $"{DescribeItem(item)} x{count}";
            CreateActionButton(label, new Color(0.28f, 0.3f, 0.4f, 1f), () => TryDepositOne(item));
        }

        if (!any)
        {
            CreateInfoLabel("표시할 아이템 정의 없음");
        }
    }

    private void TryDepositOne(ItemDefinition item)
    {
        if (targetMachine == null || item == null || string.IsNullOrEmpty(item.id))
        {
            return;
        }

        PlayerInventory inventory = GetInventory();
        if (inventory == null || inventory.GetCount(item.id) <= 0)
        {
            return;
        }

        var entry = new ItemEntry { item = item, count = 1 };
        if (!targetMachine.PutintoInputPort(entry))
        {
            return;
        }

        inventory.Remove(item.id, 1);
        RebuildContent();
    }

    private void TryWithdrawInput(ItemDefinition item, int count)
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

        var entry = new ItemEntry { item = item, count = count };
        if (!targetMachine.TakeoutInputPort(entry))
        {
            return;
        }

        inventory.Add(new ItemEntry { item = item, count = count });
        RebuildContent();
    }

    private void TryWithdrawOutput(ItemDefinition item, int count)
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

        var entry = new ItemEntry { item = item, count = count };
        if (!targetMachine.TakeoutOutputPort(entry))
        {
            return;
        }

        inventory.Add(new ItemEntry { item = item, count = count });
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

    private ItemDefinition ResolveItem(string itemId)
    {
        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        return itemManager != null ? itemManager.Get(itemId) : null;
    }

    private static string DescribeItem(ItemDefinition item)
    {
        if (item == null)
        {
            return "?";
        }

        return string.IsNullOrEmpty(item.displayName) ? item.id : item.displayName;
    }

    private void CreateSectionLabel(string message)
    {
        var labelObject = new GameObject("SectionLabel");
        labelObject.transform.SetParent(contentListRect, false);

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 28f;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 15;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = new Color(0.95f, 0.85f, 0.45f, 1f);
        label.text = message;

        dynamicRows.Add(labelObject);
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

    private void CreateRecipeButton(Recipe recipe, bool isSelected)
    {
        CreateActionButton(
            BuildRecipeLabel(recipe),
            isSelected ? new Color(0.35f, 0.55f, 0.85f, 1f) : new Color(0.2f, 0.22f, 0.28f, 1f),
            () => OnRecipeButtonClicked(recipe));
    }

    private void CreateActionButton(string labelText, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject("ActionButton");
        buttonObject.transform.SetParent(contentListRect, false);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 40f;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = color;

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 15;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = labelText;

        dynamicRows.Add(buttonObject);
    }

    private void OnRecipeButtonClicked(Recipe recipe)
    {
        if (targetMachine == null || recipe == null)
        {
            return;
        }

        targetMachine.SelectRecipe(recipe);
        RebuildContent();
    }

    private static string BuildRecipeLabel(Recipe recipe)
    {
        string recipeName = string.IsNullOrEmpty(recipe.id) ? "recipe" : recipe.id;
        string inputs = DescribeItemEntries(recipe.inputEntryList);
        string outputs = DescribeItemEntries(recipe.outputEntryList);
        return $"{recipeName}  ({inputs} → {outputs}, {recipe.recipeTime})";
    }

    private static string DescribeItemEntries(ItemEntryList list)
    {
        if (list?.entries == null || list.entries.Length == 0)
        {
            return "-";
        }

        var parts = new List<string>();
        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            parts.Add($"{DescribeItem(entry.item)}x{entry.count}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "-";
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
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(520f, 560f);

        var panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        var headerObject = new GameObject("Header");
        headerObject.transform.SetParent(panelObject.transform, false);
        var headerRect = headerObject.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 48f);
        headerRect.anchoredPosition = Vector2.zero;

        titleText = headerObject.AddComponent<Text>();
        titleText.font = uiFont;
        titleText.fontSize = 18;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;
        titleText.text = "기계";

        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = new Vector2(-56f, 0f);

        CreateCloseButton(panelObject.transform);

        var scrollObject = new GameObject("ContentScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(12f, 12f);
        scrollRectTransform.offsetMax = new Vector2(-12f, -60f);

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;
        viewportObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        contentListRect = contentObject.AddComponent<RectTransform>();
        contentListRect.anchorMin = new Vector2(0f, 1f);
        contentListRect.anchorMax = new Vector2(1f, 1f);
        contentListRect.pivot = new Vector2(0.5f, 1f);
        contentListRect.anchoredPosition = Vector2.zero;
        contentListRect.sizeDelta = new Vector2(0f, 0f);

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
        scroll.content = contentListRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
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
