using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Factory 결산 페이즈에 상시 의뢰 선택·배수·납품 UI를 런타임으로 연결한다.</summary>
public sealed class PerpetualDeliveryRuntimeUI : MonoBehaviour
{
    private static PerpetualDeliveryRuntimeUI instance;
    private readonly List<Quest> quests = new();
    private GameObject openButtonObject;
    private GameObject modal;
    private TMP_Text questName;
    private int selectedIndex;
    private Slider multiplier;
    private TMP_Text detail;
    private TMP_Text feedback;
    private Button deliverButton;
    private QuestPool pool;
    private PerpetualQuestService service;
    private Week3EconomyService economy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<PerpetualQuestPanel>() != null)
        {
            return;
        }

        if (instance == null)
        {
            new GameObject("PerpetualDeliveryRuntimeUI").AddComponent<PerpetualDeliveryRuntimeUI>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
    }

    private void Update()
    {
        bool settlement = GameSessionState.Instance != null && GameSessionState.Instance.Phase == GamePhase.Settlement;
        openButtonObject.SetActive(settlement && !modal.activeSelf);
        if (!settlement && modal.activeSelf) Close();
    }

    private void Open()
    {
        Resolve();
        DestroyCopies();
        int reputation = economy != null ? economy.Reputation : 0;
        if (pool != null) quests.AddRange(pool.CreatePerpetualQuestList(reputation));
        selectedIndex = 0;
        modal.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        modal.SetActive(false);
        DestroyCopies();
    }

    private void Resolve()
    {
        pool ??= FindAnyObjectByType<QuestPool>();
        service ??= FindAnyObjectByType<PerpetualQuestService>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();
    }

    private void Refresh()
    {
        Quest quest = Selected();
        int maximum = quest != null && service != null ? service.GetMaxMultiplier(quest) : 0;
        multiplier.wholeNumbers = true;
        multiplier.minValue = maximum > 0 ? 1 : 0;
        multiplier.maxValue = Mathf.Max(1, maximum);
        multiplier.SetValueWithoutNotify(maximum > 0 ? Mathf.Clamp(multiplier.value, 1, maximum) : 0);
        int chosen = Mathf.RoundToInt(multiplier.value);
        questName.text = quest != null ? quest.title : "표시할 상시 의뢰가 없습니다.";
        detail.text = quest == null ? "" : BuildDetail(quest, chosen, maximum);
        deliverButton.interactable = quest != null && maximum > 0;
    }

    private void Deliver()
    {
        Quest quest = Selected();
        int amount = Mathf.RoundToInt(multiplier.value);
        bool success = service != null && service.TryDeliver(quest, amount);
        feedback.text = success ? $"x{amount} 납품 완료" : "재고가 부족하여 납품하지 못했습니다.";
        Refresh();
    }

    private Quest Selected() => selectedIndex >= 0 && selectedIndex < quests.Count ? quests[selectedIndex] : null;

    private void MoveSelection(int direction)
    {
        if (quests.Count == 0) return;
        selectedIndex = (selectedIndex + direction + quests.Count) % quests.Count;
        multiplier.SetValueWithoutNotify(1);
        Refresh();
    }

    private static string BuildDetail(Quest quest, int amount, int maximum)
    {
        var lines = new List<string> { $"선택 x{amount} / 가능한 최대 x{maximum}", "필요:" };
        foreach (ItemEntry entry in quest.requiredItems?.entries ?? System.Array.Empty<ItemEntry>())
        {
            if (entry?.item == null) continue;
            int owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetCount(entry.item.Id) : 0;
            string itemName = string.IsNullOrWhiteSpace(entry.item.DisplayName) ? entry.item.Id : entry.item.DisplayName;
            lines.Add($"{itemName}  보유 {owned} / 제출 {entry.count * amount}");
        }
        return string.Join("\n", lines);
    }

    private void DestroyCopies()
    {
        foreach (Quest quest in quests)
        {
            if (quest == null) continue;
            QuestRuntimeRegistry.Forget(quest);
            Destroy(quest);
        }
        quests.Clear();
    }

    private void BuildUi()
    {
        EnsureEventSystem();
        GameObject canvasObject = new("PerpetualDeliveryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1800;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);

        openButtonObject = Panel("Open", canvasObject.transform, new Color(.25f,.46f,.72f));
        Stretch(openButtonObject.GetComponent<RectTransform>(),new Vector2(.76f,.83f),new Vector2(.94f,.91f));
        Button open = openButtonObject.AddComponent<Button>(); open.onClick.AddListener(Open); AddLabel(openButtonObject.transform,"상시 의뢰",24);

        modal = Panel("Modal",canvasObject.transform,new Color(0,0,0,.72f)); Stretch(modal.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
        GameObject window = Panel("Window",modal.transform,new Color(.08f,.11f,.18f,1)); Stretch(window.GetComponent<RectTransform>(),new Vector2(.3f,.2f),new Vector2(.7f,.8f));
        AddLabel(window.transform,"상시 의뢰 납품",34,new Vector2(.08f,.84f),new Vector2(.92f,.96f));
        Button previous = CreateButton(window.transform,"◀",new Vector2(.1f,.68f),new Vector2(.22f,.79f)); previous.onClick.AddListener(()=>MoveSelection(-1));
        questName = AddLabel(window.transform,"",22,new Vector2(.24f,.68f),new Vector2(.76f,.79f));
        Button next = CreateButton(window.transform,"▶",new Vector2(.78f,.68f),new Vector2(.9f,.79f)); next.onClick.AddListener(()=>MoveSelection(1));
        multiplier = CreateSlider(window.transform); Stretch(multiplier.GetComponent<RectTransform>(),new Vector2(.1f,.56f),new Vector2(.9f,.63f)); multiplier.onValueChanged.AddListener(_=>Refresh());
        detail = AddLabel(window.transform,"",22,new Vector2(.1f,.27f),new Vector2(.9f,.53f),TextAlignmentOptions.TopLeft);
        feedback = AddLabel(window.transform,"",18,new Vector2(.1f,.17f),new Vector2(.9f,.25f));
        deliverButton = CreateButton(window.transform,"납품",new Vector2(.54f,.06f),new Vector2(.88f,.15f)); deliverButton.onClick.AddListener(Deliver);
        Button close = CreateButton(window.transform,"닫기",new Vector2(.12f,.06f),new Vector2(.46f,.15f)); close.onClick.AddListener(Close);
        modal.SetActive(false); openButtonObject.SetActive(false);
    }

    private static Slider CreateSlider(Transform parent)
    {
        GameObject root=Panel("Slider",parent,new Color(.18f,.23f,.33f));
        Slider s=root.AddComponent<Slider>();
        GameObject fill=Panel("Fill",root.transform,new Color(.28f,.65f,.9f));
        Stretch(fill.GetComponent<RectTransform>(),new Vector2(0,.2f),new Vector2(1,.8f));
        GameObject handle=Panel("Handle",root.transform,new Color(.95f,.95f,1f));
        RectTransform handleRect=handle.GetComponent<RectTransform>();
        handleRect.sizeDelta=new Vector2(24,24);
        s.fillRect=fill.GetComponent<RectTransform>();
        s.handleRect=handleRect;
        s.targetGraphic=handle.GetComponent<Image>();
        s.direction=Slider.Direction.LeftToRight;
        s.minValue=0;s.maxValue=1;s.wholeNumbers=true;
        return s;
    }

    private static Button CreateButton(Transform parent,string text,Vector2 min,Vector2 max){GameObject o=Panel(text,parent,new Color(.25f,.46f,.72f));Stretch(o.GetComponent<RectTransform>(),min,max);Button b=o.AddComponent<Button>();AddLabel(o.transform,text,22);return b;}
    private static GameObject Panel(string name,Transform parent,Color color){GameObject o=new(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);o.GetComponent<Image>().color=color;return o;}
    private static TMP_Text AddLabel(Transform parent,string value,float size,Vector2? min=null,Vector2? max=null,TextAlignmentOptions alignment=TextAlignmentOptions.Center){GameObject o=new("Label",typeof(RectTransform),typeof(TextMeshProUGUI));o.transform.SetParent(parent,false);TMP_Text t=o.GetComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.color=Color.white;t.alignment=alignment;Stretch(t.rectTransform,min??Vector2.zero,max??Vector2.one);return t;}
    private static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
    private static void EnsureEventSystem(){if(FindAnyObjectByType<EventSystem>()==null)DontDestroyOnLoad(new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)));}
}
