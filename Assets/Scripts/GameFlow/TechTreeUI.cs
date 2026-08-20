using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class TechTreeUI : MonoBehaviour
{
    private const float ScaleX = 0.48f;
    private const float ScaleY = 0.58f;
    private const float SlotSize = 56f;
    private const float NodeWidth = 108f;
    private const float NodeHeight = 88f;
    private const float GraphPad = 28f;
    private const float LineThickness = 5f;
    private const float ArrowWidth = 16f;
    private const float ArrowHeight = 12f;

    private static readonly Color LineLocked = new Color(0.5f, 0.38f, 0.22f, 0.72f);
    private static readonly Color LineReady = new Color(0.58f, 0.4f, 0.12f, 1f);
    private static readonly Color LineUnlocked = new Color(0.28f, 0.18f, 0.08f, 1f);
    private static readonly Color ColumnGuide = new Color(0.42f, 0.32f, 0.18f, 0.32f);
    private static readonly Color Ink = new Color(0.22f, 0.16f, 0.1f, 1f);
    private static readonly Color InkMuted = new Color(0.48f, 0.4f, 0.3f, 1f);
    private static readonly Color InkGold = new Color(0.42f, 0.3f, 0.12f, 1f);

    [SerializeField] private GameObject techTreePanel;
    [SerializeField] private GameObject confirmPopupPanel;
    [SerializeField] private TMP_Text popupTitleText;
    [SerializeField] private TMP_Text popupDescText;
    [SerializeField] private TMP_Text popupCostText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private static TechTreeUI instance;

    private Canvas overlayCanvas;
    private GameObject modalRoot;
    private RectTransform graphRect;
    private TMP_Text honorText;
    private TMP_Text detailTitle;
    private TMP_Text detailBody;
    private TMP_Text detailCost;
    private Button unlockButton;
    private TMP_Text unlockLabel;
    private TechTreeCatalog.Node selectedNode;
    private TechNodeSO selectedLegacyNode;
    private bool isOpen;
    private bool graphBuilt;
    private readonly List<NodeView> nodeViews = new();
    private readonly List<ConnectionView> connectionViews = new();
    private readonly List<GameObject> columnGuides = new();
    private static Sprite arrowHeadSprite;
    private static Sprite dashSprite;

    public static bool IsOpen => instance != null && instance.isOpen;

    private sealed class NodeView
    {
        public TechTreeCatalog.Node node;
        public GameObject root;
        public Image slot;
        public Image icon;
        public GameObject highlight;
        public TMP_Text nameLabel;
        public Button button;
    }

    private sealed class ConnectionView
    {
        public string fromId;
        public string toId;
        public Image graphic;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<TechTreeUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("TechTreeUISystem");
        systemObject.AddComponent<TechTreeUI>();
    }

    public static void Toggle()
    {
        EnsureInstance();
        instance.ToggleTechTreePanel();
    }

    public static void Close()
    {
        if (instance != null && instance.isOpen)
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

        instance = FindAnyObjectByType<TechTreeUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("TechTreeUISystem");
        instance = systemObject.AddComponent<TechTreeUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsureUiHierarchy();
        Hide();
    }

    private void Start()
    {
        if (techTreePanel != null)
        {
            techTreePanel.SetActive(false);
        }

        if (confirmPopupPanel != null)
        {
            confirmPopupPanel.SetActive(false);
        }

        HookOpenButton();
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.OnUnlocksChanged += RefreshOpenState;
        }
    }

    private void OnDestroy()
    {
        if (UnlockManager.Instance != null)
        {
            UnlockManager.Instance.OnUnlocksChanged -= RefreshOpenState;
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    public void Bind(
        GameObject treePanel,
        GameObject confirmPopup,
        TMP_Text title,
        TMP_Text desc,
        TMP_Text cost,
        Button confirm,
        Button cancel)
    {
        techTreePanel = treePanel;
        confirmPopupPanel = confirmPopup;
        popupTitleText = title;
        popupDescText = desc;
        popupCostText = cost;
        confirmButton = confirm;
        cancelButton = cancel;
        if (techTreePanel != null)
        {
            techTreePanel.SetActive(false);
        }

        if (confirmPopupPanel != null)
        {
            confirmPopupPanel.SetActive(false);
        }
    }

    public void ToggleTechTreePanel()
    {
        if (isOpen)
        {
            Hide();
            return;
        }

        Open();
    }

    public void OnClickTechNode(TechNodeSO node)
    {
        if (node == null)
        {
            return;
        }

        selectedLegacyNode = node;
        selectedNode = TechTreeCatalog.Get(node.techId);
        if (!isOpen)
        {
            Open();
        }

        RefreshDetail();
        RefreshNodeVisuals();
    }

    public void OnConfirmUnlock()
    {
        TryUnlockSelected();
        if (confirmPopupPanel != null)
        {
            confirmPopupPanel.SetActive(false);
        }
    }

    public void OnCancelPopup()
    {
        if (confirmPopupPanel != null)
        {
            confirmPopupPanel.SetActive(false);
        }

        selectedLegacyNode = null;
    }

    private void Open()
    {
        EnsureUiHierarchy();
        if (!graphBuilt)
        {
            BuildGraph();
        }

        RefreshHonor();
        RefreshNodeVisuals();
        RefreshDetail();
        if (techTreePanel != null)
        {
            techTreePanel.SetActive(false);
        }

        modalRoot.SetActive(true);
        isOpen = true;
    }

    private void Hide()
    {
        isOpen = false;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }

        if (techTreePanel != null)
        {
            techTreePanel.SetActive(false);
        }

        if (confirmPopupPanel != null)
        {
            confirmPopupPanel.SetActive(false);
        }
    }

    private void RefreshOpenState()
    {
        if (!isOpen)
        {
            return;
        }

        RefreshHonor();
        RefreshNodeVisuals();
        RefreshDetail();
    }

    private void HookOpenButton()
    {
        GameObject open = GameObject.Find("TechTreeOpenButton");
        if (open == null)
        {
            return;
        }

        Button button = open.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(ToggleTechTreePanel);
        }

        UiButtonStyle.Apply(button);
        TMP_Text tmp = open.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = "테크";
            TmpUiStyle.Apply(tmp, TmpUiStyle.Role.Button);
            tmp.fontSize = 22f;
        }

        RectTransform rect = open.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(168f, 56f);
        }

        NudgeHudOpenButtons();
    }

    public static void NudgeHudOpenButtons()
    {
        AlignHudOpenButtons();
    }

    public static void AlignHudOpenButtons()
    {
        RectTransform quest = FindHudButtonRect("QuestOpenButton");
        RectTransform tech = FindHudButtonRect("TechTreeOpenButton");
        RectTransform craft = FindHudButtonRect("MachineCraftOpenButton");
        if (quest == null && tech == null && craft == null)
        {
            return;
        }

        const float buttonWidth = 168f;
        const float buttonHeight = 56f;
        const float gap = 12f;
        const float firstCenterX = 108f;
        const float rowY = 0f;

        PlaceHudButton(quest, firstCenterX, rowY, buttonWidth, buttonHeight);
        PlaceHudButton(tech, firstCenterX + buttonWidth + gap, rowY, buttonWidth, buttonHeight);
        PlaceHudButton(
            craft,
            firstCenterX + (buttonWidth + gap) * 2f,
            rowY,
            buttonWidth,
            buttonHeight);
    }

    private static RectTransform FindHudButtonRect(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    private static void PlaceHudButton(
        RectTransform rect,
        float centerX,
        float y,
        float width,
        float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(centerX, y);
        rect.localScale = Vector3.one;
    }

    private void BuildGraph()
    {
        ClearGraph();
        if (graphRect == null)
        {
            return;
        }

        float maxX = 0f;
        float maxY = 0f;
        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node node = TechTreeCatalog.All[i];
            if (!node.visibleInGame)
            {
                continue;
            }

            maxX = Mathf.Max(maxX, node.x);
            maxY = Mathf.Max(maxY, node.y);
        }

        graphRect.sizeDelta = new Vector2(
            maxX * ScaleX + NodeWidth + GraphPad * 2f,
            maxY * ScaleY + NodeHeight + GraphPad * 2f);

        CreateColumnGuides();

        // 보이는 선행만 선으로 그린다. 숨은 선행은 가장 가까운 보이는 조상으로 잇는다.
        var drawn = new HashSet<string>();
        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node to = TechTreeCatalog.All[i];
            if (!to.visibleInGame)
            {
                continue;
            }

            TechTreeCatalog.ForEachIncomingParent(to.id, fromId =>
            {
                DrawVisibleConnections(fromId, to, drawn);
            });
        }

        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node node = TechTreeCatalog.All[i];
            if (node.visibleInGame)
            {
                CreateNode(node);
            }
        }

        graphBuilt = true;
    }

    private void DrawVisibleConnections(
        string fromId,
        TechTreeCatalog.Node to,
        HashSet<string> drawn)
    {
        TechTreeCatalog.Node from = TechTreeCatalog.Get(fromId);
        if (from == null || from == to)
        {
            return;
        }

        if (from.visibleInGame)
        {
            string key = from.id + ">" + to.id;
            if (drawn.Add(key))
            {
                CreateConnection(from, to);
            }

            return;
        }

        TechTreeCatalog.ForEachIncomingParent(from.id, parentId =>
        {
            DrawVisibleConnections(parentId, to, drawn);
        });
    }

    private void CreateColumnGuides()
    {
        var columns = new List<float>();
        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node node = TechTreeCatalog.All[i];
            if (!node.visibleInGame)
            {
                continue;
            }

            bool exists = false;
            for (int c = 0; c < columns.Count; c++)
            {
                if (Mathf.Abs(columns[c] - node.x) < 1f)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                columns.Add(node.x);
            }
        }

        columns.Sort();
        float height = graphRect.sizeDelta.y - 8f;
        for (int i = 0; i < columns.Count - 1; i++)
        {
            float left = GraphPad + columns[i] * ScaleX + NodeWidth * 0.5f;
            float right = GraphPad + columns[i + 1] * ScaleX + NodeWidth * 0.5f;
            float mid = (left + right) * 0.5f;

            var guideObject = new GameObject($"ColumnGuide_{i}");
            guideObject.transform.SetParent(graphRect, false);
            var rect = guideObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(3f, height);
            rect.anchoredPosition = new Vector2(mid, -4f);

            var image = guideObject.AddComponent<Image>();
            image.sprite = GetDashSprite();
            image.type = Image.Type.Tiled;
            image.color = Color.white;
            image.raycastTarget = false;
            columnGuides.Add(guideObject);
        }
    }

    private void CreateConnection(TechTreeCatalog.Node from, TechTreeCatalog.Node to)
    {
        Vector2 fromCenter = NodeCenter(from);
        Vector2 toCenter = NodeCenter(to);
        float inset = SlotSize * 0.5f + 3f;
        bool destRight = toCenter.x >= fromCenter.x;
        bool destBelow = toCenter.y < fromCenter.y;

        Vector2 start;
        Vector2 end;
        if (Mathf.Abs(toCenter.x - fromCenter.x) < 8f)
        {
            start = new Vector2(fromCenter.x, fromCenter.y + (destBelow ? -inset : inset));
            end = new Vector2(toCenter.x, toCenter.y + (destBelow ? inset : -inset));
            CreateSegment(from.id, to.id, start, end);
            CreateArrow(from.id, to.id, end, end - start);
            return;
        }

        start = new Vector2(fromCenter.x + (destRight ? inset : -inset), fromCenter.y);
        end = new Vector2(toCenter.x + (destRight ? -inset : inset), toCenter.y);
        if (Mathf.Abs(toCenter.y - fromCenter.y) < 8f)
        {
            CreateSegment(from.id, to.id, start, end);
            CreateArrow(from.id, to.id, end, end - start);
            return;
        }

        float midX = (start.x + end.x) * 0.5f;
        Vector2 elbowTop = new Vector2(midX, start.y);
        Vector2 elbowBottom = new Vector2(midX, end.y);
        CreateSegment(from.id, to.id, start, elbowTop);
        CreateSegment(from.id, to.id, elbowTop, elbowBottom);
        CreateSegment(from.id, to.id, elbowBottom, end);
        CreateArrow(from.id, to.id, end, end - elbowBottom);
    }

    private void CreateSegment(string fromId, string toId, Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length < 1f)
        {
            return;
        }

        var lineObject = new GameObject($"Line_{fromId}_{toId}");
        lineObject.transform.SetParent(graphRect, false);
        var rect = lineObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(length, LineThickness);
        rect.anchoredPosition = start;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        var image = lineObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = LineLocked;
        connectionViews.Add(new ConnectionView
        {
            fromId = fromId,
            toId = toId,
            graphic = image,
        });
    }

    private void CreateArrow(string fromId, string toId, Vector2 tip, Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        var arrowObject = new GameObject($"Arrow_{fromId}_{toId}");
        arrowObject.transform.SetParent(graphRect, false);
        var rect = arrowObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(ArrowWidth, ArrowHeight);
        rect.anchoredPosition = tip;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        var image = arrowObject.AddComponent<Image>();
        image.sprite = GetArrowSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = LineLocked;
        connectionViews.Add(new ConnectionView
        {
            fromId = fromId,
            toId = toId,
            graphic = image,
        });
    }

    private static Sprite GetArrowSprite()
    {
        if (arrowHeadSprite != null)
        {
            return arrowHeadSprite;
        }

        const int width = 12;
        const int height = 10;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        int mid = height / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int half = Mathf.RoundToInt(x / (float)(width - 1) * mid);
                texture.SetPixel(x, y, y >= mid - half && y <= mid + half ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        arrowHeadSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(1f, 0.5f),
            16f);
        arrowHeadSprite.name = "TechTreeArrow";
        return arrowHeadSprite;
    }

    private static Sprite GetDashSprite()
    {
        if (dashSprite != null)
        {
            return dashSprite;
        }

        const int width = 2;
        const int height = 16;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.HideAndDontSave,
        };
        for (int y = 0; y < height; y++)
        {
            Color color = y < 8 ? ColumnGuide : Color.clear;
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        dashSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            1f,
            0u,
            SpriteMeshType.FullRect,
            Vector4.zero);
        dashSprite.name = "TechTreeDash";
        return dashSprite;
    }

    private void CreateNode(TechTreeCatalog.Node node)
    {
        var nodeObject = new GameObject($"Node_{node.id}");
        nodeObject.transform.SetParent(graphRect, false);
        var rect = nodeObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(NodeWidth, NodeHeight);
        rect.anchoredPosition = NodeTopLeft(node);

        var hitImage = nodeObject.AddComponent<Image>();
        hitImage.color = new Color(1f, 1f, 1f, 0f);
        hitImage.raycastTarget = true;

        var button = nodeObject.AddComponent<Button>();
        button.targetGraphic = hitImage;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.92f, 0.88f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        TechTreeCatalog.Node captured = node;
        button.onClick.AddListener(() => SelectNode(captured));

        var slotObject = new GameObject("Slot");
        slotObject.transform.SetParent(nodeObject.transform, false);
        var slotRect = slotObject.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 1f);
        slotRect.anchorMax = new Vector2(0.5f, 1f);
        slotRect.pivot = new Vector2(0.5f, 1f);
        slotRect.anchoredPosition = Vector2.zero;
        slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        var slot = slotObject.AddComponent<Image>();
        UiNoteBookSlot.ApplySlot(slot);
        slot.raycastTarget = false;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(SlotSize * 0.58f, SlotSize * 0.58f);
        var icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        GameObject highlight = UiNoteBookSlot.CreateSelectHighlight(slotObject.transform, SlotSize);
        highlight.SetActive(false);

        var nameObject = new GameObject("Name");
        nameObject.transform.SetParent(nodeObject.transform, false);
        var nameRect = nameObject.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(0f, 28f);
        var nameLabel = TmpUiStyle.Create(nameObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.Top, true);
        nameLabel.fontSize = 14f;
        nameLabel.color = Ink;
        nameLabel.text = node.name;
        nameLabel.textWrappingMode = TextWrappingModes.Normal;
        nameLabel.overflowMode = TextOverflowModes.Truncate;

        nodeViews.Add(new NodeView
        {
            node = node,
            root = nodeObject,
            slot = slot,
            icon = icon,
            highlight = highlight,
            nameLabel = nameLabel,
            button = button,
        });
    }

    private void SelectNode(TechTreeCatalog.Node node)
    {
        selectedNode = node;
        selectedLegacyNode = null;
        RefreshDetail();
        RefreshNodeVisuals();
    }

    private void RefreshNodeVisuals()
    {
        UnlockManager unlocks = UnlockManager.Instance;
        for (int i = 0; i < connectionViews.Count; i++)
        {
            ConnectionView view = connectionViews[i];
            if (view.graphic == null)
            {
                continue;
            }

            bool fromUnlocked = unlocks != null && unlocks.IsUnlocked(view.fromId);
            bool toUnlocked = unlocks != null && unlocks.IsUnlocked(view.toId);
            view.graphic.color = toUnlocked
                ? LineUnlocked
                : fromUnlocked
                    ? LineReady
                    : LineLocked;
        }

        for (int i = 0; i < nodeViews.Count; i++)
        {
            NodeView view = nodeViews[i];
            bool unlocked = unlocks != null && unlocks.IsUnlocked(view.node.id);
            bool canUnlock = unlocks != null && unlocks.CanUnlock(view.node);
            bool selected = selectedNode != null && selectedNode.id == view.node.id;

            if (unlocked)
            {
                UiNoteBookSlot.ApplyUnlockedSlot(view.slot);
            }
            else if (canUnlock)
            {
                UiNoteBookSlot.ApplySlot(view.slot);
            }
            else
            {
                UiNoteBookSlot.ApplyLockedSlot(view.slot);
            }

            Sprite icon = unlocked || canUnlock
                ? UiNoteBookSlot.GetTechIcon(view.node.id)
                : null;
            view.icon.sprite = icon;
            view.icon.enabled = icon != null;
            view.icon.color = Color.white;
            if (view.highlight != null)
            {
                view.highlight.SetActive(selected);
            }

            view.nameLabel.color = unlocked || canUnlock ? Ink : InkMuted;
        }
    }

    private void RefreshDetail()
    {
        if (detailTitle == null)
        {
            return;
        }

        if (selectedNode == null)
        {
            detailTitle.text = "기술을 고르세요";
            detailBody.text = "명예를 소모해 기계를 해금합니다.\n해금한 뒤에만 제작할 수 있습니다.";
            detailCost.text = string.Empty;
            unlockButton.interactable = false;
            unlockLabel.text = "해금";
            return;
        }

        UnlockManager unlocks = UnlockManager.Instance;
        bool unlocked = unlocks != null && unlocks.IsUnlocked(selectedNode.id);
        bool canUnlock = unlocks != null && unlocks.CanUnlock(selectedNode);
        int honor = GetHonor();
        detailTitle.text = selectedNode.name;
        detailBody.text = BuildDetailBody(selectedNode, unlocked, canUnlock);
        detailCost.text = FormatUnlockCost(selectedNode, unlocked, honor);
        if (unlocked)
        {
            unlockButton.interactable = false;
            unlockLabel.text = "해금됨";
            return;
        }

        if (!string.IsNullOrEmpty(selectedNode.grantOnQuestId))
        {
            detailCost.text = "레이에게 골드 1을 주면 해금됩니다.";
            unlockButton.interactable = false;
            unlockLabel.text = "의뢰 해금";
            return;
        }

        if (!canUnlock)
        {
            unlockButton.interactable = false;
            unlockLabel.text = "잠김";
            return;
        }

        if (honor < selectedNode.honor)
        {
            unlockButton.interactable = false;
            unlockLabel.text = "명예 부족";
            return;
        }

        unlockButton.interactable = true;
        unlockLabel.text = "해금";
    }

    private static string BuildDetailBody(
        TechTreeCatalog.Node node,
        bool unlocked,
        bool canUnlock)
    {
        var text = new System.Text.StringBuilder();
        text.Append(node.description);
        string machineNames = ResolveUnlockedMachineNames(node);
        if (!string.IsNullOrEmpty(machineNames))
        {
            text.Append("\n\n해금 기계: ");
            text.Append(machineNames);
        }
        else if (node.isFuelTrack)
        {
            text.Append("\n\n생산 하루를 ");
            text.Append(node.dayMinutes);
            text.Append("분으로 늘립니다.");
        }

        if (unlocked)
        {
            return text.ToString();
        }

        if (!string.IsNullOrEmpty(node.grantOnQuestId))
        {
            text.Append("\n레이의 의뢰(골드 1)를 마치면 자동으로 해금됩니다.");
            return text.ToString();
        }

        var missing = new System.Text.StringBuilder();
        var ready = new System.Text.StringBuilder();
        bool hasIncoming = false;
        TechTreeCatalog.ForEachIncomingParent(node.id, parentId =>
        {
            TechTreeCatalog.Node parent = TechTreeCatalog.Get(parentId);
            if (parent == null || !parent.visibleInGame)
            {
                return;
            }

            hasIncoming = true;
            bool parentUnlocked = UnlockManager.Instance != null
                && UnlockManager.Instance.IsUnlocked(parent.id);
            System.Text.StringBuilder target = parentUnlocked ? ready : missing;
            if (target.Length > 0)
            {
                target.Append(", ");
            }

            target.Append(parent.name);
        });

        if (!hasIncoming)
        {
            return text.ToString();
        }

        if (missing.Length > 0)
        {
            text.Append("\n선행 기술: ");
            text.Append(missing);
        }
        else if (!canUnlock)
        {
            if (HasLockedQuestGrantParent(node))
            {
                text.Append("\n레이의 의뢰(골드 1)를 마치면 열립니다.");
            }
            else
            {
                text.Append("\n선행 기술을 먼저 해금해야 합니다.");
            }
        }
        else if (ready.Length > 0)
        {
            text.Append("\n선행 기술: ");
            text.Append(ready);
            text.Append(" (완료)");
        }

        return text.ToString();
    }

    private static bool HasLockedQuestGrantParent(TechTreeCatalog.Node node)
    {
        bool locked = false;
        TechTreeCatalog.ForEachIncomingParent(node.id, parentId =>
        {
            TechTreeCatalog.Node parent = TechTreeCatalog.Get(parentId);
            if (parent == null
                || string.IsNullOrEmpty(parent.grantOnQuestId)
                || UnlockManager.Instance == null
                || UnlockManager.Instance.IsUnlocked(parent.id))
            {
                return;
            }

            locked = true;
        });

        return locked;
    }

    private static string FormatUnlockCost(TechTreeCatalog.Node node, bool unlocked, int honor)
    {
        if (unlocked)
        {
            return node.isFuelTrack
                ? $"생산 하루 {node.dayMinutes}분"
                : "이미 해금됨";
        }

        if (honor >= node.honor)
        {
            return $"해금 비용  명예 {node.honor}\n보유  명예 {honor}";
        }

        return $"해금 비용  명예 {node.honor}\n보유  명예 {honor}  ·  부족 {node.honor - honor}";
    }

    private static string ResolveUnlockedMachineNames(TechTreeCatalog.Node node)
    {
        var names = new System.Text.StringBuilder();
        var seen = new HashSet<string>();
        for (int i = 0; i < MachineCraftCatalog.All.Length; i++)
        {
            MachineCraftCatalog.Recipe recipe = MachineCraftCatalog.All[i];
            if (recipe == null
                || recipe.requiredTechId != node.id
                || string.IsNullOrEmpty(recipe.machineDefId)
                || !seen.Add(recipe.machineDefId))
            {
                continue;
            }

            if (names.Length > 0)
            {
                names.Append(", ");
            }

            names.Append(ResolveMachineName(recipe.machineDefId));
        }

        if (names.Length == 0 && !string.IsNullOrEmpty(node.machineDefId))
        {
            return ResolveMachineName(node.machineDefId);
        }

        return names.ToString();
    }

    private static string ResolveMachineName(string machineDefId)
    {
        PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
        MachineDatabase database = movement != null ? movement.MachineDatabase : null;
        ItemDef_Machine definition = database != null ? database.Get(machineDefId) : null;
        if (definition != null && !string.IsNullOrEmpty(definition.displayName))
        {
            return definition.displayName;
        }

        return machineDefId;
    }

    private void TryUnlockSelected()
    {
        if (selectedNode == null)
        {
            if (selectedLegacyNode != null && UnlockManager.Instance != null)
            {
                int gold = GameSessionState.Instance != null ? GameSessionState.Instance.gold : 0;
                int reputation = GetHonor();
                UnlockManager.Instance.TryUnlock(selectedLegacyNode, ref gold, ref reputation);
            }

            return;
        }

        if (UnlockManager.Instance == null)
        {
            return;
        }

        if (!UnlockManager.Instance.TryUnlock(selectedNode.id, out string error))
        {
            detailCost.text = string.IsNullOrEmpty(error) ? "해금할 수 없습니다." : error;
            return;
        }

        RefreshHonor();
        RefreshNodeVisuals();
        RefreshDetail();
    }

    private void RefreshHonor()
    {
        if (honorText != null)
        {
            honorText.text = $"명예 {GetHonor()}";
        }
    }

    private static int GetHonor()
    {
        Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
        if (economy != null)
        {
            return economy.Reputation;
        }

        return GameSessionState.Instance != null ? GameSessionState.Instance.reputation : 0;
    }

    private static Vector2 NodeTopLeft(TechTreeCatalog.Node node)
    {
        return new Vector2(GraphPad + node.x * ScaleX, -(GraphPad + node.y * ScaleY));
    }

    private static Vector2 NodeCenter(TechTreeCatalog.Node node)
    {
        Vector2 topLeft = NodeTopLeft(node);
        return new Vector2(topLeft.x + NodeWidth * 0.5f, topLeft.y - SlotSize * 0.5f);
    }

    private void ClearGraph()
    {
        for (int i = 0; i < nodeViews.Count; i++)
        {
            if (nodeViews[i].root != null)
            {
                Destroy(nodeViews[i].root);
            }
        }

        nodeViews.Clear();
        for (int i = 0; i < connectionViews.Count; i++)
        {
            if (connectionViews[i].graphic != null)
            {
                Destroy(connectionViews[i].graphic.gameObject);
            }
        }

        connectionViews.Clear();
        for (int i = 0; i < columnGuides.Count; i++)
        {
            if (columnGuides[i] != null)
            {
                Destroy(columnGuides[i]);
            }
        }

        columnGuides.Clear();
        graphBuilt = false;
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();
        if (overlayCanvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("TechTreeCanvas");
        canvasObject.transform.SetParent(transform, false);
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 72;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("TechTreeModal");
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
        backdropImage.color = new Color(0f, 0f, 0f, 0.5f);
        var backdropButton = backdropObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);

        var panelObject = new GameObject("TechTreePanelRuntime");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1480f, 820f);
        var panelImage = panelObject.AddComponent<Image>();
        UiPanelFrame.Apply(panelImage);

        var headerObject = new GameObject("Header");
        headerObject.transform.SetParent(panelObject.transform, false);
        var headerRect = headerObject.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 56f);
        headerRect.anchoredPosition = Vector2.zero;
        var headerText = TmpUiStyle.Create(headerObject, TmpUiStyle.Role.Title, TextAlignmentOptions.MidlineLeft, true);
        headerText.fontSize = 22f;
        headerText.color = Ink;
        headerText.text = "테크 트리";
        var headerTextRect = headerText.rectTransform;
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.offsetMin = new Vector2(36f, 0f);
        headerTextRect.offsetMax = new Vector2(-220f, 0f);

        var honorObject = new GameObject("Honor");
        honorObject.transform.SetParent(panelObject.transform, false);
        var honorRect = honorObject.AddComponent<RectTransform>();
        honorRect.anchorMin = new Vector2(1f, 1f);
        honorRect.anchorMax = new Vector2(1f, 1f);
        honorRect.pivot = new Vector2(1f, 1f);
        honorRect.anchoredPosition = new Vector2(-72f, -8f);
        honorRect.sizeDelta = new Vector2(220f, 40f);
        honorText = TmpUiStyle.Create(honorObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.MidlineRight, true);
        honorText.fontSize = 18f;
        honorText.color = InkGold;
        honorText.text = "명예 0";

        CreateCloseButton(panelObject.transform);

        var parchmentObject = new GameObject("Parchment");
        parchmentObject.transform.SetParent(panelObject.transform, false);
        var parchmentRect = parchmentObject.AddComponent<RectTransform>();
        parchmentRect.anchorMin = Vector2.zero;
        parchmentRect.anchorMax = Vector2.one;
        parchmentRect.offsetMin = new Vector2(28f, 24f);
        parchmentRect.offsetMax = new Vector2(-28f, -64f);
        var parchmentImage = parchmentObject.AddComponent<Image>();
        UiPanelFrame.Apply(parchmentImage, UiPanelFrame.Kind.Parchment, 0.9f);
        parchmentImage.raycastTarget = true;

        var detailObject = new GameObject("Detail");
        detailObject.transform.SetParent(parchmentObject.transform, false);
        var detailRect = detailObject.AddComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(1f, 0f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.pivot = new Vector2(1f, 0.5f);
        detailRect.offsetMin = new Vector2(-340f, 18f);
        detailRect.offsetMax = new Vector2(-18f, -18f);

        var titleObject = new GameObject("DetailTitle");
        titleObject.transform.SetParent(detailObject.transform, false);
        var titleRect = titleObject.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 56f);
        titleRect.anchoredPosition = Vector2.zero;
        detailTitle = TmpUiStyle.Create(titleObject, TmpUiStyle.Role.Title, TextAlignmentOptions.MidlineLeft, true);
        detailTitle.fontSize = 24f;
        detailTitle.color = Ink;
        var detailTitleRect = detailTitle.rectTransform;
        detailTitleRect.offsetMin = new Vector2(8f, 0f);
        detailTitleRect.offsetMax = new Vector2(-8f, 0f);

        var bodyScrollObject = new GameObject("DetailBodyScroll");
        bodyScrollObject.transform.SetParent(detailObject.transform, false);
        var bodyScrollRect = bodyScrollObject.AddComponent<RectTransform>();
        bodyScrollRect.anchorMin = Vector2.zero;
        bodyScrollRect.anchorMax = Vector2.one;
        bodyScrollRect.offsetMin = new Vector2(8f, 136f);
        bodyScrollRect.offsetMax = new Vector2(-8f, -64f);

        var bodyViewportObject = new GameObject("Viewport");
        bodyViewportObject.transform.SetParent(bodyScrollObject.transform, false);
        var bodyViewportRect = bodyViewportObject.AddComponent<RectTransform>();
        bodyViewportRect.anchorMin = Vector2.zero;
        bodyViewportRect.anchorMax = Vector2.one;
        bodyViewportRect.offsetMin = Vector2.zero;
        bodyViewportRect.offsetMax = Vector2.zero;
        bodyViewportObject.AddComponent<RectMask2D>();
        var bodyViewportImage = bodyViewportObject.AddComponent<Image>();
        bodyViewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        var bodyObject = new GameObject("DetailBody");
        bodyObject.transform.SetParent(bodyViewportObject.transform, false);
        var bodyRect = bodyObject.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(0f, 0f);
        detailBody = TmpUiStyle.Create(bodyObject, TmpUiStyle.Role.Body, TextAlignmentOptions.TopLeft, true);
        detailBody.fontSize = 18f;
        detailBody.color = Ink;
        detailBody.textWrappingMode = TextWrappingModes.Normal;
        detailBody.overflowMode = TextOverflowModes.Overflow;
        var bodyFitter = bodyObject.AddComponent<ContentSizeFitter>();
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bodyScroll = bodyScrollObject.AddComponent<ScrollRect>();
        bodyScroll.viewport = bodyViewportRect;
        bodyScroll.content = bodyRect;
        bodyScroll.horizontal = false;
        bodyScroll.vertical = true;
        bodyScroll.movementType = ScrollRect.MovementType.Clamped;
        bodyScroll.scrollSensitivity = 24f;

        var costObject = new GameObject("DetailCost");
        costObject.transform.SetParent(detailObject.transform, false);
        var costRect = costObject.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0f, 0f);
        costRect.anchorMax = new Vector2(1f, 0f);
        costRect.pivot = new Vector2(0.5f, 0f);
        costRect.anchoredPosition = new Vector2(0f, 64f);
        costRect.sizeDelta = new Vector2(0f, 56f);
        detailCost = TmpUiStyle.Create(costObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.BottomLeft, true);
        detailCost.fontSize = 18f;
        detailCost.color = InkGold;
        detailCost.textWrappingMode = TextWrappingModes.Normal;
        detailCost.overflowMode = TextOverflowModes.Truncate;
        var detailCostRect = detailCost.rectTransform;
        detailCostRect.offsetMin = new Vector2(8f, 0f);
        detailCostRect.offsetMax = new Vector2(-8f, 0f);

        var unlockObject = new GameObject("UnlockButton");
        unlockObject.transform.SetParent(detailObject.transform, false);
        var unlockRect = unlockObject.AddComponent<RectTransform>();
        unlockRect.anchorMin = new Vector2(0f, 0f);
        unlockRect.anchorMax = new Vector2(1f, 0f);
        unlockRect.pivot = new Vector2(0.5f, 0f);
        unlockRect.anchoredPosition = new Vector2(0f, 12f);
        unlockRect.sizeDelta = new Vector2(-8f, 44f);
        unlockObject.AddComponent<Image>();
        unlockButton = unlockObject.AddComponent<Button>();
        unlockButton.onClick.AddListener(TryUnlockSelected);
        UiButtonStyle.Apply(unlockButton);
        var unlockLabelObject = new GameObject("Label");
        unlockLabelObject.transform.SetParent(unlockObject.transform, false);
        var unlockLabelRect = unlockLabelObject.AddComponent<RectTransform>();
        unlockLabelRect.anchorMin = Vector2.zero;
        unlockLabelRect.anchorMax = Vector2.one;
        unlockLabelRect.offsetMin = Vector2.zero;
        unlockLabelRect.offsetMax = Vector2.zero;
        unlockLabel = TmpUiStyle.Create(unlockLabelObject, TmpUiStyle.Role.Button, TextAlignmentOptions.Center);
        unlockLabel.fontSize = 16f;
        unlockLabel.color = Color.white;
        unlockLabel.text = "해금";

        var scrollObject = new GameObject("GraphScroll");
        scrollObject.transform.SetParent(parchmentObject.transform, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(18f, 18f);
        scrollRectTransform.offsetMax = new Vector2(-348f, -18f);

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
        graphRect = contentObject.AddComponent<RectTransform>();
        graphRect.anchorMin = new Vector2(0f, 1f);
        graphRect.anchorMax = new Vector2(0f, 1f);
        graphRect.pivot = new Vector2(0f, 1f);
        graphRect.anchoredPosition = Vector2.zero;
        graphRect.sizeDelta = new Vector2(1600f, 700f);

        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = graphRect;
        scroll.horizontal = true;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
    }

    private void CreateCloseButton(Transform parent)
    {
        var closeObject = new GameObject("CloseButton");
        closeObject.transform.SetParent(parent, false);
        var closeRect = closeObject.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -10f);
        closeRect.sizeDelta = new Vector2(44f, 44f);
        closeObject.AddComponent<Image>();
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
