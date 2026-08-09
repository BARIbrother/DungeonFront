using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Prepare 단계 의뢰창. 오늘 받을 수 있는 의뢰 목록을 띄우고, 클릭 시 상세·수락을 처리한다.
public class QuestWindowController : MonoBehaviour
{
    [Header("[UI 오브젝트 참조]")]
    [Tooltip("화면에 항상(또는 Prepare 단계에) 떠 있을 '의뢰창 열기' 버튼")]
    [SerializeField] private GameObject questOpenButton;

    [Tooltip("눌렀을 때 켜질 크게 만든 의뢰창 패널 (OrderWindow)")]
    [SerializeField] private GameObject orderWindowPanel;

    [Header("[의뢰 데이터]")]
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestPool questPool;
    [SerializeField] private QuestCard questCardPrefab;
    [SerializeField] private GameObject questSystemRootPrefab;

    [Header("[목록·상세]")]
    [SerializeField] private Transform listContent;
    [SerializeField] private Transform detailRoot;
    [SerializeField] private QuestCard detailCard;
    [SerializeField] private TMP_Text detailContentText;
    [SerializeField] private Button acceptButton;

    private GameSessionState session;
    private Quest selectedQuest;
    private bool layoutReady;
    private Transform closeButtonTransform;

    public static QuestWindowController Instance { get; private set; }

    public bool IsOpen =>
        orderWindowPanel != null && orderWindowPanel.activeSelf;

    private int CurrentReputation
    {
        get
        {
            Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
            return economy != null
                ? economy.Reputation
                : GameSessionState.Instance != null
                    ? GameSessionState.Instance.reputation
                    : 0;
        }
    }

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (questOpenButton != null)
        {
            questOpenButton.SetActive(true);
        }

        if (orderWindowPanel != null)
        {
            orderWindowPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (questManager != null)
        {
            questManager.OnQuestsChanged += HandleQuestsChanged;
        }

        session = GameSessionState.Instance;
        if (session != null)
        {
            session.OnPhaseChanged += HandlePhaseChanged;
            session.OnNewGame += HandleNewGame;
        }
    }

    private void OnDisable()
    {
        if (questManager != null)
        {
            questManager.OnQuestsChanged -= HandleQuestsChanged;
        }

        if (session != null)
        {
            session.OnPhaseChanged -= HandlePhaseChanged;
            session.OnNewGame -= HandleNewGame;
        }
    }

    // 런타임 생성 UI에서 참조를 연결할 때 사용한다.
    public void Bind(GameObject openButton, GameObject orderWindow)
    {
        questOpenButton = openButton;
        orderWindowPanel = orderWindow;
        layoutReady = false;
    }

    public void Bind(
        GameObject openButton,
        GameObject orderWindow,
        QuestCard cardPrefab)
    {
        Bind(openButton, orderWindow);
        questCardPrefab = cardPrefab;
    }

    // QuestOpenButton: 의뢰창을 연다. 닫기는 Close 버튼으로 한다.
    public void OpenQuestWindow()
    {
        if (IsOpen)
        {
            return;
        }

        if (GameSessionState.Instance != null
            && GameSessionState.Instance.Phase != GamePhase.Prepare)
        {
            Debug.LogWarning("[QuestUI] Prepare 단계에서만 의뢰 목록을 열 수 있습니다.");
            return;
        }

        EnsureLayout();
        if (orderWindowPanel == null)
        {
            return;
        }

        orderWindowPanel.SetActive(true);
        BringCloseButtonToFront();
        RefreshAvailableAndList();
    }

    public void CloseQuestWindow()
    {
        if (orderWindowPanel != null)
        {
            orderWindowPanel.SetActive(false);
        }

        selectedQuest = null;
    }

    // 열려 있으면 닫고, 닫혀 있으면 연다.
    public void ToggleQuestWindow()
    {
        if (IsOpen)
        {
            CloseQuestWindow();
            return;
        }

        OpenQuestWindow();
    }

    // 구 씬 버튼 호환용. 새 목록 UI에서는 사용하지 않는다.
    public void OnToggleQuest(string questName)
    {
    }

    // 구 씬 버튼 호환용. 수락은 상세 패널의 수락 버튼으로 처리한다.
    public void OnConfirmSelection()
    {
        TryAcceptSelected();
    }

    private void HandleNewGame()
    {
        StartCoroutine(RefreshAfterNewGame());
    }

    private IEnumerator RefreshAfterNewGame()
    {
        yield return null;
        if (orderWindowPanel != null && orderWindowPanel.activeInHierarchy)
        {
            RefreshAvailableAndList();
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Prepare)
        {
            CloseQuestWindow();
            return;
        }

        if (orderWindowPanel != null && orderWindowPanel.activeInHierarchy)
        {
            RefreshAvailableAndList();
        }
    }

    private void HandleQuestsChanged()
    {
        if (orderWindowPanel != null && orderWindowPanel.activeInHierarchy)
        {
            RefreshList();
        }
    }

