using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// 맵 기계 클릭 시 AvailableRecipes를 버튼 목록으로 보여 주고 선택한다.
public class MachineRecipeUI : MonoBehaviour
{
    private static MachineRecipeUI instance;

    private Canvas canvas;
    private GameObject modalRoot;
    private RectTransform panelRect;
    private RectTransform recipeListRect;
    private Text titleText;
    private readonly List<GameObject> recipeButtons = new();
    private Machine targetMachine;
    private Font uiFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<MachineRecipeUI>() != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineRecipeUISystem");
        systemObject.AddComponent<MachineRecipeUI>();
    }

    // 레시피 전환은 MachineInfoPanel의 「레시피 변경」 목록으로 통합됐다.
    public static void ShowFor(Machine machine)
    {
        MachineInfoPanel.ShowForWithRecipeList(machine);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindAnyObjectByType<MachineRecipeUI>();
        if (instance != null)
        {
            return;
        }

        var systemObject = new GameObject("MachineRecipeUISystem");
        instance = systemObject.AddComponent<MachineRecipeUI>();
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

    private void Open(Machine machine)
    {
        targetMachine = machine;
        titleText.text = $"{machine.GetMachineDisplayName()} · 레시피";
        RebuildRecipeButtons();
        modalRoot.SetActive(true);
    }

    public void Hide()
    {
        targetMachine = null;
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    private void RebuildRecipeButtons()
    {
        ClearRecipeButtons();

        if (targetMachine == null || recipeListRect == null)
        {
            return;
        }

        RecipePool pool = targetMachine.GetAvailableRecipes();
        Recipe[] recipes = pool != null ? pool.recipes : null;
        if (recipes == null || recipes.Length == 0)
        {
            CreateInfoLabel("사용 가능한 레시피가 없습니다.");
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

    private void CreateInfoLabel(string message)
    {
        var labelObject = new GameObject("InfoLabel");
        labelObject.transform.SetParent(recipeListRect, false);

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 48f;

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        label.text = message;

        recipeButtons.Add(labelObject);
    }

    private void CreateRecipeButton(Recipe recipe, bool isSelected)
    {
        var buttonObject = new GameObject($"Recipe_{recipe.id}");
        buttonObject.transform.SetParent(recipeListRect, false);

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 44f;

        var buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = isSelected
            ? new Color(0.35f, 0.55f, 0.85f, 1f)
            : new Color(0.2f, 0.22f, 0.28f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => OnRecipeButtonClicked(recipe));

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);

        var label = labelObject.AddComponent<Text>();
        label.font = uiFont;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = BuildRecipeLabel(recipe);

        recipeButtons.Add(buttonObject);
    }

    private void OnRecipeButtonClicked(Recipe recipe)
    {
        if (targetMachine == null || recipe == null)
        {
            return;
        }

        targetMachine.SelectRecipe(recipe);
        RebuildRecipeButtons();
    }

    private static string BuildRecipeLabel(Recipe recipe)
    {
        string recipeName = string.IsNullOrEmpty(recipe.id) ? "recipe" : recipe.id;
        string inputs = DescribeItemEntries(recipe.inputEntryList);
        string outputs = DescribeItemEntries(recipe.outputEntryList);
        return $"{recipeName}  ({inputs} → {outputs}, {recipe.recipeTime})";
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

        var canvasObject = new GameObject("MachineRecipeCanvas");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        modalRoot = new GameObject("RecipeModal");
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

        var backdropButton = backdropObject.AddComponent<Button>();
        backdropButton.onClick.AddListener(Hide);

        var panelObject = new GameObject("RecipePanel");
        panelObject.transform.SetParent(modalRoot.transform, false);
        panelRect = panelObject.AddComponent<RectTransform>();
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
        titleText.fontSize = 18;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = Color.white;
        titleText.text = "레시피";

        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = new Vector2(-56f, 0f);

        CreateCloseButton(panelObject.transform);

        var scrollObject = new GameObject("RecipeScroll");
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
        recipeListRect = contentObject.AddComponent<RectTransform>();
        recipeListRect.anchorMin = new Vector2(0f, 1f);
        recipeListRect.anchorMax = new Vector2(1f, 1f);
        recipeListRect.pivot = new Vector2(0.5f, 1f);
        recipeListRect.anchoredPosition = Vector2.zero;
        recipeListRect.sizeDelta = new Vector2(0f, 0f);

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
        scroll.content = recipeListRect;
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
