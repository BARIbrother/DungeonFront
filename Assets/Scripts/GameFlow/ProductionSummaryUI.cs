using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 생산 종료 시 완성품 목록을 모달로 보여주고, 확인 시 Settlement로 전환한다.
public class ProductionSummaryUI : MonoBehaviour
{
    private static ProductionSummaryUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform listRect;
    private Text titleText;
    private Text emptyText;
    private readonly List<GameObject> lineObjects = new();
    private Font uiFont;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<ProductionSummaryUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("ProductionSummaryUISystem");
        systemObject.AddComponent<ProductionSummaryUI>();
    }

    public static void Show(List<ProductionSummaryLine> lines)
    {
        EnsureInstance();
        instance.Open(lines);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<ProductionSummaryUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("ProductionSummaryUISystem");
        instance = systemObject.AddComponent<ProductionSummaryUI>();
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

    private void Open(List<ProductionSummaryLine> lines)
    {
        titleText.text = "생산 종료";
        RebuildLines(lines);
        isOpen = true;
        modalRoot.SetActive(true);
    }

    private void Hide()
    {
        isOpen = false;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    // 확인: 팝업 닫기 → Settlement 전환 → 스토리 이벤트 발행.
    private void OnConfirmClicked()
    {
        Hide();
        ProductionEndHandler.ClearEnding();

        if (GameSessionState.Instance != null)
        {
            // Settlement 전환 시 FactoryStoryHooks가 OnProductionEnded / 001E00005를 발행한다.
            GameSessionState.Instance.SetPhase(GamePhase.Settlement);
            GameSessionState.Instance.ClearEndingProduction();
        }
    }

    private void RebuildLines(List<ProductionSummaryLine> lines)
    {
        ClearLines();

        if (listRect == null)
        {
            return;
        }

        bool hasLines = lines != null && lines.Count > 0;
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasLines);
            if (!hasLines)
            {
                emptyText.text = "이번 생산에서 완성된 품목이 없습니다";
            }
        }

        if (!hasLines)
        {
            return;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            ProductionSummaryLine line = lines[i];
            if (line.count <= 0)
            {
                continue;
            }

            string name = string.IsNullOrEmpty(line.displayName) ? line.itemId : line.displayName;
            CreateLineLabel($"{name} × {line.count}");
        }
    }

    private void CreateLineLabel(string message)
    {
        var labelObject = new GameObject("SummaryLine");
        labelObject.transform.SetParent(listRect, false);

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 36f;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = message;

        lineObjects.Add(labelObject);
    }

    private void ClearLines()
    {
        foreach (GameObject lineObject in lineObjects)
        {
            if (lineObject != null)
            {
                Destroy(lineObject);
            }
        }

        lineObjects.Clear();
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();

        if (canvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("ProductionSummaryCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("SummaryModal");
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
        backdropImage.color = new Color(0f, 0f, 0f, 0.55f);

        var panelObject = new GameObject("SummaryPanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(480f, 420f);

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
        titleText.fontSize = 20;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.text = "생산 종료";

        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = new Vector2(-16f, 0f);

        var scrollObject = new GameObject("SummaryScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);
        var scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(16f, 72f);
        scrollRectTransform.offsetMax = new Vector2(-16f, -56f);

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
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(8, 8, 0, 0);

        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = listRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var emptyObject = new GameObject("EmptyLabel");
        emptyObject.transform.SetParent(panelObject.transform, false);
        var emptyRect = emptyObject.AddComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0f, 0.35f);
        emptyRect.anchorMax = new Vector2(1f, 0.75f);
        emptyRect.offsetMin = new Vector2(24f, 0f);
        emptyRect.offsetMax = new Vector2(-24f, 0f);

        emptyText = emptyObject.AddComponent<Text>();
        emptyText.font = uiFont;
        emptyText.fontSize = 16;
        emptyText.alignment = TextAnchor.MiddleCenter;
        emptyText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        emptyText.text = "이번 생산에서 완성된 품목이 없습니다";
        emptyObject.SetActive(false);

        CreateConfirmButton(panelObject.transform);
    }

    private void CreateConfirmButton(Transform parent)
    {
        var buttonObject = new GameObject("ConfirmButton");
        buttonObject.transform.SetParent(parent, false);
        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 16f);
        buttonRect.sizeDelta = new Vector2(160f, 44f);

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.35f, 0.55f, 0.85f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(OnConfirmClicked);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "확인";
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
