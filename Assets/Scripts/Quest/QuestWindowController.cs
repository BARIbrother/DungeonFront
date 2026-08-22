using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Prepare 단계 의뢰 수락·Settlement 단계 수락·상시 의뢰 납품을 처리하는 의뢰창.
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

    private TMP_Text detailTitleText;
    private TMP_Text detailBodyText;
    private TMP_Text detailRequireText;
    private TMP_Text detailRewardsText;
    private Transform detailRequireSlotsRoot;
    private Transform detailRewardSlotsRoot;

    private GameSessionState session;
    private Quest selectedQuest;
    private bool layoutReady;
    private Transform closeButtonTransform;
    private PerpetualQuestService perpetualService;
    private readonly List<Quest> perpetualQuests = new();
    private Slider deliverMultiplierSlider;
    private TMP_Text deliverMultiplierLabel;
    private GameObject deliverMultiplierRow;
    private PlayerInventory boundInventory;

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

        ApplyPanelFrame();
    }

    // LightFantasy 패널 프레임을 의뢰창에 적용한다.
    private void ApplyPanelFrame()
    {
        UiPanelFrame.ApplyTo(orderWindowPanel);
        EnlargeQuestOpenButton();
        UiButtonStyle.ApplyInChildren(orderWindowPanel);
        UiButtonStyle.Apply(questOpenButton != null ? questOpenButton.GetComponent<Button>() : null);
        if (acceptButton != null)
        {
            UiButtonStyle.Apply(acceptButton);
        }

        TmpUiStyle.ApplyToHierarchy(orderWindowPanel);
        if (questOpenButton != null)
        {
            TmpUiStyle.ApplyToHierarchy(questOpenButton);
        }
    }

    // 의뢰 열기 버튼을 키워 글씨가 잘리지 않게 한다.
    private void EnlargeQuestOpenButton()
    {
        if (questOpenButton == null)
        {
            return;
        }

        RectTransform rect = questOpenButton.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.sizeDelta = new Vector2(168f, 56f);
        TechTreeUI.AlignHudOpenButtons();
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

        BindInventory();
    }

    private void BindInventory()
    {
        PlayerInventory inventory = PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>();
        if (inventory == boundInventory)
        {
            return;
        }

        if (boundInventory != null)
        {
            boundInventory.OnItemsChanged -= HandleInventoryChanged;
        }

        boundInventory = inventory;
        if (boundInventory != null)
        {
            boundInventory.OnItemsChanged += HandleInventoryChanged;
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

        if (boundInventory != null)
        {
            boundInventory.OnItemsChanged -= HandleInventoryChanged;
            boundInventory = null;
        }
    }

    // 런타임 생성 UI에서 참조를 연결할 때 사용한다.
    public void Bind(GameObject openButton, GameObject orderWindow)
    {
        questOpenButton = openButton;
        orderWindowPanel = orderWindow;
        layoutReady = false;
        ApplyPanelFrame();
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

        if (!TutorialActionLock.Allows(TutorialActionLock.Action.OpenQuest))
        {
            return;
        }

        if (GameSessionState.Instance != null
            && GameSessionState.Instance.Phase != GamePhase.Prepare
            && GameSessionState.Instance.Phase != GamePhase.Settlement)
        {
            Debug.LogWarning("[QuestUI] Prepare 또는 Settlement 단계에서만 의뢰창을 열 수 있습니다.");
            return;
        }

        EnsureLayout();
        ApplyPanelFrame();
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
        if (IsOpen && !TutorialActionLock.Allows(TutorialActionLock.Action.CloseQuest))
        {
            return;
        }

        if (orderWindowPanel != null)
        {
            orderWindowPanel.SetActive(false);
        }

        selectedQuest = null;
        ClearPerpetualCopies();
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
        if (phase != GamePhase.Prepare && phase != GamePhase.Settlement)
        {
            CloseQuestWindow();
            return;
        }

        if (phase == GamePhase.Settlement)
        {
            StartCoroutine(OpenQuestWindowAfterSettlement());
            return;
        }

        if (orderWindowPanel != null && orderWindowPanel.activeInHierarchy)
        {
            RefreshAvailableAndList();
        }
    }

    private IEnumerator OpenQuestWindowAfterSettlement()
    {
        yield return null;
        OpenQuestWindow();
    }

    private void HandleInventoryChanged()
    {
        if (orderWindowPanel == null || !orderWindowPanel.activeInHierarchy || !IsSettlementPhase())
        {
            return;
        }

        if (selectedQuest == null)
        {
            return;
        }

        if (IsPerpetualQuest(selectedQuest))
        {
            RefreshPerpetualDetail(selectedQuest);
            return;
        }

        if (IsAcceptedQuest(selectedQuest))
        {
            RefreshAcceptedDetail(selectedQuest);
        }
    }

    private bool IsSettlementPhase()
    {
        return session != null && session.Phase == GamePhase.Settlement;
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
        if (IsSettlementPhase())
        {
            RebuildPerpetualList();
        }
        else
        {
            questPool?.MakeAvailableQuestsToday(CurrentReputation);
        }

        RefreshList();
    }

    private void RebuildPerpetualList()
    {
        ClearPerpetualCopies();
        perpetualService ??= FindAnyObjectByType<PerpetualQuestService>();
        if (questPool == null)
        {
            return;
        }

        perpetualQuests.AddRange(questPool.CreatePerpetualQuestList(CurrentReputation));
    }

    private void ClearPerpetualCopies()
    {
        foreach (Quest quest in perpetualQuests)
        {
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeRegistry.Forget(quest);
            Destroy(quest);
        }

        perpetualQuests.Clear();
    }

    private void RefreshList()
    {
        if (questManager == null || listContent == null)
        {
            return;
        }

        ClearListEntries();

        if (IsSettlementPhase())
        {
            RefreshSettlementList();
            return;
        }

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
            // 섹션 패널 상세를 쓰므로 카드 프리팹 UI는 숨긴다.
            detailCard.gameObject.SetActive(false);
        }

        ApplyDetailSections(quest);

        if (detailContentText != null)
        {
            detailContentText.gameObject.SetActive(false);
        }

        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(true);
            acceptButton.onClick.RemoveAllListeners();
            TMP_Text label = acceptButton.GetComponentInChildren<TMP_Text>();

            if (IsSettlementPhase() && IsPerpetualQuest(quest))
            {
                EnsureDeliverMultiplierControls();
                ConfigureDeliverMultiplier(quest);
                SetDeliverMultiplierRowVisible(true);

                acceptButton.interactable = GetPerpetualMaxMultiplier(quest) > 0;
                acceptButton.onClick.AddListener(TryDeliverPerpetual);
                if (label != null)
                {
                    label.text = "납품";
                }

                RefreshPerpetualDetail(quest);
            }
            else if (IsSettlementPhase() && IsAcceptedQuest(quest))
            {
                SetDeliverMultiplierRowVisible(false);

                acceptButton.interactable = questManager.CanCompleteQuest(quest);
                acceptButton.onClick.AddListener(TryDeliverAccepted);
                if (label != null)
                {
                    label.text = "납품";
                }

                RefreshAcceptedDetail(quest);
            }
            else
            {
                SetDeliverMultiplierRowVisible(false);

                acceptButton.interactable = CanAccept(quest);
                acceptButton.onClick.AddListener(TryAcceptSelected);
                if (label != null)
                {
                    label.text = "수락";
                }
            }
        }
    }

    private void RefreshSettlementList()
    {
        Quest firstQuest = null;
        bool selectionStillAvailable = false;

        foreach (Quest quest in questManager.currentQuests)
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

        foreach (Quest quest in perpetualQuests)
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

    private bool IsAcceptedQuest(Quest quest)
    {
        return quest != null
            && questManager != null
            && questManager.currentQuests.Contains(quest);
    }

    private void RefreshAcceptedDetail(Quest quest)
    {
        if (!IsSettlementPhase() || !IsAcceptedQuest(quest))
        {
            return;
        }

        if (detailRequireText != null)
        {
            detailRequireText.text = BuildAcceptedRequirementText(quest);
        }

        if (acceptButton != null)
        {
            acceptButton.interactable = questManager.CanCompleteQuest(quest);
        }
    }

    private static string BuildAcceptedRequirementText(Quest quest)
    {
        if (quest == null)
        {
            return "-";
        }

        var builder = new StringBuilder();
        builder.AppendLine(QuestCard.FormatDeadline(quest));
        builder.AppendLine("필요:");
        foreach (ItemEntry entry in quest.requiredItems?.entries ?? System.Array.Empty<ItemEntry>())
        {
            if (entry?.item == null)
            {
                continue;
            }

            int owned = PlayerInventory.Instance != null
                ? PlayerInventory.Instance.GetCount(entry.item.Id)
                : 0;
            string itemName = string.IsNullOrWhiteSpace(entry.item.DisplayName)
                ? entry.item.Id
                : entry.item.DisplayName;
            builder.AppendLine($"{itemName}  보유 {owned} / 제출 {entry.count}");
        }

        return builder.ToString().TrimEnd();
    }

    private void TryDeliverAccepted()
    {
        if (selectedQuest == null || questManager == null)
        {
            return;
        }

        Quest delivered = selectedQuest;
        if (!questManager.progressQuest(delivered))
        {
            Debug.LogWarning(
                $"[QuestUI] 의뢰 납품 실패: {delivered.title}",
                delivered);
            RefreshAcceptedDetail(delivered);
            return;
        }

        selectedQuest = null;
        RefreshAvailableAndList();
    }

    private bool IsPerpetualQuest(Quest quest)
    {
        return quest != null && perpetualQuests.Contains(quest);
    }

    private int GetPerpetualMaxMultiplier(Quest quest)
    {
        perpetualService ??= FindAnyObjectByType<PerpetualQuestService>();
        return quest != null && perpetualService != null
            ? perpetualService.GetMaxMultiplier(quest)
            : 0;
    }

    private void ConfigureDeliverMultiplier(Quest quest)
    {
        if (deliverMultiplierSlider == null)
        {
            return;
        }

        int maximum = GetPerpetualMaxMultiplier(quest);
        deliverMultiplierSlider.wholeNumbers = true;
        deliverMultiplierSlider.minValue = maximum > 0 ? 1 : 0;
        deliverMultiplierSlider.maxValue = Mathf.Max(1, maximum);
        deliverMultiplierSlider.SetValueWithoutNotify(
            maximum > 0
                ? Mathf.Clamp(deliverMultiplierSlider.value, 1, maximum)
                : 0);
    }

    private void RefreshPerpetualDetail(Quest quest)
    {
        if (!IsSettlementPhase() || !IsPerpetualQuest(quest))
        {
            return;
        }

        ConfigureDeliverMultiplier(quest);
        int maximum = GetPerpetualMaxMultiplier(quest);
        int chosen = deliverMultiplierSlider != null
            ? Mathf.RoundToInt(deliverMultiplierSlider.value)
            : 0;

        if (deliverMultiplierLabel != null)
        {
            deliverMultiplierLabel.text = maximum > 0
                ? $"납품 배수 x{chosen} / 최대 x{maximum}"
                : "납품 가능한 재고가 없습니다.";
        }

        if (detailRequireText != null)
        {
            detailRequireText.text = BuildPerpetualRequirementText(quest, chosen, maximum);
        }

        if (acceptButton != null)
        {
            acceptButton.interactable = maximum > 0;
        }
    }

    private static string BuildPerpetualRequirementText(Quest quest, int amount, int maximum)
    {
        if (quest == null)
        {
            return "-";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"상시 의뢰 · 선택 x{amount} / 가능한 최대 x{maximum}");
        builder.AppendLine("필요:");
        foreach (ItemEntry entry in quest.requiredItems?.entries ?? System.Array.Empty<ItemEntry>())
        {
            if (entry?.item == null)
            {
                continue;
            }

            int owned = PlayerInventory.Instance != null
                ? PlayerInventory.Instance.GetCount(entry.item.Id)
                : 0;
            string itemName = string.IsNullOrWhiteSpace(entry.item.DisplayName)
                ? entry.item.Id
                : entry.item.DisplayName;
            builder.AppendLine($"{itemName}  보유 {owned} / 제출 {entry.count * amount}");
        }

        return builder.ToString().TrimEnd();
    }

    private void TryDeliverPerpetual()
    {
        if (selectedQuest == null)
        {
            return;
        }

        perpetualService ??= FindAnyObjectByType<PerpetualQuestService>();
        int amount = deliverMultiplierSlider != null
            ? Mathf.RoundToInt(deliverMultiplierSlider.value)
            : 0;
        if (perpetualService == null || !perpetualService.TryDeliver(selectedQuest, amount))
        {
            Debug.LogWarning(
                $"[QuestUI] 상시 의뢰 납품 실패: {selectedQuest.title}",
                selectedQuest);
        }

        RefreshPerpetualDetail(selectedQuest);
    }

    private void EnsureDeliverMultiplierControls()
    {
        if (deliverMultiplierSlider != null || detailRoot == null)
        {
            return;
        }

        GameObject rowObject = new GameObject(
            "DeliverMultiplierRow",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(VerticalLayoutGroup));
        rowObject.transform.SetParent(detailRoot, false);

        LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.minHeight = 72f;
        rowLayout.preferredHeight = 72f;
        rowLayout.flexibleWidth = 1f;

        VerticalLayoutGroup rowGroup = rowObject.GetComponent<VerticalLayoutGroup>();
        rowGroup.spacing = 6f;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlHeight = true;
        rowGroup.childControlWidth = true;
        rowGroup.childForceExpandHeight = false;
        rowGroup.childForceExpandWidth = true;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(rowObject.transform, false);
        deliverMultiplierLabel = labelObject.GetComponent<TextMeshProUGUI>();
        deliverMultiplierLabel.text = "납품 배수";
        deliverMultiplierLabel.alignment = TextAlignmentOptions.Center;
        TmpUiStyle.Apply(deliverMultiplierLabel, TmpUiStyle.Role.Body);

        GameObject sliderObject = new GameObject(
            "Slider",
            typeof(RectTransform),
            typeof(Image),
            typeof(Slider),
            typeof(LayoutElement));
        sliderObject.transform.SetParent(rowObject.transform, false);
        LayoutElement sliderLayout = sliderObject.GetComponent<LayoutElement>();
        sliderLayout.minHeight = 28f;
        sliderLayout.preferredHeight = 28f;
        sliderLayout.flexibleWidth = 1f;

        Image sliderBackground = sliderObject.GetComponent<Image>();
        sliderBackground.color = new Color(0.18f, 0.23f, 0.33f, 1f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.2f);
        fillRect.anchorMax = new Vector2(1f, 0.8f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillObject.GetComponent<Image>().color = new Color(0.28f, 0.65f, 0.9f, 1f);

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = new Color(0.95f, 0.95f, 1f, 1f);

        deliverMultiplierSlider = sliderObject.GetComponent<Slider>();
        deliverMultiplierSlider.fillRect = fillRect;
        deliverMultiplierSlider.handleRect = handleRect;
        deliverMultiplierSlider.targetGraphic = handleImage;
        deliverMultiplierSlider.direction = Slider.Direction.LeftToRight;
        deliverMultiplierSlider.wholeNumbers = true;
        deliverMultiplierSlider.onValueChanged.AddListener(_ => RefreshPerpetualDetail(selectedQuest));

        deliverMultiplierRow = rowObject;
        rowObject.transform.SetSiblingIndex(detailRoot.childCount - 1);
        if (acceptButton != null)
        {
            rowObject.transform.SetSiblingIndex(acceptButton.transform.GetSiblingIndex());
        }

        rowObject.SetActive(false);
    }

    private void ClearDetail()
    {
        selectedQuest = null;
        if (detailCard != null)
        {
            detailCard.gameObject.SetActive(false);
        }

        if (detailTitleText != null)
        {
            detailTitleText.text = "받을 수 있는 의뢰가 없습니다.";
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = "-";
        }

        if (detailRequireText != null)
        {
            detailRequireText.text = "-";
        }

        QuestItemIconSlot.Clear(detailRequireSlotsRoot);
        QuestItemIconSlot.Clear(detailRewardSlotsRoot);

        if (detailRewardsText != null)
        {
            detailRewardsText.gameObject.SetActive(false);
        }

        if (detailContentText != null)
        {
            detailContentText.gameObject.SetActive(false);
        }

        if (acceptButton != null)
        {
            acceptButton.interactable = false;
        }

        if (deliverMultiplierRow != null)
        {
            deliverMultiplierRow.SetActive(false);
        }
    }

    private void SetDeliverMultiplierRowVisible(bool visible)
    {
        if (deliverMultiplierRow != null)
        {
            deliverMultiplierRow.SetActive(visible);
        }
    }

    private void ApplyDetailSections(Quest quest)
    {
        if (detailTitleText != null)
        {
            string title = string.IsNullOrWhiteSpace(quest.title) ? "-" : quest.title.Trim();
            string client = string.IsNullOrWhiteSpace(quest.clientName)
                ? ""
                : quest.clientName.Trim();
            detailTitleText.text = string.IsNullOrEmpty(client)
                ? title
                : $"{title}\n<size=70%>수취인: {client}</size>";
        }

        if (detailBodyText != null)
        {
            detailBodyText.text = string.IsNullOrWhiteSpace(quest.content)
                ? "-"
                : quest.content.Trim();
            detailBodyText.ForceMeshUpdate();
        }

        if (detailRequireText != null)
        {
            detailRequireText.text = QuestCard.FormatDeadline(quest);
        }

        QuestItemIconSlot.Populate(detailRequireSlotsRoot, quest.requiredItems);
        QuestItemIconSlot.PopulateRewards(detailRewardSlotsRoot, quest);

        if (detailRewardsText != null)
        {
            detailRewardsText.gameObject.SetActive(false);
        }
    }

    private void TryAcceptSelected()
    {
        if (selectedQuest == null || questManager == null)
        {
            return;
        }

        if (!TutorialActionLock.Allows(TutorialActionLock.Action.AcceptQuest))
        {
            return;
        }

        if (TutorialActionLock.IsRestricting)
        {
            bool isMandatory = QuestRuntimeRegistry.Get(selectedQuest)?.isMandatory ?? false;
            if (!isMandatory)
            {
                return;
            }
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
        // 상단은 Close 버튼 영역을 비워 두고, 장식 테두리 inset을 확보한다.
        const float topInset = 64f;
        const float sideInset = 36f;
        if (listContent == null || !layoutReady)
        {
            listContent = FindOrCreateChild(
                panelRect,
                "QuestListContent",
                new Vector2(0f, 0f),
                new Vector2(0.42f, 1f),
                new Vector2(sideInset, sideInset),
                new Vector2(-6f, -topInset));
        }

        // 가로로 채운 뒤 다음 줄로 넘어가는 그리드.
        VerticalLayoutGroup legacyVertical = listContent.GetComponent<VerticalLayoutGroup>();
        if (legacyVertical != null)
        {
            UnityEngine.Object.DestroyImmediate(legacyVertical);
        }

        GridLayoutGroup grid = listContent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = listContent.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = new Vector2(
            QuestListEntry.CellWidth,
            QuestListEntry.CellHeight);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(2, 2, 2, 2);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        ContentSizeFitter fitter = listContent.gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (detailRoot == null || !layoutReady)
        {
            detailRoot = FindOrCreateChild(
                panelRect,
                "QuestDetailRoot",
                new Vector2(0.42f, 0f),
                new Vector2(1f, 1f),
                new Vector2(10f, sideInset),
                new Vector2(-sideInset, -topInset));
        }

        EnsureDetailSections();

        // QuestCard 프리팹은 목록형 레이아웃이라, 상세는 섹션 패널+수락 버튼을 기본으로 쓴다.
        // 씬에 detailCard를 직접 연결한 경우에만 카드 UI를 사용한다.

        if (acceptButton == null)
        {
            acceptButton = CreateAcceptButton(detailRoot);
        }
        else
        {
            ConfigureAcceptButtonLayout(acceptButton);
            acceptButton.transform.SetParent(detailRoot, false);
        }

        acceptButton.transform.SetAsLastSibling();

        BringCloseButtonToFront();
        layoutReady = true;
    }

    // 상세를 이름·수취인·내용·보상 패널로 나눈다.
    private void EnsureDetailSections()
    {
        if (detailRoot == null)
        {
            return;
        }

        UiPanelFrame.ClearCache();

        // 바깥 통짜 패널 프레임은 제거하고 섹션만 패널로 쓴다.
        Image detailRootImage = detailRoot.GetComponent<Image>();
        if (detailRootImage != null)
        {
            detailRootImage.enabled = false;
            detailRootImage.raycastTarget = false;
        }

        VerticalLayoutGroup layout = detailRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = detailRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = 10f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        // 제목(+수취인 작은 글씨) · 내용 패널(요구·보상 내부 얇은 테두리)
        detailTitleText = EnsureDetailSection(
            detailRoot,
            "DetailTitlePanel",
            headerLabel: null,
            preferredHeight: 96f,
            TmpUiStyle.Role.Title,
            UiPanelFrame.Kind.BannerCream,
            lightBackground: true,
            bodyAlignment: TextAlignmentOptions.Center,
            scrollableBody: false,
            out _);

        // 수취인 전용 탭은 쓰지 않는다.
        HideLegacyDetailChild("DetailClientPanel");

        detailBodyText = null;
        detailRequireText = null;
        detailRewardsText = null;
        EnsureCombinedBodyPanel(out LayoutElement bodyLayout);
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.minHeight = 320f;
        bodyLayout.preferredHeight = 400f;

        // 예전 분리 패널은 숨긴다.
        HideLegacyDetailChild("DetailContentPanel");
        HideLegacyDetailChild("DetailRequirePanel");
        HideLegacyDetailChild("DetailRewardsPanel");

        // 섹션 순서를 고정한다.
        SetDetailSibling("DetailTitlePanel", 0);
        SetDetailSibling("DetailBodyPanel", 1);

        if (detailContentText != null)
        {
            detailContentText.gameObject.SetActive(false);
        }

        Transform legacySummary = orderWindowPanel != null
            ? orderWindowPanel.transform.Find("QuestSummaryText")
            : null;
        if (legacySummary != null)
        {
            legacySummary.gameObject.SetActive(false);
        }
    }

    private void HideLegacyDetailChild(string name)
    {
        if (detailRoot == null)
        {
            return;
        }

        Transform child = detailRoot.Find(name);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    // 내용(스크롤) · 요구 · 보상을 한 프레임 안에 세로로 쌓아 겹치지 않게 한다.
    private void EnsureCombinedBodyPanel(out LayoutElement layoutElement)
    {
        Transform existing = detailRoot.Find("DetailBodyPanel");
        GameObject rootObject;
        if (existing != null)
        {
            rootObject = existing.gameObject;
            rootObject.SetActive(true);
        }
        else
        {
            rootObject = new GameObject(
                "DetailBodyPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            rootObject.transform.SetParent(detailRoot, false);
        }

        layoutElement = rootObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rootObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = 320f;
        layoutElement.preferredHeight = 400f;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 1f;

        Image panelImage = rootObject.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = rootObject.AddComponent<Image>();
        }

        UiPanelFrame.Apply(panelImage, UiPanelFrame.Kind.Content, 0.6f);
        panelImage.raycastTarget = false;

        VerticalLayoutGroup sectionLayout = rootObject.GetComponent<VerticalLayoutGroup>();
        if (sectionLayout == null)
        {
            sectionLayout = rootObject.AddComponent<VerticalLayoutGroup>();
        }

        // 바깥 내용 프레임과 안쪽 텍스트/블록 사이 margin. 위·좌우를 더 확보한다.
        sectionLayout.padding = new RectOffset(36, 36, 40, 24);
        sectionLayout.spacing = 12f;
        sectionLayout.childAlignment = TextAnchor.UpperCenter;
        sectionLayout.childControlHeight = true;
        sectionLayout.childControlWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.childForceExpandWidth = true;

        // '내용' 라벨은 쓰지 않는다.
        Transform contentHeaderTransform = rootObject.transform.Find("ContentHeader");
        if (contentHeaderTransform != null)
        {
            contentHeaderTransform.gameObject.SetActive(false);
        }

        detailBodyText = EnsureScrollableBody(
            rootObject.transform,
            TmpUiStyle.Role.Body,
            preferredHeight: 120f);
        detailBodyText.alignment = TextAlignmentOptions.TopLeft;
        detailBodyText.textWrappingMode = TextWrappingModes.Normal;
        detailBodyText.overflowMode = TextOverflowModes.Overflow;

        Transform scroll = rootObject.transform.Find("Scroll");
        if (scroll != null)
        {
            LayoutElement scrollLayout = scroll.GetComponent<LayoutElement>();
            if (scrollLayout != null)
            {
                scrollLayout.flexibleHeight = 1f;
                scrollLayout.minHeight = 100f;
                scrollLayout.preferredHeight = 150f;
            }
        }

        // 요구·보상은 내용 프레임 안쪽 얇은 테두리 + 아이콘 슬롯.
        detailRequireText = EnsureInlineBlock(
            rootObject.transform,
            "RequireBlock",
            "요구",
            preferredHeight: 110f,
            thinFrame: true,
            showItemSlots: true,
            includeDeadlineLine: true,
            out detailRequireSlotsRoot);
        detailRewardsText = EnsureInlineBlock(
            rootObject.transform,
            "RewardsBlock",
            "보상",
            preferredHeight: 96f,
            thinFrame: true,
            showItemSlots: true,
            includeDeadlineLine: false,
            out detailRewardSlotsRoot);
        if (detailRewardsText != null)
        {
            // 보상은 납기 텍스트가 필요 없어 숨긴다.
            detailRewardsText.gameObject.SetActive(false);
        }

        // 블록 순서: 스크롤 → 요구 → 보상
        if (scroll != null)
        {
            scroll.SetSiblingIndex(0);
        }

        Transform requireBlock = rootObject.transform.Find("RequireBlock");
        Transform rewardsBlock = rootObject.transform.Find("RewardsBlock");
        if (requireBlock != null)
        {
            requireBlock.SetSiblingIndex(1);
        }

        if (rewardsBlock != null)
        {
            rewardsBlock.SetSiblingIndex(2);
        }
    }

    private static float ComputeInlineBlockHeight(bool thinFrame, bool includeDeadlineLine)
    {
        int margin = thinFrame ? 20 : 8;
        const float headerHeight = 20f;
        const float deadlineHeight = 18f;
        const float rowSpacing = 6f;

        float height = margin * 2f + headerHeight + rowSpacing + QuestItemIconSlot.SlotSize;
        if (includeDeadlineLine)
        {
            height += deadlineHeight + rowSpacing;
        }

        return height;
    }

    private static TMP_Text EnsureInlineBlock(
        Transform parent,
        string rootName,
        string headerLabel,
        float preferredHeight,
        bool thinFrame,
        bool showItemSlots,
        bool includeDeadlineLine,
        out Transform slotsRoot)
    {
        if (showItemSlots)
        {
            preferredHeight = ComputeInlineBlockHeight(thinFrame, includeDeadlineLine);
        }
        Transform existing = parent.Find(rootName);
        GameObject rootObject;
        if (existing != null)
        {
            rootObject = existing.gameObject;
        }
        else
        {
            rootObject = new GameObject(
                rootName,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            rootObject.transform.SetParent(parent, false);
        }

        LayoutElement layoutElement = rootObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rootObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;
        layoutElement.flexibleWidth = 1f;

        Image frameImage = rootObject.GetComponent<Image>();
        if (frameImage == null)
        {
            frameImage = rootObject.AddComponent<Image>();
        }

        if (thinFrame)
        {
            UiPanelFrame.Apply(frameImage, UiPanelFrame.Kind.Bar, 1.35f);
            frameImage.raycastTarget = false;
        }

        VerticalLayoutGroup layout = rootObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = rootObject.AddComponent<VerticalLayoutGroup>();
        }

        int margin = thinFrame ? 20 : 8;
        layout.padding = new RectOffset(margin, margin, margin, margin);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        TMP_Text header = EnsureSectionLabel(
            rootObject.transform,
            "Header",
            headerLabel,
            TmpUiStyle.Role.Caption,
            preferredHeight: 20f,
            flexibleHeight: 0f);
        header.alignment = TextAlignmentOptions.Left;
        header.overflowMode = TextOverflowModes.Overflow;

        // 기존 Body 텍스트: 요구는 납기 표시용, 보상은 숨김.
        TMP_Text body = EnsureSectionLabel(
            rootObject.transform,
            "Body",
            "-",
            TmpUiStyle.Role.Caption,
            preferredHeight: showItemSlots ? 18f : Mathf.Max(28f, preferredHeight - 24f - (margin * 2)),
            flexibleHeight: showItemSlots ? 0f : 1f);
        body.alignment = TextAlignmentOptions.Left;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;

        slotsRoot = null;
        if (showItemSlots)
        {
            Transform slotsTransform = rootObject.transform.Find("Slots");
            GameObject slotsObject;
            if (slotsTransform != null)
            {
                slotsObject = slotsTransform.gameObject;
            }
            else
            {
                slotsObject = new GameObject(
                    "Slots",
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(HorizontalLayoutGroup));
                slotsObject.transform.SetParent(rootObject.transform, false);
            }

            LayoutElement slotsLayout = slotsObject.GetComponent<LayoutElement>();
            if (slotsLayout == null)
            {
                slotsLayout = slotsObject.AddComponent<LayoutElement>();
            }

            slotsLayout.minHeight = QuestItemIconSlot.SlotSize;
            slotsLayout.preferredHeight = QuestItemIconSlot.SlotSize;
            slotsLayout.flexibleWidth = 1f;
            slotsLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup hlg = slotsObject.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = slotsObject.AddComponent<HorizontalLayoutGroup>();
            }

            hlg.spacing = 8f;
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            slotsRoot = slotsObject.transform;
            header.transform.SetSiblingIndex(0);
            body.transform.SetSiblingIndex(1);
            slotsObject.transform.SetSiblingIndex(2);
        }

        return body;
    }

    private void SetDetailSibling(string name, int index)
    {
        if (detailRoot == null)
        {
            return;
        }

        Transform child = detailRoot.Find(name);
        if (child != null)
        {
            child.SetSiblingIndex(index);
        }
    }

    private static TMP_Text EnsureDetailSection(
        Transform parent,
        string rootName,
        string headerLabel,
        float preferredHeight,
        TmpUiStyle.Role bodyRole,
        UiPanelFrame.Kind frameKind,
        bool lightBackground,
        TextAlignmentOptions bodyAlignment,
        bool scrollableBody,
        out LayoutElement layoutElement)
    {
        Transform existing = parent.Find(rootName);
        GameObject rootObject;
        if (existing != null)
        {
            rootObject = existing.gameObject;
        }
        else
        {
            rootObject = new GameObject(
                rootName,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            rootObject.transform.SetParent(parent, false);
        }

        layoutElement = rootObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rootObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 0f;

        Image panelImage = rootObject.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = rootObject.AddComponent<Image>();
        }

        float ppu = frameKind == UiPanelFrame.Kind.BannerCream
            || frameKind == UiPanelFrame.Kind.BannerTan
            || frameKind == UiPanelFrame.Kind.Parchment
            ? 0.9f
            : 0.6f;
        UiPanelFrame.Apply(panelImage, frameKind, ppu);
        panelImage.raycastTarget = false;

        VerticalLayoutGroup sectionLayout = rootObject.GetComponent<VerticalLayoutGroup>();
        if (sectionLayout == null)
        {
            sectionLayout = rootObject.AddComponent<VerticalLayoutGroup>();
        }

        bool hasHeader = !string.IsNullOrEmpty(headerLabel);
        int insetX = frameKind == UiPanelFrame.Kind.Parchment ? 16 : 22;
        int insetTop = hasHeader ? 16 : 12;
        int insetBottom = 12;
        sectionLayout.padding = new RectOffset(insetX, insetX, insetTop, insetBottom);
        sectionLayout.spacing = hasHeader ? 4f : 0f;
        sectionLayout.childAlignment = TextAnchor.UpperCenter;
        sectionLayout.childControlHeight = true;
        sectionLayout.childControlWidth = true;
        sectionLayout.childForceExpandHeight = false;
        sectionLayout.childForceExpandWidth = true;

        Transform headerTransform = rootObject.transform.Find("Header");
        if (hasHeader)
        {
            TMP_Text header = EnsureSectionLabel(
                rootObject.transform,
                "Header",
                headerLabel,
                TmpUiStyle.Role.Caption,
                preferredHeight: 22f,
                flexibleHeight: 0f);
            header.gameObject.SetActive(true);
            header.alignment = TextAlignmentOptions.Left;
            header.overflowMode = TextOverflowModes.Overflow;
            if (lightBackground)
            {
                TmpUiStyle.ApplyOnLightPanel(header, TmpUiStyle.Role.Caption);
                header.color = new Color(0.28f, 0.22f, 0.16f, 0.95f);
            }
        }
        else if (headerTransform != null)
        {
            headerTransform.gameObject.SetActive(false);
        }

        TMP_Text body;
        if (scrollableBody)
        {
            body = EnsureScrollableBody(
                rootObject.transform,
                bodyRole,
                preferredHeight: Mathf.Max(48f, preferredHeight - (hasHeader ? 48f : 28f)));
        }
        else
        {
            body = EnsureSectionLabel(
                rootObject.transform,
                "Body",
                "-",
                bodyRole,
                preferredHeight: Mathf.Max(28f, preferredHeight - (hasHeader ? 48f : 24f)),
                flexibleHeight: 1f);
        }

        body.alignment = bodyAlignment;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        if (lightBackground)
        {
            TmpUiStyle.ApplyOnLightPanel(body, bodyRole);
        }

        return body;
    }

    private static TMP_Text EnsureScrollableBody(
        Transform sectionRoot,
        TmpUiStyle.Role bodyRole,
        float preferredHeight)
    {
        Transform scrollTransform = sectionRoot.Find("Scroll");
        GameObject scrollObject;
        if (scrollTransform != null)
        {
            scrollObject = scrollTransform.gameObject;
        }
        else
        {
            scrollObject = new GameObject(
                "Scroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.transform.SetParent(sectionRoot, false);
        }

        LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
        if (scrollLayout == null)
        {
            scrollLayout = scrollObject.AddComponent<LayoutElement>();
        }

        scrollLayout.minHeight = preferredHeight;
        scrollLayout.preferredHeight = preferredHeight;
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.flexibleWidth = 1f;

        Image scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.01f);
        scrollImage.raycastTarget = true;

        Transform viewportTransform = scrollObject.transform.Find("Viewport");
        GameObject viewportObject;
        if (viewportTransform != null)
        {
            viewportObject = viewportTransform.gameObject;
        }
        else
        {
            viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
        }

        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        // 위쪽 inset으로 테두리와 본문 텍스트가 겹치지 않게 한다.
        viewportRect.offsetMin = new Vector2(8f, 2f);
        viewportRect.offsetMax = new Vector2(-8f, -10f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        Mask mask = viewportObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        Transform contentTransform = viewportObject.transform.Find("Content");
        GameObject contentObject;
        if (contentTransform != null)
        {
            contentObject = contentTransform.gameObject;
        }
        else
        {
            contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);
        }

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        }

        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Transform legacyBody = sectionRoot.Find("Body");
        TMP_Text body;
        if (legacyBody != null && legacyBody.parent != contentObject.transform)
        {
            legacyBody.SetParent(contentObject.transform, false);
            body = legacyBody.GetComponent<TMP_Text>();
        }
        else
        {
            body = EnsureSectionLabel(
                contentObject.transform,
                "Body",
                "-",
                bodyRole,
                preferredHeight: 40f,
                flexibleHeight: 0f);
        }

        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = new Vector2(0f, 40f);

        ContentSizeFitter bodyFitter = body.GetComponent<ContentSizeFitter>();
        if (bodyFitter == null)
        {
            bodyFitter = body.gameObject.AddComponent<ContentSizeFitter>();
        }

        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement bodyLayout = body.GetComponent<LayoutElement>();
        if (bodyLayout != null)
        {
            bodyLayout.flexibleHeight = 0f;
            bodyLayout.minHeight = 40f;
            bodyLayout.preferredHeight = -1f;
        }

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        return body;
    }

    private static TMP_Text EnsureSectionLabel(
        Transform parent,
        string name,
        string text,
        TmpUiStyle.Role role,
        float preferredHeight,
        float flexibleHeight)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI label;
        GameObject labelObject;
        if (existing != null)
        {
            labelObject = existing.gameObject;
            label = existing.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMeshProUGUI>();
            }
        }
        else
        {
            labelObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = labelObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = flexibleHeight;
        layoutElement.flexibleWidth = 1f;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, preferredHeight);

        label.text = text;
        label.raycastTarget = false;
        TmpUiStyle.Apply(label, role);
        return label;
    }

    private void CacheCloseButton()
    {
        if (orderWindowPanel == null)
        {
            return;
        }

        if (closeButtonTransform == null)
        {
            Transform found = orderWindowPanel.transform.Find("QuestCloseButton");
            if (found != null)
            {
                closeButtonTransform = found;
            }
        }

        PlaceCloseButtonTopRight();
    }

    // 닫기 버튼을 의뢰 패널 우상단에 둔다.
    private void PlaceCloseButtonTopRight()
    {
        if (closeButtonTransform == null)
        {
            return;
        }

        RectTransform rect = closeButtonTransform as RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -14f);
        rect.sizeDelta = new Vector2(120f, 44f);
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
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Button button = buttonObject.GetComponent<Button>();
        ConfigureAcceptButtonLayout(button);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.55f, 0.35f, 1f);
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "수락";
        label.alignment = TextAlignmentOptions.Center;
        TmpUiStyle.Apply(label, TmpUiStyle.Role.Button);

        UiButtonStyle.Apply(button);
        return button;
    }

    private static void ConfigureAcceptButtonLayout(Button button)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, 52f);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = 52f;
        layoutElement.preferredHeight = 52f;
        layoutElement.flexibleWidth = 1f;
        layoutElement.flexibleHeight = 0f;
    }
}
