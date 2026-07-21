using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 기계 선택 시 이름·레시피·WIP·상태를 보여주고, 레시피 변경 목록을 오른쪽에 연다.
public class MachineInfoPanel : MonoBehaviour
{
    private static MachineInfoPanel instance;

    private static readonly Color PanelFill = new Color(0.78f, 0.72f, 0.59f, 0.98f);
    private static readonly Color ButtonFill = new Color(0.85f, 0.78f, 0.65f, 1f);
    private static readonly Color ButtonSelected = new Color(0.72f, 0.82f, 0.55f, 1f);
    private static readonly Color TextDark = new Color(0.22f, 0.16f, 0.1f, 1f);
    private static readonly Color TextMuted = new Color(0.4f, 0.32f, 0.22f, 1f);
    private static readonly Color StatusIdle = new Color(0.45f, 0.55f, 0.65f, 1f);
    private static readonly Color StatusWorking = new Color(0.28f, 0.62f, 0.35f, 1f);
    private static readonly Color StatusBroken = new Color(0.78f, 0.22f, 0.2f, 1f);
    private static readonly Color BarTrack = new Color(0.35f, 0.28f, 0.2f, 1f);
    private static readonly Color BarFill = new Color(0.85f, 0.65f, 0.25f, 1f);
    private static readonly Color BrokenAlert = new Color(0.72f, 0.3f, 0.25f, 0.9f);
    private static readonly Color SlotFrame = new Color(0.45f, 0.36f, 0.26f, 1f);
    private static readonly Color SlotEmpty = new Color(0.55f, 0.48f, 0.38f, 0.85f);

    // 기준 레이아웃 대비 배율(가로·세로 각 2배 = 면적 4배).
    private const float UiScale = 2f;
    private const float InfoWidth = 320f * UiScale;
    private const float InfoHeight = 340f * UiScale;
    private const float ListWidth = 360f * UiScale;
    private const float ListGap = 16f * UiScale;
    private const float PortSlotSize = 40f * UiScale;

    private Canvas canvas;
    private GameObject panelRoot;
    private RectTransform infoPanelRect;
    private RectTransform glowRect;
    private RectTransform recipeListRect;
    private Image glowImage;
    private Text nameText;
    private Image statusBadgeImage;
    private Text statusBadgeText;
    private Text recipeNameText;
    private Text recipeIoText;
    private Image wipFillImage;
    private RectTransform wipFillRect;
    private Text wipText;
    private RectTransform inputSlotsRect;
    private RectTransform outputSlotsRect;
    private readonly List<PortSlotView> inputSlotViews = new();
    private readonly List<PortSlotView> outputSlotViews = new();
    private GameObject brokenAlertObject;
    private Button changeRecipeButton;
    private Text changeRecipeLabel;
    private GameObject recipeListRoot;
    private RectTransform recipeListContent;
    private readonly List<GameObject> recipeButtons = new();
    private Machine targetMachine;
    private Font uiFont;
    private bool recipeListOpen;

    private sealed class PortSlotView
    {
        public GameObject root;
        public Image iconImage;
        public Text countText;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<MachineInfoPanel>() != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineInfoPanelSystem");
        systemObject.AddComponent<MachineInfoPanel>();
    }

    public static void ShowFor(Machine machine)
    {
        if (machine == null || !machine.SupportsInfoPanel())
        {
            return;
        }

        EnsureInstance();
        instance.Open(machine, openRecipeList: false);
    }

    // 레시피 목록까지 바로 연다. MachineRecipeUI 호환 진입점.
    public static void ShowForWithRecipeList(Machine machine)
    {
        if (machine == null || !machine.SupportsInfoPanel())
        {
            return;
        }

        EnsureInstance();
        instance.Open(machine, openRecipeList: machine.SupportsRecipeSelectionUi());
    }

    public static void HideActive()
    {
        if (instance == null)
        {
            return;
        }

        instance.Hide();
    }