    private void ResolveReferences()
    {
        EnsureQuestSystem();

        QuestManager resolved = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
        if (resolved != questManager)
        {
            if (questManager != null)
            {
                questManager.OnQuestsChanged -= HandleQuestsChanged;
            }

            questManager = resolved;
            if (isActiveAndEnabled && questManager != null)
            {
                questManager.OnQuestsChanged += HandleQuestsChanged;
            }
        }

        questPool ??= FindAnyObjectByType<QuestPool>();
    }

    // Factory/Production에 QuestManager가 없으면 QuestSystemRoot를 띄운다.
    private void EnsureQuestSystem()
    {
        if (QuestManager.Instance != null || FindAnyObjectByType<QuestManager>() != null)
        {
            return;
        }

        if (questSystemRootPrefab == null)
        {
            Debug.LogWarning(
                "[QuestUI] QuestManager가 없고 questSystemRootPrefab도 비어 있습니다.",
                this);
            return;
        }

        Instantiate(questSystemRootPrefab);
    }

    private void RefreshAvailableAndList()
    {
        ResolveReferences();
        EnsureLayout();
        questPool?.MakeAvailableQuestsToday(CurrentReputation);
        RefreshList();
    }

    private void RefreshList()
    {
        if (questManager == null || listContent == null)
        {
            return;
        }

        ClearListEntries();

        Quest firstQuest = null;
        bool selectionStillAvailable = false;
        foreach (Quest quest in questManager.availableQuestsToday)
        {
            if (quest == null)
            {
                continue;
            }

            firstQuest ??= quest;
            if (selectedQuest == quest)
            {
                selectionStillAvailable = true;
            }

            QuestListEntry entry = QuestListEntry.Create(listContent);
            entry.Bind(quest, ShowDetail);
            entry.SetSelected(selectedQuest == quest);
        }

        if (!selectionStillAvailable)
        {
            selectedQuest = null;
            if (firstQuest != null)
            {
                ShowDetail(firstQuest);
            }
            else
            {
                ClearDetail();
            }
        }
        else
        {
            ShowDetail(selectedQuest);
        }
    }

