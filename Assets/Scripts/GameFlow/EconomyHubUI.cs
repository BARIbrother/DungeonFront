using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 기존 ShopUI/UnlockManager의 로직을 호출만 하는 정식 상점·해금 화면이다.
/// 기존 서비스와 프리팹의 직렬화 필드는 수정하지 않는다.
/// </summary>
public sealed class EconomyHubUI : MonoBehaviour
{
    private static EconomyHubUI instance;
    private const string PauseRequester = "EconomyHub";

    private GameObject modal;
    private RectTransform list;
    private TMP_Text goldText;
    private TMP_Text reputationText;
    private TMP_Text feedbackText;
    private ShopUI shop;
    private UnlockManager unlocks;
    private Week3EconomyService economy;
    private ShopCatalog catalog;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            new GameObject("EconomyHubUI").AddComponent<EconomyHubUI>();
        }
    }

    public static void Show()
    {
        if (instance != null)
        {
            instance.SetVisible(true);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUi();
    }

    private void OnDisable() => GamePauseService.ReleasePause(PauseRequester);

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (!visible && !GamePauseService.IsPaused)
            {
                SetVisible(true);
            }
            else if (visible)
            {
                SetVisible(false);
            }
        }
    }

    private void SetVisible(bool nextVisible)
    {
        if (nextVisible && GameSessionState.Instance != null && GameSessionState.Instance.Phase != GamePhase.Prepare)
        {
            Debug.Log("[Shop] 상점은 준비 단계에서만 열 수 있습니다.");
            return;
        }

        visible = nextVisible;
        modal.SetActive(visible);
        if (visible)
        {
            GamePauseService.RequestPause(PauseRequester);
            ResolveServices();
            Rebuild();
        }
        else
        {
            GamePauseService.ReleasePause(PauseRequester);
        }
    }

    private void ResolveServices()
    {
        shop ??= FindAnyObjectByType<ShopUI>();
        unlocks ??= FindAnyObjectByType<UnlockManager>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        if (catalog == null)
        {
            ShopCatalog[] loaded = Resources.FindObjectsOfTypeAll<ShopCatalog>();
            if (loaded.Length > 0)
            {
                catalog = loaded[0];
            }
        }
    }

    private void Rebuild()
    {
        foreach (Transform child in list)
        {
            Destroy(child.gameObject);
        }

        goldText.text = $"골드 {economy?.Gold ?? 0}";
        reputationText.text = $"명성 {economy?.Reputation ?? 0}";
        feedbackText.text = "B 또는 닫기 버튼으로 돌아가기";

        if (shop == null || catalog == null)
        {
            feedbackText.text = "상점 카탈로그를 찾지 못했습니다. QuestSystemRoot의 ShopUI 연결을 확인하세요.";
            return;
        }

        foreach (ShopEntry entry in catalog.entries)
        {
            if (entry == null)
            {
                continue;
            }
            CreateEntry(entry);
        }
    }

    private void CreateEntry(ShopEntry entry)
    {
        GameObject row = Panel($"Entry_{entry.entryId}", list, new Color(0.16f, 0.19f, 0.27f, 1f));
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.minHeight = 76f;

        string itemName = string.IsNullOrWhiteSpace(entry.displayName) ? entry.entryId : entry.displayName;
        TMP_Text label = Text("Label", row.transform, 20, TextAlignmentOptions.Left, Color.white);
        label.text = $"{itemName}\n{entry.price} G";
        Stretch(label.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.62f, 0.9f));

        bool isLockedMachine = entry.IsMachine && (unlocks == null || !unlocks.IsUnlocked(entry.machineDefId));
        string buttonText = isLockedMachine
            ? $"명성 {unlocks?.GetRequiredReputation(entry.machineDefId) ?? 0} 해금"
            : "구매";
        Button button = Button("Action", row.transform, buttonText);
        Stretch(button.GetComponent<RectTransform>(), new Vector2(0.66f, 0.18f), new Vector2(0.96f, 0.82f));
        button.interactable = isLockedMachine
            ? unlocks != null && economy != null && economy.Reputation >= unlocks.GetRequiredReputation(entry.machineDefId)
            : economy != null && economy.Gold >= entry.price;
        button.onClick.AddListener(() =>
        {
            if (isLockedMachine)
            {
                bool unlocked = unlocks != null && unlocks.TryUnlock(entry.machineDefId);
                feedbackText.text = unlocked ? $"{itemName} 해금 완료" : "해금 조건을 만족하지 못했습니다.";
            }
            else
            {
                bool purchased = shop.TryPurchase(entry.entryId);
                feedbackText.text = purchased ? $"{itemName} 구매 완료" : "구매하지 못했습니다.";
            }
            Rebuild();
        });
    }

    private void CreateUi()
    {
        EnsureEventSystem();
        GameObject canvasObject = new("EconomyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1950;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        modal = Panel("Modal", canvasObject.transform, new Color(0f, 0f, 0f, 0.7f));
        Stretch(modal.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject window = Panel("Window", modal.transform, new Color(0.07f, 0.1f, 0.16f, 1f));
        Stretch(window.GetComponent<RectTransform>(), new Vector2(0.26f, 0.13f), new Vector2(0.74f, 0.87f));
        TMP_Text title = Text("Title", window.transform, 38, TextAlignmentOptions.Left, Color.white);
        title.text = "상점 · 기계 해금";
        Stretch(title.rectTransform, new Vector2(0.06f, 0.89f), new Vector2(0.6f, 0.98f));
        goldText = Text("Gold", window.transform, 23, TextAlignmentOptions.Right, new Color(1f, 0.83f, 0.35f));
        Stretch(goldText.rectTransform, new Vector2(0.62f, 0.89f), new Vector2(0.92f, 0.98f));
        reputationText = Text("Reputation", window.transform, 20, TextAlignmentOptions.Right, new Color(0.55f, 0.82f, 1f));
        Stretch(reputationText.rectTransform, new Vector2(0.62f, 0.83f), new Vector2(0.92f, 0.9f));
        Button close = Button("Close", window.transform, "닫기");
        Stretch(close.GetComponent<RectTransform>(), new Vector2(0.78f, 0.04f), new Vector2(0.92f, 0.11f));
        close.onClick.AddListener(() => SetVisible(false));
        feedbackText = Text("Feedback", window.transform, 17, TextAlignmentOptions.Left, Color.white);
        Stretch(feedbackText.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.75f, 0.11f));

        GameObject scrollObject = new("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(window.transform, false);
        Stretch(scrollObject.GetComponent<RectTransform>(), new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.8f));
        GameObject viewport = Panel("Viewport", scrollObject.transform, new Color(1f, 1f, 1f, 0.02f));
        Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        GameObject content = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        list = content.GetComponent<RectTransform>();
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.pivot = new Vector2(0.5f, 1f);
        list.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = list;
        scroll.horizontal = false;
        modal.SetActive(false);
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        GameObject result = new(name, typeof(RectTransform), typeof(Image));
        result.transform.SetParent(parent, false);
        result.GetComponent<Image>().color = color;
        return result;
    }

    private static TMP_Text Text(string name, Transform parent, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject result = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        result.transform.SetParent(parent, false);
        TMP_Text text = result.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button Button(string name, Transform parent, string label)
    {
        GameObject result = Panel(name, parent, new Color(0.25f, 0.45f, 0.72f));
        Button button = result.AddComponent<Button>();
        TMP_Text text = Text("Label", result.transform, 18, TextAlignmentOptions.Center, Color.white);
        text.text = label;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)));
        }
    }
}