    // 회수·제거된 기계가 선택 중이면 패널을 닫는다.
    public static void NotifyMachineRemoved(Machine machine)
    {
        if (instance == null || machine == null)
        {
            return;
        }

        if (instance.targetMachine == machine)
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

        instance = FindAnyObjectByType<MachineInfoPanel>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineInfoPanelSystem");
        instance = systemObject.AddComponent<MachineInfoPanel>();
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
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
        {
            return;
        }

        // 회수·파괴된 기계가 남아 있으면 패널을 닫는다.
        if (targetMachine == null)
        {
            Hide();
            return;
        }

        RefreshContents();
        UpdateBrokenGlow();
    }

    private void Open(Machine machine, bool openRecipeList)
    {
        targetMachine = machine;
        recipeListOpen = false;
        SetRecipeListVisible(false);
        RefreshPanelLayout();
        RefreshContents();
        UpdateBrokenGlow();
        panelRoot.SetActive(true);

        if (openRecipeList && machine.SupportsRecipeSelectionUi() && !machine.IsBroken)
        {
            OpenRecipeList();
        }
    }

    private void Hide()
    {
        targetMachine = null;
        recipeListOpen = false;
        SetRecipeListVisible(false);
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void RefreshContents()
    {
        if (targetMachine == null)
        {
            return;
        }

        nameText.text = targetMachine.GetMachineDisplayName();

        MachineDisplayStatus status = ResolveStatus(targetMachine);
        ApplyStatusBadge(status);

        Recipe recipe = targetMachine.currentRecipe != null
            ? targetMachine.currentRecipe
            : targetMachine.GetSelectedRecipe();
        if (recipe == null)
        {
            recipeNameText.text = "-";
            recipeIoText.text = "-";
        }
        else
        {
            recipeNameText.text = string.IsNullOrEmpty(recipe.id) ? "recipe" : recipe.id;
            recipeIoText.text =
                $"{DescribeItemEntries(recipe.inputEntryList)}  →  {DescribeItemEntries(recipe.outputEntryList)}";
        }

        RefreshWip(status);
        RefreshPorts();
        RefreshBrokenAlert(status);
        RefreshChangeRecipeButton();
    }

    private void RefreshWip(MachineDisplayStatus status)
    {
        int recipeTime = targetMachine.currentRecipe != null
            ? targetMachine.currentRecipe.recipeTime
            : 0;
        int progress = targetMachine.ProgressTicks;
        bool hasWip = targetMachine.HasActiveWip;

        if (status == MachineDisplayStatus.Broken)
        {
            SetWipFill(hasWip && recipeTime > 0
                ? Mathf.Clamp01((float)progress / recipeTime)
                : 0f);
            wipText.text = "중단됨";
            return;
        }

        if (hasWip && recipeTime > 0)
        {
            SetWipFill(Mathf.Clamp01((float)progress / recipeTime));
            wipText.text = $"{progress} / {recipeTime}";
            return;
        }

        SetWipFill(0f);
        wipText.text = "대기 중";
    }

    // 스프라이트 없는 Image는 Filled가 동작하지 않아, 가로 앵커로 채운다.
    private void SetWipFill(float amount)
    {
        if (wipFillRect == null)
        {
            return;
        }

        amount = Mathf.Clamp01(amount);
        wipFillRect.anchorMin = Vector2.zero;
        wipFillRect.anchorMax = new Vector2(amount, 1f);
        wipFillRect.offsetMin = Vector2.zero;
        wipFillRect.offsetMax = Vector2.zero;
    }

    // inputPort(좌)·outputPort(우) 슬롯을 포트 길이만큼 아이콘·수량으로 갱신한다.
    private void RefreshPorts()
    {
        RefreshPortSlots(inputSlotViews, inputSlotsRect, targetMachine.inputPort);
        RefreshPortSlots(outputSlotViews, outputSlotsRect, targetMachine.outputPort);
    }

    private void RefreshPortSlots(
        List<PortSlotView> views,
        RectTransform container,
        ItemEntryList port)
    {
        if (container == null)
        {
            return;
        }

        int slotCount = GetPortSlotCount(port);
        EnsurePortSlotViews(views, container, slotCount);

        for (int i = 0; i < views.Count; i++)
        {
            PortSlotView view = views[i];
            if (i >= slotCount)
            {
                view.root.SetActive(false);
                continue;
            }

            view.root.SetActive(true);
            ItemEntry entry = GetPortEntry(port, i);
            if (entry?.item != null && entry.count > 0)
            {
                Sprite icon = entry.item.icon;
                view.iconImage.enabled = icon != null;
                view.iconImage.sprite = icon;
                view.iconImage.color = Color.white;
                view.countText.text = entry.count.ToString();
                view.countText.enabled = true;
            }
            else
            {
                view.iconImage.enabled = false;
                view.iconImage.sprite = null;
                view.countText.text = string.Empty;
                view.countText.enabled = false;
            }
        }
    }

    private static int GetPortSlotCount(ItemEntryList port)
    {
        if (port == null)
        {
            return 0;
        }

        if (port.length > 0)
        {
            return port.length;
        }

        return port.entries != null ? port.entries.Length : 0;
    }

    private static ItemEntry GetPortEntry(ItemEntryList port, int index)
    {
        if (port?.entries == null || index < 0 || index >= port.entries.Length)
        {
            return null;
        }

        return port.entries[index];
    }

    private void EnsurePortSlotViews(List<PortSlotView> views, RectTransform container, int slotCount)
    {
        while (views.Count < slotCount)
        {
            views.Add(CreatePortSlotView(container));
        }
    }

    private PortSlotView CreatePortSlotView(RectTransform container)
    {
        var slotObject = new GameObject("PortSlot");
        slotObject.transform.SetParent(container, false);

        var layoutElement = slotObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = PortSlotSize;
        layoutElement.minHeight = PortSlotSize;
        layoutElement.preferredWidth = PortSlotSize;
        layoutElement.preferredHeight = PortSlotSize;

        var frame = slotObject.AddComponent<Image>();
        frame.color = SlotFrame;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.12f, 0.22f);
        iconRect.anchorMax = new Vector2(0.88f, 0.88f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImage = iconObject.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.enabled = false;

        var countObject = new GameObject("Count");
        countObject.transform.SetParent(slotObject.transform, false);
        var countRect = countObject.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0.4f);
        countRect.offsetMin = new Vector2(2f * UiScale, 1f * UiScale);
        countRect.offsetMax = new Vector2(-2f * UiScale, 0f);
        var countText = countObject.AddComponent<Text>();
        countText.font = uiFont;
        countText.fontSize = Mathf.RoundToInt(12f * UiScale);
        countText.alignment = TextAnchor.LowerRight;
        countText.color = Color.white;
        countText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countText.verticalOverflow = VerticalWrapMode.Truncate;
        countText.enabled = false;

        // 빈 슬롯 속 배경
        var emptyObject = new GameObject("Empty");
        emptyObject.transform.SetParent(slotObject.transform, false);
        emptyObject.transform.SetAsFirstSibling();
        var emptyRect = emptyObject.AddComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0.08f, 0.08f);
        emptyRect.anchorMax = new Vector2(0.92f, 0.92f);
        emptyRect.offsetMin = Vector2.zero;
        emptyRect.offsetMax = Vector2.zero;
        emptyObject.AddComponent<Image>().color = SlotEmpty;

