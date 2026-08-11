using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 2키로 여는 가로3×세로4 구역 해금 UI. 구역 배치와 같은 모양의 버튼을 보여준다.
public class ZoneExpansionUI : MonoBehaviour
{
    private static ZoneExpansionUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform zoneGridRect;
    private Text titleText;
    private readonly List<GameObject> zoneButtons = new List<GameObject>();
    private Font uiFont;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<ZoneExpansionUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("ZoneExpansionUISystem");
        systemObject.AddComponent<ZoneExpansionUI>();
    }

    // 구역 해금 UI를 연다. 이미 열려 있으면 닫는다.
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

        instance = FindAnyObjectByType<ZoneExpansionUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("ZoneExpansionUISystem");
        instance = systemObject.AddComponent<ZoneExpansionUI>();
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

    private void Open()
    {
        RebuildZoneButtons();
        titleText.text = "구역 확장";
        modalRoot.SetActive(true);
        isOpen = true;
    }

    public void Hide()
    {
        isOpen = false;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void RebuildZoneButtons()
    {
        ClearZoneButtons();

        if (zoneGridRect == null)
        {
            return;
        }

        ZoneManager zoneManager = ZoneManager.Instance;
        if (zoneManager == null)
        {
            zoneManager = FindAnyObjectByType<ZoneManager>();
        }

        // UI는 위에서부터 채우므로 월드 y가 큰 구역이 위 줄에 온다.
        for (int uiRow = 0; uiRow < ZoneManager.ZonesY; uiRow++)
        {
            int zoneY = ZoneManager.ZonesY - 1 - uiRow;
            for (int zoneX = 0; zoneX < ZoneManager.ZonesX; zoneX++)
            {
                CreateZoneButton(zoneManager, zoneX, zoneY);
            }
        }
    }

    private void CreateZoneButton(ZoneManager zoneManager, int zoneX, int zoneY)
    {
        string zoneId = ZoneManager.GetZoneId(zoneX, zoneY);
        bool isCenter = zoneX == ZoneManager.CenterZoneX && zoneY == ZoneManager.CenterZoneY;
        bool unlocked = zoneManager != null && zoneManager.IsZoneUnlocked(zoneX, zoneY);
        bool canUnlock = zoneManager != null && zoneManager.CanUnlockZone(zoneX, zoneY);

        var buttonObject = new GameObject($"Zone_{zoneX}_{zoneY}");
        buttonObject.transform.SetParent(zoneGridRect, false);

        var buttonImage = buttonObject.AddComponent<Image>();
        if (unlocked)
        {
            buttonImage.color = new Color(0.2f, 0.35f, 0.22f, 1f);
        }
        else if (canUnlock)
        {
            buttonImage.color = new Color(0.2f, 0.22f, 0.28f, 1f);
        }
        else
        {
            buttonImage.color = new Color(0.25f, 0.18f, 0.18f, 1f);
        }

        var button = buttonObject.AddComponent<Button>();
        button.interactable = canUnlock;
        if (canUnlock)
        {
            int capturedX = zoneX;
            int capturedY = zoneY;
            button.onClick.AddListener(() => OnZoneButtonClicked(capturedX, capturedY));
        }

        UiButtonStyle.Apply(button);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(4f, 4f);
        labelRect.offsetMax = new Vector2(-4f, -4f);

        string status;
        if (isCenter)
        {
            status = "시작\n(열림)";
        }
        else if (unlocked)
        {
            status = $"{zoneId}\n(해금됨)";
        }
        else if (canUnlock)
        {
            status = $"{zoneId}\n해금";
        }
        else
        {
            status = $"{zoneId}\n(인접 필요)";
        }

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 14;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = status;

        zoneButtons.Add(buttonObject);
    }

    private void OnZoneButtonClicked(int zoneX, int zoneY)
    {
        if (!isOpen)
        {
            return;
        }

        ZoneManager zoneManager = ZoneManager.Instance;
        if (zoneManager == null)
        {
            zoneManager = FindAnyObjectByType<ZoneManager>();
        }

        if (zoneManager == null)
        {
            Debug.LogWarning("[ZoneExpansionUI] ZoneManager가 없습니다.");
            return;
        }

        if (zoneManager.TryUnlockZone(zoneX, zoneY))
        {
            RebuildZoneButtons();
        }
    }

    private void ClearZoneButtons()
    {
        for (int i = 0; i < zoneButtons.Count; i++)
        {
            if (zoneButtons[i] != null)
            {
                Destroy(zoneButtons[i]);
            }
        }

        zoneButtons.Clear();
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("ZoneExpansionCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 75;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("ZoneModal");
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

        var panelObject = new GameObject("ZonePanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(420f, 560f);

        var panelImage = panelObject.AddComponent<Image>();
        UiPanelFrame.Apply(panelImage);

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
        titleText.text = "구역 확장";

        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = new Vector2(-56f, 0f);

        CreateCloseButton(panelObject.transform);

        var gridObject = new GameObject("ZoneGrid");
        gridObject.transform.SetParent(panelObject.transform, false);
        zoneGridRect = gridObject.AddComponent<RectTransform>();
        zoneGridRect.anchorMin = Vector2.zero;
        zoneGridRect.anchorMax = Vector2.one;
        zoneGridRect.offsetMin = new Vector2(36f, 36f);
        zoneGridRect.offsetMax = new Vector2(-36f, -72f);

        var layout = gridObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(110f, 100f);
        layout.spacing = new Vector2(12f, 12f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = ZoneManager.ZonesX;
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

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "×";
        label.raycastTarget = false;
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
