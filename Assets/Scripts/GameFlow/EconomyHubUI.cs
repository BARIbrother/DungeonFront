using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 상점 카탈로그 구매와 테크트리 기계 해금을 L키 허브에서 호출한다.
/// (원래 B키였으나 PlacementController의 배치 모드 토글과 겹쳐 임시로 L키로 변경 — Docs 참고)
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
        // 배치 모드(PlacementController)가 B키를 쓰기 때문에 상점·해금 허브는 L키를 쓴다.
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
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
        unlocks ??= UnlockManager.Instance ?? FindAnyObjectByType<UnlockManager>();
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
        feedbackText.text = "L 또는 닫기 버튼으로 돌아가기";

        if (shop == null || catalog == null)
        {
            feedbackText.text = catalog == null
                ? "상점 카탈로그를 찾지 못했습니다. QuestSystemRoot의 ShopUI 연결을 확인하세요."
                : "ShopUI가 없어도 카탈로그로 구매·해금을 진행합니다.";
            if (catalog == null)
            {
                return;
            }
        }

        foreach (ShopEntry entry in catalog.entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.IsMachine && !MachineCraftCatalog.IsObtainable(entry.machineDefId))
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
        MachineCraftCatalog.Recipe recipe = entry.IsMachine
            ? MachineCraftCatalog.Get(entry.machineDefId)
            : null;
        bool isLockedMachine = recipe != null && !MachineCraftService.IsTechUnlocked(recipe);
        TechTreeCatalog.Node techNode = isLockedMachine
            ? TechTreeCatalog.Get(recipe.requiredTechId)
            : null;

        TMP_Text label = Text("Label", row.transform, 20, TextAlignmentOptions.Left, Color.white);
        label.text = isLockedMachine
            ? $"{itemName}\n{TechTreeCatalog.DisplayName(recipe.requiredTechId)} 해금 필요"
            : recipe != null
                ? $"{itemName}\n{MachineCraftService.FormatCost(recipe)}"
                : $"{itemName}\n{entry.price} G";
        Stretch(label.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.62f, 0.9f));

        string buttonText;
        bool interactable;
        if (isLockedMachine)
        {
            int honor = techNode != null ? techNode.honor : 0;
            buttonText = honor > 0 ? $"명예 {honor} 해금" : "해금";
            interactable = unlocks != null
                && unlocks.CanUnlock(recipe.requiredTechId)
                && (honor <= 0 || (economy != null && economy.Reputation >= honor));
        }
        else
        {
            buttonText = "구매";
            if (recipe != null)
            {
                interactable = MachineCraftService.CanAfford(
                    recipe,
                    PlayerInventory.GetOrFind(),
                    economy != null ? economy.Gold : 0);
            }
            else
            {
                interactable = economy != null && economy.Gold >= entry.price;
            }
        }

        Button button = Button("Action", row.transform, buttonText);
        Stretch(button.GetComponent<RectTransform>(), new Vector2(0.66f, 0.18f), new Vector2(0.96f, 0.82f));
        button.interactable = interactable;
        button.onClick.AddListener(() =>
        {
            if (isLockedMachine)
            {
                string error = "해금 시스템을 찾지 못했습니다.";
                bool unlocked = unlocks != null && unlocks.TryUnlock(recipe.requiredTechId, out error);
                feedbackText.text = unlocked
                    ? $"{itemName} 해금 완료"
                    : string.IsNullOrEmpty(error) ? "해금 조건을 만족하지 못했습니다." : error;
            }
            else
            {
                feedbackText.text = TryBuy(entry, itemName);
            }
            Rebuild();
        });
    }

    private string TryBuy(ShopEntry entry, string itemName)
    {
        if (shop != null)
        {
            return shop.TryPurchase(entry.entryId)
                ? $"{itemName} 구매 완료"
                : "구매하지 못했습니다.";
        }

        if (entry.IsMachine)
        {
            bool crafted = MachineCraftService.TryCraft(entry.machineDefId, out string error, entry.machineDefinition);
            return crafted ? $"{itemName} 구매 완료" : error;
        }

        if (economy == null || !economy.TrySpendGold(entry.price))
        {
            PlayCatalogSfx(audio => audio.Catalog.uiDeny);
            return "골드가 부족합니다.";
        }

        PlayerInventory inventory = PlayerInventory.GetOrFind();
        if (entry.item == null || inventory == null)
        {
            economy.AddGold(entry.price);
            PlayCatalogSfx(audio => audio.Catalog.uiDeny);
            return "구매 대상을 지급할 수 없습니다.";
        }

        inventory.Add(new ItemEntry
        {
            item = Item.FromDefinition(entry.item),
            count = entry.count
        });
        PlayCatalogSfx(audio => audio.Catalog.coin);
        return $"{itemName} 구매 완료";
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
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

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
        button.gameObject.AddComponent<UiButtonSound>();
        TMP_Text text = Text("Label", result.transform, 18, TextAlignmentOptions.Center, Color.white);
        text.text = label;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static void PlayCatalogSfx(System.Func<AudioManager, AudioCatalog.AudioEntry> selectClip)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || audio.Catalog == null || selectClip == null)
        {
            return;
        }

        AudioCatalog.AudioEntry entry = selectClip(audio);
        if (entry == audio.Catalog.uiDeny || entry == audio.Catalog.coin)
        {
            UiButtonSound.SuppressClickSoundForCurrentFrame();
        }

        audio.PlaySfx(entry);
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
