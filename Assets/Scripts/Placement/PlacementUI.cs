using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 배치 모드 하단 슬라이드바. 인벤 MachineInventoryEntry 목록을 표시하고 선택한다.
public class PlacementUI : MonoBehaviour
{
    [SerializeField] private float panelHeight = 168f;
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
    private readonly List<GameObject> machineButtonPool = new();
    private bool isVisible;
    private float targetAnchoredY;
    private float slideHeight;
    private string selectedDefinitionId;
    private RectTransform hoverTooltipRect;
    private Text hoverTooltipText;
    private const float HoverTooltipOffsetY = 8f;

    public void Initialize(PlacementController controller, PlayerInventory inventory)
    {
        if (placementController != null)
        {
            placementController.OnInventoryChanged -= Refresh;
        }

        placementController = controller;
        playerInventory = inventory;
        EnsureUiHierarchy();
        if (placementController != null)
        {
            placementController.OnInventoryChanged += Refresh;
        }
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

        if (!visible)
        {
            HideMachineHoverTooltip();
        }

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
        selectedDefinitionId = selectedMachine?.definition != null ? selectedMachine.definition.id : null;

        var groupedMachines = new List<(ItemDef_Machine definition, int count)>();
        var indexByDefinitionId = new Dictionary<string, int>();

        foreach (MachineInventoryEntry machine in playerInventory.Machines)
        {
            if (machine?.definition == null || string.IsNullOrEmpty(machine.definition.id))
            {
                continue;
            }

            string definitionId = machine.definition.id;
            if (indexByDefinitionId.TryGetValue(definitionId, out int index))
            {
                (ItemDef_Machine definition, int count) entry = groupedMachines[index];
                groupedMachines[index] = (entry.definition, entry.count + 1);
            }
            else
            {
                indexByDefinitionId[definitionId] = groupedMachines.Count;
                groupedMachines.Add((machine.definition, 1));
            }
        }

        foreach ((ItemDef_Machine definition, int count) group in groupedMachines)
        {
            CreateMachineButton(group.definition, group.count);
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
        canvasObject.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
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
        UiButtonStyle.Apply(pickupButton);

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
        UiPanelFrame.Apply(panelImage);

        var scrollObject = new GameObject("MachineScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);
        var scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(16f, 12f);
        scrollRect.offsetMax = new Vector2(-16f, -12f);

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
        contentRect.sizeDelta = new Vector2(0f, panelHeight - 8f);

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

        CreateHoverTooltip(canvasObject.transform);
    }

    private void CreateHoverTooltip(Transform parent)
    {
        var labelObject = new GameObject("MachineHoverTooltip");
        labelObject.transform.SetParent(parent, false);
        hoverTooltipRect = labelObject.AddComponent<RectTransform>();
        hoverTooltipRect.pivot = new Vector2(0.5f, 0f);

        Image background = labelObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        background.raycastTarget = false;

        var fitter = labelObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = labelObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 4, 4);
        layout.childAlignment = TextAnchor.MiddleCenter;

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(labelObject.transform, false);
        hoverTooltipText = textObject.AddComponent<Text>();
        hoverTooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hoverTooltipText.fontSize = 16;
        hoverTooltipText.alignment = TextAnchor.MiddleCenter;
        hoverTooltipText.color = Color.white;
        hoverTooltipText.raycastTarget = false;
        hoverTooltipText.horizontalOverflow = HorizontalWrapMode.Overflow;
        hoverTooltipText.verticalOverflow = VerticalWrapMode.Overflow;

        labelObject.SetActive(false);
    }

    internal void ShowMachineHoverTooltip(string text, RectTransform anchor)
    {
        if (hoverTooltipRect == null || hoverTooltipText == null || anchor == null)
        {
            return;
        }

        hoverTooltipText.text = text;
        hoverTooltipRect.gameObject.SetActive(true);

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        float centerX = (corners[0].x + corners[2].x) * 0.5f;
        float topY = corners[1].y;
        hoverTooltipRect.position = new Vector3(centerX, topY + HoverTooltipOffsetY, 0f);
    }

    internal void HideMachineHoverTooltip()
    {
        if (hoverTooltipRect != null)
        {
            hoverTooltipRect.gameObject.SetActive(false);
        }
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

    private void CreateMachineButton(ItemDef_Machine definition, int count)
    {
        if (definition == null)
        {
            return;
        }

        GameObject buttonObject = RentMachineButton($"Machine_{definition.id}");
        bool isSelected = definition.id == selectedDefinitionId;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);

        Transform highlight = buttonObject.transform.Find("Highlight");
        if (highlight != null)
        {
            highlight.gameObject.SetActive(isSelected);
        }

        Image iconImage = buttonObject.transform.Find("Icon").GetComponent<Image>();
        const float slotSize = 112f;
        MachineIconResolver.ConfigureInventoryImage(iconImage, definition, slotSize * 0.58f);
        bool hasIcon = iconImage.sprite != null;

        Text label = buttonObject.transform.Find("Count").GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 14;
        label.alignment = TextAnchor.LowerRight;
        label.color = Color.white;
        string displayName = !string.IsNullOrEmpty(definition.displayName)
            ? definition.displayName
            : definition.id;
        label.text = hasIcon ? $"x{count}" : $"{displayName}\nx{count}";
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.RemoveAllListeners();
        string definitionId = definition.id;
        button.onClick.AddListener(() => placementController.SelectMachineDefinition(definitionId));

        MachineButtonHoverHandler hoverHandler = buttonObject.GetComponent<MachineButtonHoverHandler>()
            ?? buttonObject.AddComponent<MachineButtonHoverHandler>();
        hoverHandler.Initialize(this, displayName);
    }

    private GameObject RentMachineButton(string buttonName)
    {
        GameObject buttonObject;
        if (machineButtonPool.Count > 0)
        {
            int last = machineButtonPool.Count - 1;
            buttonObject = machineButtonPool[last];
            machineButtonPool.RemoveAt(last);
            buttonObject.SetActive(true);
        }
        else
        {
            buttonObject = BuildMachineButtonShell();
        }

        buttonObject.name = buttonName;
        buttonObject.transform.SetParent(contentRect, false);
        machineButtons.Add(buttonObject);
        return buttonObject;
    }

    private GameObject BuildMachineButtonShell()
    {
        const float slotSize = 112f;

        var buttonObject = new GameObject("MachineButton");
        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(slotSize, slotSize);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);
        buttonObject.AddComponent<Button>();

        GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(buttonObject.transform, false);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(slotSize, slotSize);
        Image frameImage = frameObject.GetComponent<Image>();
        UiNoteBookSlot.ApplySlot(frameImage);
        frameImage.raycastTarget = false;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(buttonObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(slotSize * 0.58f, slotSize * 0.58f);
        var iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var countObject = new GameObject("Count");
        countObject.transform.SetParent(buttonObject.transform, false);
        var countRect = countObject.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0f, 0f);
        countRect.anchorMax = new Vector2(1f, 0.35f);
        countRect.offsetMin = new Vector2(4f, 2f);
        countRect.offsetMax = new Vector2(-4f, -2f);
        countObject.AddComponent<Text>();

        GameObject highlight = UiNoteBookSlot.CreateSelectHighlight(buttonObject.transform, slotSize);
        highlight.name = "Highlight";
        highlight.SetActive(false);

        return buttonObject;
    }

    private void ClearMachineButtons()
    {
        HideMachineHoverTooltip();

        foreach (GameObject buttonObject in machineButtons)
        {
            if (buttonObject == null)
            {
                continue;
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }

            buttonObject.SetActive(false);
            machineButtonPool.Add(buttonObject);
        }

        machineButtons.Clear();
    }

    private sealed class MachineButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private PlacementUI owner;
        private string label;

        public void Initialize(PlacementUI owner, string label)
        {
            this.owner = owner;
            this.label = label;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner == null || string.IsNullOrEmpty(label))
            {
                return;
            }

            owner.ShowMachineHoverTooltip(label, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HideMachineHoverTooltip();
        }
    }
}
