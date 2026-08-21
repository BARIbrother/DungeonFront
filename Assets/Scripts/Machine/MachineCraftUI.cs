using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 해금된 기계를 골드+재료로 즉시 구매한다. 1키로 연다.
public class MachineCraftUI : MonoBehaviour
{
    // 9슬라이스 배율을 올리면 둥근 모서리가 작아진다. 낮추면 더 둥글어진다.
    private const float CraftButtonPixelsPerUnit = 1.8f;

    private static MachineCraftUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform listRect;
    private TMP_Text titleText;
    private TMP_Text feedbackText;
    private readonly List<GameObject> rows = new();

    private MachineDatabase machineDatabase;
    private PlayerInventory playerInventory;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<MachineCraftUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineCraftUISystem");
        systemObject.AddComponent<MachineCraftUI>();
    }

    public static void Toggle()
    {
        PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
        Toggle(movement != null ? movement.MachineDatabase : null, PlayerInventory.GetOrFind());
    }

    public static void Toggle(MachineDatabase database, PlayerInventory inventory)
    {
        EnsureInstance();
        if (instance.isOpen)
        {
            instance.Hide();
            return;
        }

        inventory ??= PlayerInventory.GetOrFind();
        if (database == null || inventory == null)
        {
            Debug.LogWarning("[MachineCraftUI] MachineDatabase 또는 PlayerInventory가 null입니다.");
            return;
        }

        instance.Open(database, inventory);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<MachineCraftUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineCraftUISystem");
        instance = systemObject.AddComponent<MachineCraftUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureUiHierarchy();
        Hide();
    }

    private void Start()
    {
        HookOpenButton();
    }

    private void OnEnable()
    {
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.OnUnlocksChanged += HandleUnlocksChanged;
        }
    }

    private void OnDisable()
    {
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.OnUnlocksChanged -= HandleUnlocksChanged;
        }
    }

    private void OnDestroy()
    {
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.OnUnlocksChanged -= HandleUnlocksChanged;
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void HandleUnlocksChanged()
    {
        if (isOpen)
        {
            RebuildRows();
        }
    }

    private void HookOpenButton()
    {
        if (GameObject.Find("MachineCraftOpenButton") != null)
        {
            return;
        }

        GameObject techOpen = GameObject.Find("TechTreeOpenButton");
        if (techOpen == null)
        {
            return;
        }

        GameObject craftOpen = Instantiate(techOpen, techOpen.transform.parent, false);
        craftOpen.name = "MachineCraftOpenButton";

        RectTransform techRect = techOpen.GetComponent<RectTransform>();
        RectTransform craftRect = craftOpen.GetComponent<RectTransform>();
        if (techRect != null && craftRect != null)
        {
            craftRect.anchorMin = techRect.anchorMin;
            craftRect.anchorMax = techRect.anchorMax;
            craftRect.pivot = techRect.pivot;
            craftRect.sizeDelta = new Vector2(168f, 56f);
            craftRect.anchoredPosition = techRect.anchoredPosition;
        }

        TMP_Text tmp = craftOpen.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = "기계제작";
            TmpUiStyle.Apply(tmp, TmpUiStyle.Role.Button);
            tmp.fontSize = 22f;
        }
        else
        {
            TextMeshProUGUI label = TmpUiStyle.Create(
                craftOpen,
                TmpUiStyle.Role.Button,
                TextAlignmentOptions.Center);
            label.text = "기계제작";
            label.fontSize = 22f;
        }

        Button button = craftOpen.GetComponent<Button>();
        if (button == null)
        {
            button = craftOpen.AddComponent<Button>();
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnHudClicked);
        UiButtonStyle.Apply(button);
        TechTreeUI.NudgeHudOpenButtons();
    }

    private void OnHudClicked()
    {
        Toggle();
    }

    private void Open(MachineDatabase database, PlayerInventory inventory)
    {
        machineDatabase = database;
        playerInventory = inventory;
        machineDatabase.RebuildLookup();
        titleText.text = "기계 제작";
        feedbackText.text = "테크 트리에서 해금한 기계를, 준비 단계에서 재료와 골드로 만듭니다.";
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.OnUnlocksChanged -= HandleUnlocksChanged;
            UnlockManager.Instance.OnUnlocksChanged += HandleUnlocksChanged;
        }

        RebuildRows();
        modalRoot.SetActive(true);
        isOpen = true;
    }

    public void Hide()
    {
        isOpen = false;
        machineDatabase = null;
        playerInventory = null;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void RebuildRows()
    {
        ClearRows();
        if (listRect == null || machineDatabase == null || playerInventory == null)
        {
            return;
        }

        int gold = GetGold();
        int visible = 0;
        for (int i = 0; i < MachineCraftCatalog.All.Length; i++)
        {
            MachineCraftCatalog.Recipe recipe = MachineCraftCatalog.All[i];
            if (!MachineCraftCatalog.IsObtainable(recipe.machineDefId))
            {
                continue;
            }

            ItemDef_Machine definition = machineDatabase.Get(recipe.machineDefId);
            if (definition == null || definition.machinePrefab == null)
            {
                continue;
            }

            CreateRow(recipe, definition, gold);
            visible++;
        }

        if (visible == 0)
        {
            CreateInfoLabel("구매할 수 있는 기계가 없습니다.");
        }
    }

    private void CreateRow(MachineCraftCatalog.Recipe recipe, ItemDef_Machine definition, int gold)
    {
        bool unlocked = MachineCraftService.IsTechUnlocked(recipe);
        bool affordable = unlocked && MachineCraftService.CanAfford(recipe, playerInventory, gold);
        string name = string.IsNullOrEmpty(definition.displayName) ? definition.id : definition.displayName;

        var buttonObject = new GameObject($"Craft_{recipe.machineDefId}");
        buttonObject.transform.SetParent(listRect, false);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 64f;
        layoutElement.preferredHeight = 64f;

        var buttonImage = buttonObject.AddComponent<Image>();
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.interactable = unlocked && affordable;
        if (unlocked)
        {
            string id = recipe.machineDefId;
            ItemDef_Machine captured = definition;
            button.onClick.AddListener(() => OnClicked(id, captured));
        }

        UiButtonStyle.Apply(button, CraftButtonPixelsPerUnit);
        ApplyDarkNormalSprite(buttonImage);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);

        var label = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Button, TextAlignmentOptions.MidlineLeft);
        label.fontSize = 16f;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        string cost = MachineCraftService.FormatCost(recipe);
        if (!unlocked)
        {
            string techName = TechTreeCatalog.DisplayName(recipe.requiredTechId);
            label.text = $"{name}\n{techName} 해금 필요 · {cost}";
        }
        else
        {
            label.text = $"{name}\n{cost}";
        }

        rows.Add(buttonObject);
    }

    private static void ApplyDarkNormalSprite(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (image.sprite == null)
        {
            Sprite sprite = Resources.Load<Sprite>("UI/LightFantasy_button_dark_normal");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = CraftButtonPixelsPerUnit;
                image.fillCenter = true;
            }
        }

        image.color = Color.white;
    }

    private void OnClicked(string machineDefId, ItemDef_Machine definition)
    {
        if (!isOpen)
        {
            return;
        }

        if (MachineCraftService.TryCraft(machineDefId, out string error, definition))
        {
            string name = string.IsNullOrEmpty(definition.displayName) ? machineDefId : definition.displayName;
            feedbackText.text = $"{name} 제작 완료";
            RebuildRows();
            return;
        }

        feedbackText.text = error ?? "제작에 실패했습니다.";
        RebuildRows();
    }

    private void CreateInfoLabel(string message)
    {
        var labelObject = new GameObject("InfoLabel");
        labelObject.transform.SetParent(listRect, false);
        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 48f;
        var label = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.Center, true);
        label.fontSize = 16f;
        label.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        label.text = message;
        rows.Add(labelObject);
    }

    private void ClearRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null)
            {
                Destroy(rows[i]);
            }
        }

        rows.Clear();
    }

    private static int GetGold()
    {
        Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
        if (economy != null)
        {
            return economy.Gold;
        }

        return GameSessionState.Instance != null ? GameSessionState.Instance.gold : 0;
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();
        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("MachineCraftCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("CraftModal");
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

        var panelObject = new GameObject("CraftPanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 620f);
        var panelImage = panelObject.AddComponent<Image>();
        UiPanelFrame.Apply(panelImage);

        CreateTitleBanner(panelObject.transform);
        CreateCloseButton(panelObject.transform);

        var feedbackObject = new GameObject("Feedback");
        feedbackObject.transform.SetParent(panelObject.transform, false);
        var feedbackRect = feedbackObject.AddComponent<RectTransform>();
        feedbackRect.anchorMin = new Vector2(0f, 0f);
        feedbackRect.anchorMax = new Vector2(1f, 0f);
        feedbackRect.pivot = new Vector2(0.5f, 0f);
        feedbackRect.sizeDelta = new Vector2(0f, 40f);
        feedbackRect.anchoredPosition = Vector2.zero;
        feedbackText = TmpUiStyle.Create(feedbackObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.MidlineLeft);
        feedbackText.fontSize = 14f;
        feedbackText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
        var feedbackTextRect = feedbackText.rectTransform;
        feedbackTextRect.anchorMin = Vector2.zero;
        feedbackTextRect.anchorMax = Vector2.one;
        feedbackTextRect.offsetMin = new Vector2(36f, 4f);
        feedbackTextRect.offsetMax = new Vector2(-36f, -4f);

        var scrollObject = new GameObject("CraftScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(36f, 48f);
        scrollRectTransform.offsetMax = new Vector2(-36f, -44f);

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
        listRect = contentObject.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = Vector2.zero;
        listRect.sizeDelta = new Vector2(0f, 0f);

        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
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
        scroll.content = listRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private void CreateTitleBanner(Transform parent)
    {
        var bannerObject = new GameObject("TitleBanner");
        bannerObject.transform.SetParent(parent, false);
        var bannerRect = bannerObject.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0f, 1f);
        bannerRect.anchorMax = new Vector2(0f, 1f);
        bannerRect.pivot = new Vector2(0f, 0f);
        bannerRect.anchoredPosition = new Vector2(-8f, -10f);
        bannerRect.sizeDelta = new Vector2(220f, 52f);
        var bannerImage = bannerObject.AddComponent<Image>();
        bannerImage.raycastTarget = false;
        UiPanelFrame.Apply(bannerImage, UiPanelFrame.Kind.BannerCream, 0.9f);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(bannerObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(22f, 4f);
        labelRect.offsetMax = new Vector2(-22f, -4f);
        titleText = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Title, TextAlignmentOptions.Center, true);
        titleText.fontSize = 20f;
        titleText.color = new Color(0.28f, 0.22f, 0.16f, 0.95f);
        titleText.text = "기계 제작";
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
        UiButtonStyle.Apply(closeButton);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(closeObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Button, TextAlignmentOptions.Center);
        label.fontSize = 22f;
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
