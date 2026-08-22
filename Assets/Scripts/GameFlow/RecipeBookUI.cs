using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 레시피북. K키로 열고 닫는다. 왼쪽 챕터, 오른쪽 식·아이콘·기계.
public class RecipeBookUI : MonoBehaviour
{
    private static readonly Color Ink = Color.black;
    private static readonly Color SpineColor = new Color(0.42f, 0.32f, 0.18f, 0.5f);
    private static readonly Color RowNormal = new Color(1f, 0.97f, 0.9f, 0.28f);
    private static readonly Color RowHighlight = new Color(0.95f, 0.82f, 0.45f, 0.7f);
    private const float IconSize = 52f;
    private const float RecipeHeaderHeight = 40f;

    private static RecipeBookUI instance;

    private Canvas overlayCanvas;
    private GameObject modalRoot;
    private TMP_Text pageIndicatorText;
    private Button prevButton;
    private Button nextButton;
    private bool isOpen;
    private int currentChapter;
    private int highlightRecipeIndex = -1;
    private RectTransform chapterContentRect;
    private RectTransform recipeContentRect;
    private ScrollRect recipeScroll;
    private readonly List<RectTransform> recipeRowRects = new();

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
            if (!TutorialActionLock.Allows(TutorialActionLock.Action.CloseRecipeBook))
            {
                return;
            }