        return new PortSlotView
        {
            root = slotObject,
            iconImage = iconImage,
            countText = countText,
        };
    }

    private void RefreshBrokenAlert(MachineDisplayStatus status)
    {
        if (brokenAlertObject != null)
        {
            brokenAlertObject.SetActive(status == MachineDisplayStatus.Broken);
        }
    }

    private void RefreshChangeRecipeButton()
    {
        bool canChange = targetMachine != null
            && targetMachine.SupportsRecipeSelectionUi()
            && !targetMachine.IsBroken;
        changeRecipeButton.interactable = canChange;
        changeRecipeLabel.color = canChange ? TextDark : TextMuted;
    }

    private void ApplyStatusBadge(MachineDisplayStatus status)
    {
        switch (status)
        {
            case MachineDisplayStatus.Broken:
                statusBadgeImage.color = StatusBroken;
                statusBadgeText.text = "고장";
                break;
            case MachineDisplayStatus.Working:
                statusBadgeImage.color = StatusWorking;
                statusBadgeText.text = "가동";
                break;
            default:
                statusBadgeImage.color = StatusIdle;
                statusBadgeText.text = "대기";
                break;
        }
    }

    private static MachineDisplayStatus ResolveStatus(Machine machine)
    {
        if (machine.IsBroken)
        {
            return MachineDisplayStatus.Broken;
        }

        if (machine.HasActiveWip)
        {
            return MachineDisplayStatus.Working;
        }

        return MachineDisplayStatus.Idle;
    }

    private void UpdateBrokenGlow()
    {
        if (glowImage == null)
        {
            return;
        }

        bool broken = targetMachine != null && targetMachine.IsBroken;
        glowImage.gameObject.SetActive(broken);
        if (!broken)
        {
            return;
        }

        float pulse = 0.35f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f));
        glowImage.color = new Color(1f, 0.18f, 0.15f, pulse);
    }

    private void OnChangeRecipeClicked()
    {
        if (targetMachine == null || !targetMachine.SupportsRecipeSelectionUi() || targetMachine.IsBroken)
        {
            return;
        }

        if (recipeListOpen)
        {
            CloseRecipeList();
            return;
        }

        OpenRecipeList();
    }

    private void OpenRecipeList()
    {
        recipeListOpen = true;
        RebuildRecipeButtons();
        SetRecipeListVisible(true);
        RefreshPanelLayout();
    }

    private void CloseRecipeList()
    {
        recipeListOpen = false;
        SetRecipeListVisible(false);
        RefreshPanelLayout();
    }

    // 정보 패널·레시피 목록을 화면 중앙 기준으로 나란히 배치한다.
    private void RefreshPanelLayout()
    {
        if (infoPanelRect == null)
        {
            return;
        }

        if (recipeListOpen)
        {
            float totalWidth = InfoWidth + ListGap + ListWidth;
            float infoX = -totalWidth * 0.5f + InfoWidth * 0.5f;
            float listX = -totalWidth * 0.5f + InfoWidth + ListGap + ListWidth * 0.5f;
            infoPanelRect.anchoredPosition = new Vector2(infoX, 0f);
            if (recipeListRect != null)
            {
                recipeListRect.anchoredPosition = new Vector2(listX, 0f);
            }
        }
        else
        {
            infoPanelRect.anchoredPosition = Vector2.zero;
        }

        if (glowRect != null)
        {
            glowRect.anchoredPosition = infoPanelRect.anchoredPosition;
        }
    }

    private void SetRecipeListVisible(bool visible)
    {
        if (recipeListRoot != null)
        {
            recipeListRoot.SetActive(visible);
        }
    }

    private void RebuildRecipeButtons()
    {
        ClearRecipeButtons();

        if (targetMachine == null || recipeListContent == null)
        {
            return;
        }

        RecipePool pool = targetMachine.GetAvailableRecipes();
        Recipe[] recipes = pool != null ? pool.recipes : null;
        if (recipes == null || recipes.Length == 0)
        {
            CreateRecipeInfoLabel("사용 가능한 레시피가 없습니다.");
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

    private void CreateRecipeInfoLabel(string message)
    {
        var labelObject = new GameObject("InfoLabel");
        labelObject.transform.SetParent(recipeListContent, false);

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 40f * UiScale;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = Mathf.RoundToInt(14f * UiScale);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = TextMuted;
        label.text = message;

        recipeButtons.Add(labelObject);
    }

    private void CreateRecipeButton(Recipe recipe, bool isSelected)
    {
        var buttonObject = new GameObject($"Recipe_{recipe.id}");
        buttonObject.transform.SetParent(recipeListContent, false);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 44f * UiScale;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = isSelected ? ButtonSelected : ButtonFill;

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => OnRecipeButtonClicked(recipe));

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f * UiScale, 4f * UiScale);
        labelRect.offsetMax = new Vector2(-10f * UiScale, -4f * UiScale);

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = Mathf.RoundToInt(14f * UiScale);
        label.alignment = TextAnchor.MiddleLeft;
        label.color = TextDark;
        label.text = BuildRecipeLabel(recipe);

        recipeButtons.Add(buttonObject);
    }

    private void OnRecipeButtonClicked(Recipe recipe)
    {
        if (targetMachine == null || recipe == null || targetMachine.IsBroken)
        {
            return;
        }

        targetMachine.SelectRecipe(recipe);
        RefreshContents();
        CloseRecipeList();
    }

    private static string BuildRecipeLabel(Recipe recipe)
    {
        string recipeName = string.IsNullOrEmpty(recipe.id) ? "recipe" : recipe.id;
        string inputs = DescribeItemEntries(recipe.inputEntryList);
        string outputs = DescribeItemEntries(recipe.outputEntryList);
        return $"{recipeName}  ({inputs} → {outputs})";
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

            string itemName = string.IsNullOrEmpty(entry.item.displayName)
                ? entry.item.id
                : entry.item.displayName;
            parts.Add($"{itemName}x{entry.count}");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "-";
    }

    private void ClearRecipeButtons()
    {
        foreach (GameObject buttonObject in recipeButtons)
        {
            if (buttonObject != null)
            {
                Destroy(buttonObject);
            }
        }

        recipeButtons.Clear();
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("MachineInfoCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("InfoRoot");
        panelRoot.transform.SetParent(canvasObject.transform, false);
        var rootRect = panelRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(InfoWidth + ListGap + ListWidth, InfoHeight);

        CreateInfoPanel(panelRoot.transform);
        CreateRecipeListPanel(panelRoot.transform);
        RefreshPanelLayout();
    }

    private void CreateInfoPanel(Transform parent)
    {
        var glowObject = new GameObject("BrokenGlow");
        glowObject.transform.SetParent(parent, false);
        glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(InfoWidth + 16f * UiScale, InfoHeight + 16f * UiScale);
        glowImage = glowObject.AddComponent<Image>();
        glowImage.color = new Color(1f, 0.2f, 0.15f, 0.5f);
        glowObject.SetActive(false);

        var panelObject = new GameObject("InfoPanel");
        panelObject.transform.SetParent(parent, false);
        infoPanelRect = panelObject.AddComponent<RectTransform>();
        infoPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        infoPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        infoPanelRect.pivot = new Vector2(0.5f, 0.5f);
        infoPanelRect.anchoredPosition = Vector2.zero;
        infoPanelRect.sizeDelta = new Vector2(InfoWidth, InfoHeight);

        var panelImage = panelObject.AddComponent<Image>();
        panelImage.color = PanelFill;

        CreateCloseButton(panelObject.transform, Hide);

        var headerObject = new GameObject("Header");
        headerObject.transform.SetParent(panelObject.transform, false);
        var headerRect = headerObject.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -12f * UiScale);
        headerRect.sizeDelta = new Vector2(-24f * UiScale, 36f * UiScale);

        nameText = CreateText(
            headerObject.transform,
            "Name",
            Mathf.RoundToInt(20f * UiScale),
            TextAnchor.MiddleLeft,
            TextDark);
        var nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(0f, 0f);
        nameRect.offsetMax = new Vector2(-88f * UiScale, 0f);

        var badgeObject = new GameObject("StatusBadge");
        badgeObject.transform.SetParent(headerObject.transform, false);
        var badgeRect = badgeObject.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(1f, 0.5f);
        badgeRect.anchorMax = new Vector2(1f, 0.5f);
        badgeRect.pivot = new Vector2(1f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(-44f * UiScale, 0f);
        badgeRect.sizeDelta = new Vector2(64f * UiScale, 28f * UiScale);
        statusBadgeImage = badgeObject.AddComponent<Image>();
        statusBadgeImage.color = StatusIdle;
        statusBadgeText = CreateText(
            badgeObject.transform,
            "Status",
            Mathf.RoundToInt(14f * UiScale),
            TextAnchor.MiddleCenter,
            Color.white);
        StretchFull(statusBadgeText.rectTransform);

        recipeNameText = CreateLabeledBlock(
            panelObject.transform,
            "RecipeBlock",
            "레시피",
            new Vector2(0f, -56f * UiScale),
            out recipeIoText);

        CreateWipBlock(panelObject.transform);

        CreatePortsBlock(panelObject.transform);

        brokenAlertObject = CreateBrokenAlert(panelObject.transform);

        var buttonObject = new GameObject("ChangeRecipeButton");
        buttonObject.transform.SetParent(panelObject.transform, false);
        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 14f * UiScale);
        buttonRect.sizeDelta = new Vector2(-24f * UiScale, 40f * UiScale);

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = ButtonFill;
        changeRecipeButton = buttonObject.AddComponent<Button>();
        changeRecipeButton.onClick.AddListener(OnChangeRecipeClicked);
        changeRecipeLabel = CreateText(
            buttonObject.transform,
            "Label",
            Mathf.RoundToInt(16f * UiScale),
            TextAnchor.MiddleCenter,
            TextDark);
        StretchFull(changeRecipeLabel.rectTransform);
        changeRecipeLabel.text = "레시피 변경";
    }

    private Text CreateLabeledBlock(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchoredPos,
        out Text bodyText)
    {
        var block = new GameObject(objectName);
        block.transform.SetParent(parent, false);
        var blockRect = block.AddComponent<RectTransform>();
        blockRect.anchorMin = new Vector2(0f, 1f);
        blockRect.anchorMax = new Vector2(1f, 1f);
        blockRect.pivot = new Vector2(0.5f, 1f);
        blockRect.anchoredPosition = anchoredPos;
        blockRect.sizeDelta = new Vector2(-24f * UiScale, 56f * UiScale);

        var labelText = CreateText(
            block.transform,
            "Label",
            Mathf.RoundToInt(13f * UiScale),
            TextAnchor.UpperLeft,
            TextMuted);
        var labelRect = labelText.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 18f * UiScale);
        labelText.text = label;

        var title = CreateText(
            block.transform,
            "Title",
            Mathf.RoundToInt(16f * UiScale),
            TextAnchor.UpperLeft,
            TextDark);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f * UiScale);
        titleRect.sizeDelta = new Vector2(0f, 20f * UiScale);

        bodyText = CreateText(
            block.transform,
            "Body",
            Mathf.RoundToInt(14f * UiScale),
            TextAnchor.UpperLeft,
            TextMuted);
        var bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(0f, 0f);
        bodyRect.offsetMax = new Vector2(0f, -38f * UiScale);

        return title;
    }

    private void CreateWipBlock(Transform parent)
    {
        var block = new GameObject("WipBlock");
        block.transform.SetParent(parent, false);
        var blockRect = block.AddComponent<RectTransform>();
        blockRect.anchorMin = new Vector2(0f, 1f);
        blockRect.anchorMax = new Vector2(1f, 1f);
        blockRect.pivot = new Vector2(0.5f, 1f);
        blockRect.anchoredPosition = new Vector2(0f, -124f * UiScale);
        blockRect.sizeDelta = new Vector2(-24f * UiScale, 52f * UiScale);

        var label = CreateText(
            block.transform,
            "Label",
            Mathf.RoundToInt(13f * UiScale),
            TextAnchor.UpperLeft,
            TextMuted);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 18f * UiScale);
        label.text = "진행";

        var trackObject = new GameObject("Track");
        trackObject.transform.SetParent(block.transform, false);
        var trackRect = trackObject.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(0.72f, 0f);
        trackRect.pivot = new Vector2(0f, 0f);
        trackRect.anchoredPosition = new Vector2(0f, 8f * UiScale);
        trackRect.sizeDelta = new Vector2(0f, 16f * UiScale);
        var trackImage = trackObject.AddComponent<Image>();
        trackImage.color = BarTrack;

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(trackObject.transform, false);
        wipFillRect = fillObject.AddComponent<RectTransform>();
        wipFillImage = fillObject.AddComponent<Image>();
        wipFillImage.color = BarFill;
        SetWipFill(0f);

        wipText = CreateText(
            block.transform,
            "Value",
            Mathf.RoundToInt(14f * UiScale),
            TextAnchor.MiddleRight,
            TextDark);
        var valueRect = wipText.rectTransform;
        valueRect.anchorMin = new Vector2(0.72f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.pivot = new Vector2(1f, 0f);
        valueRect.anchoredPosition = new Vector2(0f, 4f * UiScale);
        valueRect.sizeDelta = new Vector2(0f, 24f * UiScale);
    }

    // 진행 바 아래: input(좌) · output(우) 포트 슬롯.
    private void CreatePortsBlock(Transform parent)
    {
        var block = new GameObject("PortsBlock");
        block.transform.SetParent(parent, false);
        var blockRect = block.AddComponent<RectTransform>();
        blockRect.anchorMin = new Vector2(0f, 1f);
        blockRect.anchorMax = new Vector2(1f, 1f);
        blockRect.pivot = new Vector2(0.5f, 1f);
        blockRect.anchoredPosition = new Vector2(0f, -184f * UiScale);
        blockRect.sizeDelta = new Vector2(-24f * UiScale, 64f * UiScale);

        var inputLabel = CreateText(
            block.transform,
            "InputLabel",
            Mathf.RoundToInt(12f * UiScale),
            TextAnchor.UpperLeft,
            TextMuted);
        var inputLabelRect = inputLabel.rectTransform;
        inputLabelRect.anchorMin = new Vector2(0f, 1f);
        inputLabelRect.anchorMax = new Vector2(0.5f, 1f);
        inputLabelRect.pivot = new Vector2(0f, 1f);
        inputLabelRect.anchoredPosition = Vector2.zero;
        inputLabelRect.sizeDelta = new Vector2(0f, 16f * UiScale);
        inputLabel.text = "입력";

        var outputLabel = CreateText(
            block.transform,
            "OutputLabel",
            Mathf.RoundToInt(12f * UiScale),
            TextAnchor.UpperRight,
            TextMuted);
        var outputLabelRect = outputLabel.rectTransform;
        outputLabelRect.anchorMin = new Vector2(0.5f, 1f);
        outputLabelRect.anchorMax = new Vector2(1f, 1f);
        outputLabelRect.pivot = new Vector2(1f, 1f);
        outputLabelRect.anchoredPosition = Vector2.zero;
        outputLabelRect.sizeDelta = new Vector2(0f, 16f * UiScale);
        outputLabel.text = "출력";

        inputSlotsRect = CreatePortSlotsRow(block.transform, isInput: true);
        outputSlotsRect = CreatePortSlotsRow(block.transform, isInput: false);
    }

    private RectTransform CreatePortSlotsRow(Transform parent, bool isInput)
    {
        var rowObject = new GameObject(isInput ? "InputSlots" : "OutputSlots");
        rowObject.transform.SetParent(parent, false);
        var rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = isInput ? new Vector2(0f, 0f) : new Vector2(0.5f, 0f);
        rowRect.anchorMax = isInput ? new Vector2(0.5f, 1f) : new Vector2(1f, 1f);
        rowRect.offsetMin = new Vector2(isInput ? 0f : 6f * UiScale, 0f);
        rowRect.offsetMax = new Vector2(isInput ? -6f * UiScale : 0f, -18f * UiScale);

        var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f * UiScale;
        layout.childAlignment = isInput ? TextAnchor.LowerLeft : TextAnchor.LowerRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return rowRect;
    }

    private GameObject CreateBrokenAlert(Transform parent)
    {
        var alertObject = new GameObject("BrokenAlert");
        alertObject.transform.SetParent(parent, false);
        var alertRect = alertObject.AddComponent<RectTransform>();
        alertRect.anchorMin = new Vector2(0f, 0f);
        alertRect.anchorMax = new Vector2(1f, 0f);
        alertRect.pivot = new Vector2(0.5f, 0f);
        alertRect.anchoredPosition = new Vector2(0f, 62f * UiScale);
        alertRect.sizeDelta = new Vector2(-24f * UiScale, 36f * UiScale);

        var alertImage = alertObject.AddComponent<Image>();
        alertImage.color = BrokenAlert;

        var alertText = CreateText(
            alertObject.transform,
            "Label",
            Mathf.RoundToInt(13f * UiScale),
            TextAnchor.MiddleCenter,
            Color.white);
        StretchFull(alertText.rectTransform);
        alertText.text = "근처에서 상호작용으로 수리";

        alertObject.SetActive(false);
        return alertObject;
    }

    private void CreateRecipeListPanel(Transform parent)
    {
        recipeListRoot = new GameObject("RecipeListPanel");
        recipeListRoot.transform.SetParent(parent, false);
        recipeListRect = recipeListRoot.AddComponent<RectTransform>();
        recipeListRect.anchorMin = new Vector2(0.5f, 0.5f);
        recipeListRect.anchorMax = new Vector2(0.5f, 0.5f);
        recipeListRect.pivot = new Vector2(0.5f, 0.5f);
        recipeListRect.anchoredPosition = Vector2.zero;
        recipeListRect.sizeDelta = new Vector2(ListWidth, InfoHeight);

        var listImage = recipeListRoot.AddComponent<Image>();
        listImage.color = PanelFill;

        var title = CreateText(
            recipeListRoot.transform,
            "Title",
            Mathf.RoundToInt(18f * UiScale),
            TextAnchor.MiddleLeft,
            TextDark);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(14f * UiScale, -44f * UiScale);
        titleRect.offsetMax = new Vector2(-48f * UiScale, 0f);
        title.text = "레시피 선택";

        CreateCloseButton(recipeListRoot.transform, CloseRecipeList);

        var scrollObject = new GameObject("RecipeScroll");
        scrollObject.transform.SetParent(recipeListRoot.transform, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(12f * UiScale, 12f * UiScale);
        scrollRectTransform.offsetMax = new Vector2(-12f * UiScale, -52f * UiScale);

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        StretchFull(viewportRect);
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;
        viewportObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        recipeListContent = contentObject.AddComponent<RectTransform>();
        recipeListContent.anchorMin = new Vector2(0f, 1f);
        recipeListContent.anchorMax = new Vector2(1f, 1f);
        recipeListContent.pivot = new Vector2(0.5f, 1f);
        recipeListContent.anchoredPosition = Vector2.zero;
        recipeListContent.sizeDelta = new Vector2(0f, 0f);

        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f * UiScale;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = recipeListContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        recipeListRoot.SetActive(false);
    }

    private void CreateCloseButton(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var closeObject = new GameObject("CloseButton");
        closeObject.transform.SetParent(parent, false);
        var closeRect = closeObject.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-8f * UiScale, -8f * UiScale);
        closeRect.sizeDelta = new Vector2(32f * UiScale, 32f * UiScale);

        var closeImage = closeObject.AddComponent<Image>();
        closeImage.color = ButtonFill;

        var closeButton = closeObject.AddComponent<Button>();
        closeButton.onClick.AddListener(onClick);

        var label = CreateText(
            closeObject.transform,
            "Label",
            Mathf.RoundToInt(20f * UiScale),
            TextAnchor.MiddleCenter,
            TextDark);
        StretchFull(label.rectTransform);
        label.text = "×";
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.AddComponent<RectTransform>();
        var text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    private enum MachineDisplayStatus
    {
        Idle,
        Working,
        Broken,
    }
}