    private void ShowDetail(Quest quest)
    {
        selectedQuest = quest;
        EnsureLayout();

        foreach (QuestListEntry entry in listContent.GetComponentsInChildren<QuestListEntry>(true))
        {
            entry.SetSelected(entry.BoundQuest == quest);
        }

        if (quest == null)
        {
            ClearDetail();
            return;
        }

        if (detailCard != null)
        {
            detailCard.gameObject.SetActive(true);
            detailCard.SetQuest(quest);
            detailCard.SetButtonLabel("수락");
            detailCard.SetAcceptAction(TryAcceptSelected);
            detailCard.SetAcceptButtonInteractable(CanAccept(quest));
        }

        if (detailContentText != null)
        {
            detailContentText.gameObject.SetActive(true);
            detailContentText.text = detailCard != null && !string.IsNullOrWhiteSpace(quest.content)
                ? quest.content
                : BuildDetailText(quest);
        }

        if (acceptButton != null)
        {
            // QuestCard에 수락 버튼이 있으면 중복 버튼을 숨긴다.
            bool useCardAccept = detailCard != null;
            acceptButton.gameObject.SetActive(!useCardAccept);
            if (!useCardAccept)
            {
                acceptButton.interactable = CanAccept(quest);
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(TryAcceptSelected);
                TMP_Text label = acceptButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = "수락";
                }
            }
        }
    }

    private void ClearDetail()
    {
        selectedQuest = null;
        if (detailCard != null)
        {
            detailCard.gameObject.SetActive(false);
        }

        if (detailContentText != null)
        {
            detailContentText.text = "받을 수 있는 의뢰가 없습니다.";
        }

        if (acceptButton != null)
        {
            acceptButton.interactable = false;
        }
    }

    private void TryAcceptSelected()
    {
        if (selectedQuest == null || questManager == null)
        {
            return;
        }

        if (!questManager.acceptQuest(selectedQuest))
        {
            Debug.LogWarning($"[QuestUI] 의뢰 수락 실패: {selectedQuest.title}", selectedQuest);
            RefreshList();
            return;
        }

        selectedQuest = null;
        RefreshList();
    }

    private bool CanAccept(Quest quest)
    {
        return questManager != null && questManager.CanAcceptQuest(quest);
    }

    private void ClearListEntries()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }
    }

    private static string BuildDetailText(Quest quest)
    {
        var builder = new StringBuilder();
        builder.AppendLine(quest.title);
        if (!string.IsNullOrWhiteSpace(quest.clientName))
        {
            builder.Append("의뢰인: ");
            builder.AppendLine(quest.clientName);
        }

        builder.AppendLine(QuestCard.FormatDeadline(quest));
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(quest.content))
        {
            builder.AppendLine(quest.content);
            builder.AppendLine();
        }

        builder.AppendLine("[요구]");
        builder.AppendLine(FormatItems(quest.requiredItems));
        builder.AppendLine();
        builder.AppendLine("[보상]");
        builder.Append(FormatItems(quest.rewards));
        return builder.ToString().TrimEnd();
    }

    private static string FormatItems(ItemEntryList list)
    {
        if (list?.entries == null || list.entries.Length == 0)
        {
            return "-";
        }

        var builder = new StringBuilder();
        foreach (ItemEntry entry in list.entries)
        {
            if (entry?.item == null)
            {
                continue;
            }

            builder.Append(string.IsNullOrWhiteSpace(entry.item.DisplayName)
                ? entry.item.Id
                : entry.item.DisplayName);
            builder.Append(" x");
            builder.Append(entry.count);
            builder.Append('\n');
        }

        string text = builder.ToString().TrimEnd();
        return string.IsNullOrEmpty(text) ? "-" : text;
    }

    // orderWindow 아래에 목록·상세 영역을 만들고, 구 QuestButton은 숨긴다.
    private void EnsureLayout()
    {
        if (orderWindowPanel == null)
        {
            return;
        }

        HideLegacyQuestButtons();
        CacheCloseButton();

        RectTransform panelRect = orderWindowPanel.GetComponent<RectTransform>();
        // 상단은 Close 버튼 영역을 비워 둔다.
        const float topInset = 48f;
        if (listContent == null || !layoutReady)
        {
            listContent = FindOrCreateChild(
                panelRect,
                "QuestListContent",
                new Vector2(0f, 0f),
                new Vector2(0.42f, 1f),
                new Vector2(12f, 12f),
                new Vector2(-6f, -topInset));
        }

        VerticalLayoutGroup layout = listContent.gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = listContent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = 6f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = listContent.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (detailRoot == null || !layoutReady)
        {
            detailRoot = FindOrCreateChild(
                panelRect,
                "QuestDetailRoot",
                new Vector2(0.42f, 0f),
                new Vector2(1f, 1f),
                new Vector2(6f, 12f),
                new Vector2(-12f, -topInset));
        }

        if (detailContentText == null)
        {
            Transform existingSummary = orderWindowPanel.transform.Find("QuestSummaryText");
            if (existingSummary != null)
            {
                detailContentText = existingSummary.GetComponent<TMP_Text>();
                existingSummary.SetParent(detailRoot, false);
                RectTransform summaryRect = existingSummary.GetComponent<RectTransform>();
                summaryRect.anchorMin = new Vector2(0f, 0.2f);
                summaryRect.anchorMax = new Vector2(1f, 1f);
                summaryRect.offsetMin = new Vector2(8f, 8f);
                summaryRect.offsetMax = new Vector2(-8f, -8f);
            }
        }

        if (detailContentText == null)
        {
            GameObject body = new GameObject("QuestDetailBody", typeof(RectTransform), typeof(TextMeshProUGUI));
            body.transform.SetParent(detailRoot, false);
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0.2f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(8f, 8f);
            bodyRect.offsetMax = new Vector2(-8f, -8f);
            detailContentText = body.GetComponent<TextMeshProUGUI>();
            detailContentText.fontSize = 18f;
            detailContentText.color = Color.white;
            detailContentText.alignment = TextAlignmentOptions.TopLeft;
            detailContentText.textWrappingMode = TextWrappingModes.Normal;
        }

        // 상세 텍스트가 Close 버튼 클릭을 가로채지 않게 한다.
        if (detailContentText != null)
        {
            detailContentText.raycastTarget = false;
        }

        // QuestCard 프리팹은 목록형 레이아웃이라, 상세는 텍스트+수락 버튼을 기본으로 쓴다.
        // 씬에 detailCard를 직접 연결한 경우에만 카드 UI를 사용한다.

        if (acceptButton == null)
        {
            acceptButton = CreateAcceptButton(detailRoot);
        }

        BringCloseButtonToFront();
        layoutReady = true;
    }

    private void CacheCloseButton()
    {
        if (closeButtonTransform != null || orderWindowPanel == null)
        {
            return;
        }

        Transform found = orderWindowPanel.transform.Find("QuestCloseButton");
        if (found != null)
        {
            closeButtonTransform = found;
        }
    }

    private void BringCloseButtonToFront()
    {
        CacheCloseButton();
        if (closeButtonTransform != null)
        {
            closeButtonTransform.SetAsLastSibling();
        }
    }

    private void HideLegacyQuestButtons()
    {
        if (orderWindowPanel == null)
        {
            return;
        }

        foreach (Transform child in orderWindowPanel.transform)
        {
            if (child.name.StartsWith("QuestButton"))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static Transform FindOrCreateChild(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        if (existing != null)
        {
            rect = existing.GetComponent<RectTransform>();
        }
        else
        {
            GameObject created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            rect = created.GetComponent<RectTransform>();
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static Button CreateAcceptButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "QuestAcceptButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.55f, 0f);
        rect.anchorMax = new Vector2(1f, 0.18f);
        rect.offsetMin = new Vector2(8f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.55f, 0.35f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "수락";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.color = Color.white;

        return button;
    }
}
