using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 레시피북. K키로 열고 닫는다.
// 마인크래프트 책&깃펜 방식으로, 좌/우 두 페이지를 한 번에 보여주고 넘기며 읽는다.
// 내용·페이지 배치는 RecipeBookCatalog.PageLayout에 정의된 대로 그대로 그린다.
public class RecipeBookUI : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.22f, 0.16f, 0.1f, 1f);
    private static readonly Color HeaderInk = new Color(0.36f, 0.22f, 0.08f, 1f);
    private static readonly Color SpineColor = new Color(0.42f, 0.32f, 0.18f, 0.5f);

    private static RecipeBookUI instance;

    private Canvas overlayCanvas;
    private GameObject modalRoot;
    private TMP_Text leftPageText;
    private TMP_Text rightPageText;
    private TMP_Text pageIndicatorText;
    private Button prevButton;
    private Button nextButton;
    private bool isOpen;
    private List<string> pages;
    private int currentSpread;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<RecipeBookUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("RecipeBookUISystem");
        systemObject.AddComponent<RecipeBookUI>();
    }

    public static void Toggle()
    {
        EnsureInstance();
        instance.ToggleRecipeBook();
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

        instance = FindAnyObjectByType<RecipeBookUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("RecipeBookUISystem");
        instance = systemObject.AddComponent<RecipeBookUI>();
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        // 책이 열려 있을 때만 반응하고, 이동 등 다른 조작과는 겹치지 않는 방향키만 쓴다.
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            NextPage();
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            PrevPage();
        }
    }

    public void ToggleRecipeBook()
    {
        if (isOpen)
        {
            Hide();
            return;
        }

        Open();
    }

    private void Open()
    {
        EnsureUiHierarchy();
        RebuildPages();
        currentSpread = 0;
        RefreshPageView();
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
    }

    private void NextPage()
    {
        int totalSpreads = Mathf.Max(1, Mathf.CeilToInt(pages.Count / 2f));
        if (currentSpread >= totalSpreads - 1)
        {
            return;
        }

        currentSpread++;
        RefreshPageView();
    }

    private void PrevPage()
    {
        if (currentSpread <= 0)
        {
            return;
        }

        currentSpread--;
        RefreshPageView();
    }

    private void RebuildPages()
    {
        pages = BuildPages();
    }

    // RecipeBookCatalog.PageLayout에 정의된 페이지 구성을 그대로 그린다 (자동 계산 없음).
    // 조각(chunk)의 start가 0이면 그 섹션의 시작이라 제목을 보여주고, 0이 아니면 이전 페이지에서
    // 이어지는 조각이라 제목 자리를 비워 둔다 — 다만 같은 구분자(sep)를 그대로 써서 높이는 유지한다.
    // 페이지별 compact 여부에 따라 항목 사이 구분자를 촘촘하게(한 줄) 또는 넉넉하게(빈 줄 하나) 쓴다.
    private static List<string> BuildPages()
    {
        var sectionsByTitle = new Dictionary<string, RecipeBookCatalog.Section>();
        for (int i = 0; i < RecipeBookCatalog.Sections.Count; i++)
        {
            sectionsByTitle[RecipeBookCatalog.Sections[i].title] = RecipeBookCatalog.Sections[i];
        }

        string headerColor = ColorUtility.ToHtmlStringRGB(HeaderInk);
        var pageList = new List<string>();

        for (int p = 0; p < RecipeBookCatalog.PageLayout.Length; p++)
        {
            RecipeBookCatalog.PageChunk[] chunks = RecipeBookCatalog.PageLayout[p];
            string sep = RecipeBookCatalog.CompactPageIndices.Contains(p) ? "\n" : "\n\n";
            var current = new StringBuilder();
            bool hasContent = false;

            for (int c = 0; c < chunks.Length; c++)
            {
                RecipeBookCatalog.PageChunk chunk = chunks[c];
                if (!sectionsByTitle.TryGetValue(chunk.sectionTitle, out RecipeBookCatalog.Section section))
                {
                    continue;
                }

                if (hasContent)
                {
                    current.Append("\n\n");
                }

                if (chunk.start == 0)
                {
                    current.Append($"<b><color=#{headerColor}>{section.title}</color></b>");
                }

                current.Append(sep);

                int end = Mathf.Min(chunk.start + chunk.count, section.lines.Count);
                for (int i = chunk.start; i < end; i++)
                {
                    if (i > chunk.start)
                    {
                        current.Append(sep);
                    }

                    current.Append("· ").Append(section.lines[i]);
                }

                hasContent = true;
            }

            pageList.Add(current.ToString());
        }

        if (pageList.Count == 0)
        {
            pageList.Add(string.Empty);
        }

        return pageList;
    }

    private void RefreshPageView()
    {
        if (pages == null || pages.Count == 0)
        {
            RebuildPages();
        }

        int leftIndex = currentSpread * 2;
        int rightIndex = leftIndex + 1;

        leftPageText.text = leftIndex < pages.Count ? pages[leftIndex] : string.Empty;
        rightPageText.text = rightIndex < pages.Count ? pages[rightIndex] : string.Empty;

        int totalSpreads = Mathf.Max(1, Mathf.CeilToInt(pages.Count / 2f));
        pageIndicatorText.text = $"{currentSpread + 1} / {totalSpreads}";
        prevButton.interactable = currentSpread > 0;
        nextButton.interactable = currentSpread < totalSpreads - 1;
    }

    private void EnsureUiHierarchy()
    {
        EnsureEventSystem();
        if (overlayCanvas != null)
        {
            return;
        }

        var canvasObject = new GameObject("RecipeBookCanvas");
        canvasObject.transform.SetParent(transform, false);
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 72;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("RecipeBookModal");
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

        var panelObject = new GameObject("RecipeBookPanelRuntime");
        panelObject.transform.SetParent(modalRoot.transform, false);
        var panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1300f, 800f);
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
        headerText.text = "레시피북";
        var headerTextRect = headerText.rectTransform;
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.offsetMin = new Vector2(36f, 0f);
        headerTextRect.offsetMax = new Vector2(-72f, 0f);

        CreateCloseButton(panelObject.transform);

        // 책 펼침면 배경 (마인크래프트 책&깃펜 느낌의 양피지 프레임)
        var bookAreaObject = new GameObject("BookArea");
        bookAreaObject.transform.SetParent(panelObject.transform, false);
        var bookAreaRect = bookAreaObject.AddComponent<RectTransform>();
        bookAreaRect.anchorMin = Vector2.zero;
        bookAreaRect.anchorMax = Vector2.one;
        bookAreaRect.offsetMin = new Vector2(28f, 70f);
        bookAreaRect.offsetMax = new Vector2(-28f, -64f);
        var bookAreaImage = bookAreaObject.AddComponent<Image>();
        UiPanelFrame.Apply(bookAreaImage, UiPanelFrame.Kind.Parchment, 0.9f);
        bookAreaImage.raycastTarget = true;

        leftPageText = CreatePage(bookAreaObject.transform, "LeftPage", new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(24f, 18f), new Vector2(-14f, -18f));
        rightPageText = CreatePage(bookAreaObject.transform, "RightPage", new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(14f, 18f), new Vector2(-24f, -18f));

        var spineObject = new GameObject("Spine");
        spineObject.transform.SetParent(bookAreaObject.transform, false);
        var spineRect = spineObject.AddComponent<RectTransform>();
        spineRect.anchorMin = new Vector2(0.5f, 0f);
        spineRect.anchorMax = new Vector2(0.5f, 1f);
        spineRect.pivot = new Vector2(0.5f, 0.5f);
        spineRect.sizeDelta = new Vector2(3f, 0f);
        spineRect.anchoredPosition = Vector2.zero;
        var spineImage = spineObject.AddComponent<Image>();
        spineImage.color = SpineColor;
        spineImage.raycastTarget = false;

        CreateNavBar(panelObject.transform);
    }

    private static TMP_Text CreatePage(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        var pageObject = new GameObject(name);
        pageObject.transform.SetParent(parent, false);
        var pageRect = pageObject.AddComponent<RectTransform>();
        pageRect.anchorMin = anchorMin;
        pageRect.anchorMax = anchorMax;
        pageRect.offsetMin = offsetMin;
        pageRect.offsetMax = offsetMax;

        TMP_Text text = TmpUiStyle.Create(pageObject, TmpUiStyle.Role.Body, TextAlignmentOptions.TopLeft, true);
        text.fontSize = 24f;
        text.color = Ink;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.lineSpacing = 12f;
        // 페이지당 항목 수를 이미 여유 있게 계산해뒀으니, 자동 축소는 줄바꿈 넘칠 때 대비한 보험 정도로만 둔다.
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 24f;
        text.text = string.Empty;
        return text;
    }

    private void CreateNavBar(Transform parent)
    {
        prevButton = CreateNavButton(parent, "PrevButton", "◀ 이전", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 10f), PrevPage);
        nextButton = CreateNavButton(parent, "NextButton", "다음 ▶", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 10f), NextPage);

        var indicatorObject = new GameObject("PageIndicator");
        indicatorObject.transform.SetParent(parent, false);
        var indicatorRect = indicatorObject.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0f);
        indicatorRect.pivot = new Vector2(0.5f, 0f);
        indicatorRect.anchoredPosition = new Vector2(0f, 18f);
        indicatorRect.sizeDelta = new Vector2(160f, 32f);
        pageIndicatorText = TmpUiStyle.Create(indicatorObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.Center, true);
        pageIndicatorText.fontSize = 18f;
        pageIndicatorText.color = Ink;
        pageIndicatorText.text = "1 / 1";
    }

    private static Button CreateNavButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = pivot;
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(120f, 44f);
        buttonObject.AddComponent<Image>();
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        UiButtonStyle.Apply(button);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Button, TextAlignmentOptions.Center);
        labelText.fontSize = 18f;
        labelText.color = Color.white;
        labelText.text = label;

        return button;
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
