using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// MachineDatabase 목록을 버튼으로 보여주고, 클릭 시 인벤에 기계를 지급한다.
public class MachineGrantUI : MonoBehaviour
{
    private static MachineGrantUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform machineListRect;
    private Text titleText;
    private readonly List<GameObject> machineButtons = new();
    private Font uiFont;

    private MachineDatabase machineDatabase;
    private PlayerInventory playerInventory;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<MachineGrantUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineGrantUISystem");
        systemObject.AddComponent<MachineGrantUI>();
    }

    // MachineDatabase의 기계 목록 UI를 연다.
    public static void Show(MachineDatabase database, PlayerInventory inventory)
    {
        if (database == null || inventory == null)
        {
            Debug.LogWarning("[MachineGrantUI] MachineDatabase 또는 PlayerInventory가 null입니다.");
            return;
        }

        EnsureInstance();
        instance.Open(database, inventory);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<MachineGrantUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineGrantUISystem");
        instance = systemObject.AddComponent<MachineGrantUI>();
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

    private void Open(MachineDatabase database, PlayerInventory inventory)
    {
        machineDatabase = database;
        playerInventory = inventory;
        machineDatabase.RebuildLookup();
        titleText.text = "기계 지급";
        RebuildMachineButtons();
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

    private void RebuildMachineButtons()
    {
        ClearMachineButtons();

        if (machineListRect == null || machineDatabase == null)
        {
            return;
        }

        IReadOnlyList<ItemDef_Machine> machines = machineDatabase.All;
        if (machines == null || machines.Count == 0)
        {
            CreateInfoLabel("MachineDatabase에 등록된 기계가 없습니다.");
            return;
        }

        int buttonCount = 0;
        for (int i = 0; i < machines.Count; i++)
        {
            ItemDef_Machine def = machines[i];
            if (def == null || string.IsNullOrEmpty(def.id))
            {
                continue;
            }

            CreateMachineButton(def);
            buttonCount++;
        }

        if (buttonCount == 0)
        {
            CreateInfoLabel("지급 가능한 기계가 없습니다.");
        }
    }

    private void CreateInfoLabel(string message)
    {
        var labelObject = new GameObject("InfoLabel");
        labelObject.transform.SetParent(machineListRect, false);

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 48f;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        label.text = message;

        machineButtons.Add(labelObject);
    }

    private void CreateMachineButton(ItemDef_Machine definition)
    {
        var buttonObject = new GameObject($"Machine_{definition.id}");
        buttonObject.transform.SetParent(machineListRect, false);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 44f;

        bool canGrant = definition.machinePrefab != null;
        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = canGrant
            ? new Color(0.2f, 0.22f, 0.28f, 1f)
            : new Color(0.25f, 0.18f, 0.18f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.interactable = canGrant;
        if (canGrant)
        {
            ItemDef_Machine captured = definition;
            button.onClick.AddListener(() => OnMachineButtonClicked(captured));
        }

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);

        string displayName = string.IsNullOrEmpty(definition.displayName)
            ? definition.id
            : definition.displayName;
        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = canGrant
            ? $"{displayName}  ({definition.id})"
            : $"{displayName}  ({definition.id}) — prefab 없음";

        machineButtons.Add(buttonObject);
    }

    private void OnMachineButtonClicked(ItemDef_Machine definition)
    {
        if (!isOpen || playerInventory == null || definition == null || definition.machinePrefab == null)
        {
            return;
        }

        int countBefore = playerInventory.Machines.Count;
        playerInventory.AddMachine(definition);
        if (playerInventory.Machines.Count <= countBefore)
        {
            Debug.LogWarning($"[MachineGrantUI] 기계 지급 실패: {definition.id}");
            return;
        }

        MachineInventoryEntry added = playerInventory.Machines[playerInventory.Machines.Count - 1];
        Debug.Log(
            $"[MachineGrantUI] 기계 지급: {definition.displayName} ({definition.id}), " +
            $"instanceId={added.instanceId}, 인벤 총 {playerInventory.Machines.Count}대");
    }

    private void ClearMachineButtons()
    {
        for (int i = 0; i < machineButtons.Count; i++)
        {
            if (machineButtons[i] != null)
            {
                Destroy(machineButtons[i]);
            }
        }

        machineButtons.Clear();
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("MachineGrantCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("GrantModal");
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

        // 배경 클릭으로는 닫지 않는다. 닫기 버튼만 사용.
        backdropImage.raycastTarget = true;

        var panelObject = new GameObject("GrantPanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
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
        titleText.text = "기계 지급";

        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = new Vector2(-56f, 0f);

        CreateCloseButton(panelObject.transform);

        var scrollObject = new GameObject("MachineScroll");
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
        machineListRect = contentObject.AddComponent<RectTransform>();
        machineListRect.anchorMin = new Vector2(0f, 1f);
        machineListRect.anchorMax = new Vector2(1f, 1f);
        machineListRect.pivot = new Vector2(0.5f, 1f);
        machineListRect.anchoredPosition = Vector2.zero;
        machineListRect.sizeDelta = new Vector2(0f, 0f);

        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
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
        scroll.content = machineListRect;
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