            Hide();
            return;
        }

        if (!TutorialActionLock.Allows(TutorialActionLock.Action.OpenRecipeBook))
        {
            return;
        }

        Open();
    }

    private void Open()
    {
        if (!TutorialActionLock.Allows(TutorialActionLock.Action.OpenRecipeBook))
        {
            return;
        }

        EnsureUiHierarchy();
        currentChapter = 0;
        highlightRecipeIndex = -1;
        RefreshChapterView();
        modalRoot.SetActive(true);
        isOpen = true;
    }

    private void Hide()
    {
        if (isOpen && !TutorialActionLock.Allows(TutorialActionLock.Action.CloseRecipeBook))
        {
            return;
        }

        isOpen = false;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void NextPage()
    {
        if (currentChapter >= RecipeBookCatalog.Sections.Count - 1)
        {
            return;
        }

        SelectChapter(currentChapter + 1, -1);
    }

    private void PrevPage()
    {
        if (currentChapter <= 0)
        {
            return;
        }

        SelectChapter(currentChapter - 1, -1);
    }

    private void SelectChapter(int chapterIndex, int recipeIndex)
    {
        if (chapterIndex < 0 || chapterIndex >= RecipeBookCatalog.Sections.Count)
        {
            return;
        }

        currentChapter = chapterIndex;
        highlightRecipeIndex = recipeIndex;
        RefreshChapterView();
        if (recipeIndex >= 0)
        {
            Canvas.ForceUpdateCanvases();
            ScrollToRecipe(recipeIndex);
        }
    }

    private void JumpToItemRecipe(string itemId)
    {
        if (!RecipeBookCatalog.TryFindOutput(itemId, out int chapterIndex, out int recipeIndex))
        {
            return;
        }

        SelectChapter(chapterIndex, recipeIndex);
    }

    private void RefreshChapterView()
    {
        EnsureUiHierarchy();
        RebuildChapterButtons();
        RebuildRecipeRows();

        int total = Mathf.Max(1, RecipeBookCatalog.Sections.Count);
        string title = RecipeBookCatalog.Sections[currentChapter].title;
        pageIndicatorText.text = $"{title}  {currentChapter + 1} / {total}";
        prevButton.interactable = currentChapter > 0;
        nextButton.interactable = currentChapter < total - 1;
    }

    private void RebuildChapterButtons()
    {
        for (int i = chapterContentRect.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(chapterContentRect.GetChild(i).gameObject);
        }

        for (int i = 0; i < RecipeBookCatalog.Sections.Count; i++)
        {
            int chapterIndex = i;
            RecipeBookCatalog.Section section = RecipeBookCatalog.Sections[i];

            var buttonObject = new GameObject($"Chapter_{i}");
            buttonObject.transform.SetParent(chapterContentRect, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 48f);
            Image image = buttonObject.AddComponent<Image>();
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectChapter(chapterIndex, -1));
            UiButtonStyle.Apply(button);
            if (i == currentChapter)
            {
                image.color = new Color(1f, 0.9f, 0.65f, 1f);
            }

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
            TMP_Text label = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Button, TextAlignmentOptions.MidlineLeft);
            label.fontSize = 22f;
            label.color = Color.white;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 22f;
            label.text = section.title;
        }
    }

    private void RebuildRecipeRows()
    {
        for (int i = recipeContentRect.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(recipeContentRect.GetChild(i).gameObject);
        }

        recipeRowRects.Clear();
        RecipeBookCatalog.Section section = RecipeBookCatalog.Sections[currentChapter];
        for (int i = 0; i < section.recipes.Count; i++)
        {
            recipeRowRects.Add(CreateRecipeRow(section.recipes[i], i == highlightRecipeIndex));
        }

        if (recipeScroll != null)
        {
            recipeScroll.verticalNormalizedPosition = 1f;
        }
    }

    private RectTransform CreateRecipeRow(RecipeBookCatalog.RecipeLine line, bool highlight)
    {
        var rowObject = new GameObject(line.recipeId);
        rowObject.transform.SetParent(recipeContentRect, false);
        var rowRect = rowObject.AddComponent<RectTransform>();
        Image rowImage = rowObject.AddComponent<Image>();
        rowImage.color = highlight ? RowHighlight : RowNormal;
        rowImage.raycastTarget = false;
        var rowLayout = rowObject.AddComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(10, 10, 8, 10);
        rowLayout.spacing = 6f;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;
        var rowFitter = rowObject.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var headerObject = new GameObject("Header");
        headerObject.transform.SetParent(rowObject.transform, false);
        LayoutElement headerLayout = headerObject.AddComponent<LayoutElement>();
        headerLayout.minHeight = RecipeHeaderHeight;
        headerLayout.preferredHeight = RecipeHeaderHeight;
        var headerGroup = headerObject.AddComponent<HorizontalLayoutGroup>();
        headerGroup.spacing = 12f;
        headerGroup.childAlignment = TextAnchor.MiddleLeft;
        headerGroup.childControlWidth = true;
        headerGroup.childControlHeight = true;
        headerGroup.childForceExpandWidth = false;
        headerGroup.childForceExpandHeight = true;

        string outputName = line.outputs.Length > 0
            ? FormatStackName(line.outputs[0])
            : line.recipeId;
        var titleObject = new GameObject("Title");
        titleObject.transform.SetParent(headerObject.transform, false);
        LayoutElement titleLayout = titleObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.minWidth = 80f;
        TMP_Text title = TmpUiStyle.Create(titleObject, TmpUiStyle.Role.Body, TextAlignmentOptions.MidlineLeft, true);
        title.fontSize = 26f;
        title.color = Ink;
        title.enableAutoSizing = false;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.text = outputName;

        var machineObject = new GameObject("Machine");
        machineObject.transform.SetParent(headerObject.transform, false);
        LayoutElement machineLayout = machineObject.AddComponent<LayoutElement>();
        machineLayout.preferredWidth = 160f;
        machineLayout.minWidth = 120f;
        TMP_Text machineText = TmpUiStyle.Create(machineObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.MidlineRight, true);
        machineText.fontSize = 22f;
        machineText.color = Ink;
        machineText.enableAutoSizing = false;
        machineText.textWrappingMode = TextWrappingModes.NoWrap;
        machineText.text = line.machineLabel;

        var ioObject = new GameObject("IO");
        ioObject.transform.SetParent(rowObject.transform, false);
        LayoutElement ioLayoutElement = ioObject.AddComponent<LayoutElement>();
        ioLayoutElement.minHeight = IconSize;
        ioLayoutElement.preferredHeight = IconSize;
        var ioLayout = ioObject.AddComponent<HorizontalLayoutGroup>();
        ioLayout.spacing = 8f;
        ioLayout.childAlignment = TextAnchor.MiddleLeft;
        ioLayout.childControlWidth = false;
        ioLayout.childControlHeight = false;
        ioLayout.childForceExpandWidth = false;
        ioLayout.childForceExpandHeight = false;

        if (line.inputs.Length == 0 && line.manaCost <= 0)
        {
            CreatePlainLabel(ioObject.transform, "(없음)");
        }
        else
        {
            for (int i = 0; i < line.inputs.Length; i++)
            {
                CreateItemIcon(ioObject.transform, line.inputs[i]);
            }

            if (line.manaCost > 0)
            {
                CreatePlainLabel(ioObject.transform, $"마나 {line.manaCost}");
            }
        }

        CreatePlainLabel(ioObject.transform, "→");

        for (int i = 0; i < line.outputs.Length; i++)
        {
            CreateItemIcon(ioObject.transform, line.outputs[i]);
        }

        return rowRect;
    }

    private void CreateItemIcon(Transform parent, RecipeBookCatalog.Stack stack)
    {
        var slotObject = new GameObject(stack.itemId);
        slotObject.transform.SetParent(parent, false);
        var slotRect = slotObject.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(IconSize, IconSize);
        Image frame = slotObject.AddComponent<Image>();
        frame.color = new Color(0.2f, 0.16f, 0.12f, 0.18f);
        frame.raycastTarget = false;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(slotObject.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(3f, 3f);
        iconRect.offsetMax = new Vector2(-3f, -3f);
        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.preserveAspect = true;
        Sprite sprite = ItemIconResolver.ResolveById(stack.itemId);
        if (ItemIconResolver.IsUsable(sprite))
        {
            iconImage.sprite = sprite;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = new Color(0.55f, 0.5f, 0.4f, 0.7f);
        }

        bool canJump = RecipeBookCatalog.TryFindOutput(stack.itemId, out _, out _);
        iconImage.raycastTarget = canJump;
        if (canJump)
        {
            Button button = slotObject.AddComponent<Button>();
            button.targetGraphic = iconImage;
            button.transition = Selectable.Transition.None;
            string itemId = stack.itemId;
            button.onClick.AddListener(() => JumpToItemRecipe(itemId));
        }

        string badge = FormatStackBadge(stack);
        if (!string.IsNullOrEmpty(badge))
        {
            var countObject = new GameObject("Count");
            countObject.transform.SetParent(slotObject.transform, false);
            var countRect = countObject.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0f, 0f);
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            TMP_Text countText = TmpUiStyle.Create(countObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.BottomRight, true);
            countText.fontSize = 16f;
            countText.color = Ink;
            countText.raycastTarget = false;
            countText.text = badge;
        }
    }

    private static void CreatePlainLabel(Transform parent, string text)
    {
        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(36f, IconSize);
        TMP_Text label = TmpUiStyle.Create(labelObject, TmpUiStyle.Role.Body, TextAlignmentOptions.Midline, true);
        label.fontSize = 24f;
        label.color = Ink;
        label.text = text;
        LayoutElement layout = labelObject.AddComponent<LayoutElement>();
        layout.minWidth = 28f;
        layout.preferredWidth = text == "→" ? 28f : Mathf.Max(52f, text.Length * 14f);
    }

    private static string FormatStackName(RecipeBookCatalog.Stack stack)
    {
        string name = RecipeBookCatalog.ItemName(stack.itemId);
        if (stack.level > 1)
        {
            name = $"{name} lv{stack.level}";
        }

        if (stack.count > 1)
        {
            name = $"{name} x{stack.count}";
        }

        return name;
    }

    private static string FormatStackBadge(RecipeBookCatalog.Stack stack)
    {
        if (stack.level > 1 && stack.count > 1)
        {
            return $"lv{stack.level}\nx{stack.count}";
        }

        if (stack.level > 1)
        {
            return $"lv{stack.level}";
        }

        if (stack.count > 1)
        {
            return $"x{stack.count}";
        }

        return string.Empty;
    }

    private void ScrollToRecipe(int recipeIndex)
    {
        if (recipeScroll == null || recipeRowRects.Count <= 1)
        {
            return;
        }

        int clamped = Mathf.Clamp(recipeIndex, 0, recipeRowRects.Count - 1);
        recipeScroll.verticalNormalizedPosition = 1f - (clamped / (float)(recipeRowRects.Count - 1));
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
        headerText.fontSize = 28f;
        headerText.color = Ink;
        headerText.text = "레시피북";
        var headerTextRect = headerText.rectTransform;
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.offsetMin = new Vector2(36f, 0f);
        headerTextRect.offsetMax = new Vector2(-72f, 0f);

        CreateCloseButton(panelObject.transform);

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

        CreateChapterPane(bookAreaObject.transform);
        CreateRecipePane(bookAreaObject.transform);

        var spineObject = new GameObject("Spine");
        spineObject.transform.SetParent(bookAreaObject.transform, false);
        var spineRect = spineObject.AddComponent<RectTransform>();
        spineRect.anchorMin = new Vector2(0.28f, 0f);
        spineRect.anchorMax = new Vector2(0.28f, 1f);
        spineRect.pivot = new Vector2(0.5f, 0.5f);
        spineRect.sizeDelta = new Vector2(3f, 0f);
        spineRect.anchoredPosition = Vector2.zero;
        var spineImage = spineObject.AddComponent<Image>();
        spineImage.color = SpineColor;
        spineImage.raycastTarget = false;

        CreateNavBar(panelObject.transform);
    }

    private void CreateChapterPane(Transform parent)
    {
        var paneObject = new GameObject("ChapterPane");
        paneObject.transform.SetParent(parent, false);
        var paneRect = paneObject.AddComponent<RectTransform>();
        paneRect.anchorMin = new Vector2(0f, 0f);
        paneRect.anchorMax = new Vector2(0.28f, 1f);
        paneRect.offsetMin = new Vector2(16f, 16f);
        paneRect.offsetMax = new Vector2(-10f, -16f);

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(paneObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObject.AddComponent<RectMask2D>();
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        chapterContentRect = contentObject.AddComponent<RectTransform>();
        chapterContentRect.anchorMin = new Vector2(0f, 1f);
        chapterContentRect.anchorMax = new Vector2(1f, 1f);
        chapterContentRect.pivot = new Vector2(0.5f, 1f);
        chapterContentRect.sizeDelta = Vector2.zero;
        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = paneObject.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = chapterContentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private void CreateRecipePane(Transform parent)
    {
        var paneObject = new GameObject("RecipePane");
        paneObject.transform.SetParent(parent, false);
        var paneRect = paneObject.AddComponent<RectTransform>();
        paneRect.anchorMin = new Vector2(0.28f, 0f);
        paneRect.anchorMax = Vector2.one;
        paneRect.offsetMin = new Vector2(14f, 16f);
        paneRect.offsetMax = new Vector2(-16f, -16f);

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(paneObject.transform, false);
        var viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObject.AddComponent<RectMask2D>();
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        recipeContentRect = contentObject.AddComponent<RectTransform>();
        recipeContentRect.anchorMin = new Vector2(0f, 1f);
        recipeContentRect.anchorMax = new Vector2(1f, 1f);
        recipeContentRect.pivot = new Vector2(0.5f, 1f);
        recipeContentRect.sizeDelta = Vector2.zero;
        var layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        recipeScroll = paneObject.AddComponent<ScrollRect>();
        recipeScroll.viewport = viewportRect;
        recipeScroll.content = recipeContentRect;
        recipeScroll.horizontal = false;
        recipeScroll.vertical = true;
        recipeScroll.movementType = ScrollRect.MovementType.Clamped;
        recipeScroll.scrollSensitivity = 24f;
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
        indicatorRect.sizeDelta = new Vector2(320f, 32f);
        pageIndicatorText = TmpUiStyle.Create(indicatorObject, TmpUiStyle.Role.Caption, TextAlignmentOptions.Center, true);
        pageIndicatorText.fontSize = 22f;
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
