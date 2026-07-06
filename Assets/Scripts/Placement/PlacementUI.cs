using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 배치 모드 하단 슬라이드바. 인벤 MachineInventoryEntry 목록을 표시하고 선택한다.
public class PlacementUI : MonoBehaviour
{
    [SerializeField] private float panelHeight = 120f;
    [SerializeField] private float pickupBarHeight = 40f;
    [SerializeField] private float slideSpeed = 900f;

    private PlacementController placementController;
    private PlayerInventory playerInventory;
    private Canvas canvas;
    private RectTransform slideRootRect;
    private RectTransform panelRect;
    private RectTransform contentRect;
    private Image pickupButtonImage;
    private readonly List<GameObject> machineButtons = new();
    private bool isVisible;
    private float targetAnchoredY;
    private float slideHeight;
    private string selectedInstanceId;

    public void Initialize(PlacementController controller, PlayerInventory inventory)
    {
        placementController = controller;
        playerInventory = inventory;
        EnsureUiHierarchy();
        placementController.OnInventoryChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (placementController != null)
        {
            placementController.OnInventoryChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (slideRootRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = slideRootRect.anchoredPosition;
        anchoredPosition.y = Mathf.MoveTowards(anchoredPosition.y, targetAnchoredY, slideSpeed * Time.deltaTime);
        slideRootRect.anchoredPosition = anchoredPosition;
    }

    public void SetVisible(bool visible, bool instant = false)
    {
        isVisible = visible;
        targetAnchoredY = visible ? 0f : -slideHeight;

        if (instant && slideRootRect != null)
        {
            slideRootRect.anchoredPosition = new Vector2(0f, targetAnchoredY);
        }
    }

    public void Refresh()
    {
        if (contentRect == null || playerInventory == null)
        {
            return;
        }

        ClearMachineButtons();

        MachineInventoryEntry selectedMachine = placementController != null ? placementController.SelectedMachine : null;
        selectedInstanceId = selectedMachine != null ? selectedMachine.instanceId : null;

        foreach (MachineInventoryEntry machine in playerInventory.Machines)
        {
            CreateMachineButton(machine);
        }

        RefreshPickupButton();
    }

    private void RefreshPickupButton()
    {
        if (pickupButtonImage == null || placementController == null)
        {
            return;
        }

        bool isPickupMode = placementController.IsPickupMode;
        pickupButtonImage.color = isPickupMode
            ? new Color(0.75f, 0.35f, 0.25f, 1f)
            : new Color(0.2f, 0.22f, 0.28f, 1f);
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("PlacementCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        slideHeight = panelHeight + pickupBarHeight;

        var slideRootObject = new GameObject("PlacementSlideRoot");
        slideRootObject.transform.SetParent(canvasObject.transform, false);
        slideRootRect = slideRootObject.AddComponent<RectTransform>();
        slideRootRect.anchorMin = new Vector2(0f, 0f);
        slideRootRect.anchorMax = new Vector2(1f, 0f);
        slideRootRect.pivot = new Vector2(0.5f, 0f);
        slideRootRect.sizeDelta = new Vector2(0f, slideHeight);
        slideRootRect.anchoredPosition = new Vector2(0f, -slideHeight);

        var pickupBarObject = new GameObject("PickupBar");
        pickupBarObject.transform.SetParent(slideRootObject.transform, false);
        var pickupBarRect = pickupBarObject.AddComponent<RectTransform>();
        pickupBarRect.anchorMin = new Vector2(0f, 0f);
        pickupBarRect.anchorMax = new Vector2(1f, 0f);
        pickupBarRect.pivot = new Vector2(0.5f, 0f);
        pickupBarRect.sizeDelta = new Vector2(0f, pickupBarHeight);
        pickupBarRect.anchoredPosition = new Vector2(0f, panelHeight);

        var pickupButtonObject = new GameObject("PickupButton");
        pickupButtonObject.transform.SetParent(pickupBarObject.transform, false);
        var pickupButtonRect = pickupButtonObject.AddComponent<RectTransform>();
        pickupButtonRect.anchorMin = new Vector2(0f, 0.5f);
        pickupButtonRect.anchorMax = new Vector2(0f, 0.5f);
        pickupButtonRect.pivot = new Vector2(0f, 0.5f);
        pickupButtonRect.anchoredPosition = new Vector2(12f, 0f);
        pickupButtonRect.sizeDelta = new Vector2(96f, pickupBarHeight - 8f);

        pickupButtonImage = pickupButtonObject.AddComponent<Image>();
        pickupButtonImage.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        var pickupButton = pickupButtonObject.AddComponent<Button>();
        pickupButton.onClick.AddListener(() => placementController.TogglePickupMode());

        var pickupLabelObject = new GameObject("Label");
        pickupLabelObject.transform.SetParent(pickupButtonObject.transform, false);
        var pickupLabelRect = pickupLabelObject.AddComponent<RectTransform>();
        pickupLabelRect.anchorMin = Vector2.zero;
        pickupLabelRect.anchorMax = Vector2.one;
        pickupLabelRect.offsetMin = Vector2.zero;
        pickupLabelRect.offsetMax = Vector2.zero;

        var pickupLabel = pickupLabelObject.AddComponent<Text>();
        pickupLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pickupLabel.fontSize = 16;
        pickupLabel.alignment = TextAnchor.MiddleCenter;
        pickupLabel.color = Color.white;
        pickupLabel.text = "회수";

        var panelObject = new GameObject("PlacementPanel");
        panelObject.transform.SetParent(slideRootObject.transform, false);
        panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(0f, panelHeight);
        panelRect.anchoredPosition = Vector2.zero;

        var panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.92f);

        var scrollObject = new GameObject("MachineScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);
        var scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(12f, 12f);
        scrollRect.offsetMax = new Vector2(-12f, -12f);

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
        contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.sizeDelta = new Vector2(0f, panelHeight - 24f);

        var layout = contentObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
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

    private void CreateMachineButton(MachineInventoryEntry machine)
    {
        if (machine?.definition == null)
        {
            return;
        }

        var buttonObject = new GameObject($"Machine_{machine.instanceId}");
        buttonObject.transform.SetParent(contentRect, false);

        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(96f, 96f);

        var buttonImage = buttonObject.AddComponent<Image>();
        bool isSelected = machine.instanceId == selectedInstanceId;
        buttonImage.color = isSelected ? new Color(0.35f, 0.55f, 0.85f, 1f) : new Color(0.2f, 0.22f, 0.28f, 1f);

        var button = buttonObject.AddComponent<Button>();
        string instanceId = machine.instanceId;
        button.onClick.AddListener(() => placementController.SelectMachine(instanceId));

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -4f);

        var label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = machine.definition.displayName;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        machineButtons.Add(buttonObject);
    }

    private void ClearMachineButtons()
    {
        foreach (GameObject buttonObject in machineButtons)
        {
            if (buttonObject != null)
            {
                Destroy(buttonObject);
            }
        }

        machineButtons.Clear();
    }
}
