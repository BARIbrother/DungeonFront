using System;
using System.Collections.Generic;
using UnityEngine;

// 의뢰 수락·납품·완료를 관리한다.
public class QuestManager : MonoBehaviour
{
    public const int MaxActiveQuestCount = 3;
    public static QuestManager Instance { get; private set; }

    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private bool persistBetweenScenes = true;

    // 현재 수락하여 진행 중인 의뢰 목록
    public List<Quest> currentQuests = new();

    // 수락했던 의뢰 (완료포함)
    public List<int> acceptedQuestIds = new();

    // 오늘 받을 수 있는 의뢰 목록
    public List<Quest> availableQuestsToday = new();

    public event Action OnQuestsChanged;
    public event Action<Quest> OnQuestAccepted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest> OnQuestExpired;

    private GameSessionState boundSession;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // 같은 GameObject의 QuestPool까지 지우지 않도록 컴포넌트만 제거한다.
            Destroy(this);
            return;
        }

        Instance = this;
        if (persistBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        BindSession();
    }

    private void Start()
    {
        BindSession();
    }

    private void OnDisable()
    {
        UnbindSession();
    }

    private void OnDestroy()
    {
        UnbindSession();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // availableQuestsToday에서 의뢰를 수락해 currentQuests로 옮긴다.
    public bool acceptQuest(Quest quest)
    {
        if (!CanAcceptQuest(quest))
        {
            PlayCatalogSfx(audio => audio.Catalog.uiDeny);
            return false;
        }
        PlayCatalogSfx(audio => audio.Catalog.questAccept);

        int today = GameSessionState.Instance != null ? GameSessionState.Instance.day : 1;
        Quest acceptedQuest = CreateQuestInstance(quest, today);
        availableQuestsToday.Remove(quest);
        currentQuests.Add(acceptedQuest);
        QuestRuntimeRegistry.Forget(quest);
        Destroy(quest);
        if (int.TryParse(
                QuestRuntimeRegistry.GetStableId(acceptedQuest),
                out int acceptedId)
            && !acceptedQuestIds.Contains(acceptedId))
        {
            acceptedQuestIds.Add(acceptedId);
        }

        SyncAcceptedQuestToSession(acceptedQuest);
        OnQuestAccepted?.Invoke(acceptedQuest);
        OnQuestsChanged?.Invoke();
        return true;
    }

    public bool CanAcceptQuest(Quest quest)
    {
        return quest != null
            && !(QuestRuntimeRegistry.Get(quest)?.IsPerpetual ?? false)
            && availableQuestsToday.Contains(quest)
            && currentQuests.Count < MaxActiveQuestCount;
    }

    // 요구 품목을 전부 보유한 경우에만 한 번에 납품하고 완료 처리한다.
    // 일부 품목만 납품하는 것은 허용하지 않는다.
    public bool progressQuest(Quest quest)
    {
        if (quest == null || !currentQuests.Contains(quest))
        {
            return false;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (!CanCompleteQuest(quest, inventory))
        {
            return false;
        }

        foreach (ItemEntry entry in quest.requiredItems.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            inventory.Remove(entry.item, entry.count);
        }

        finishQuest(quest);
        return true;
    }

    // 의뢰를 완료 처리하고 currentQuests에서 제거한다.
    public void finishQuest(Quest quest)
    {
        if (quest == null || !currentQuests.Contains(quest))
        {
            return;
        }

        givePlayerReward(quest);
        currentQuests.Remove(quest);
        RemoveQuestFromSession(quest);
        OnQuestCompleted?.Invoke(quest);
        OnQuestsChanged?.Invoke();
        QuestRuntimeRegistry.Forget(quest);
        Destroy(quest);
    }

    private static void PlayCatalogSfx(Func<AudioManager, AudioCatalog.AudioEntry> selectClip)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || audio.Catalog == null || selectClip == null)
        {
            return;
        }

        AudioCatalog.AudioEntry entry = selectClip(audio);
        if (entry == audio.Catalog.uiDeny || entry == audio.Catalog.questAccept)
        {
            UiButtonSound.SuppressClickSoundForCurrentFrame();
        }

        audio.PlaySfx(entry);
    }

    // 의뢰 보상을 플레이어 인벤토리에 지급한다.
    public void givePlayerReward(Quest quest)
    {
        if (quest?.rewards?.entries == null)
        {
            return;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (inventory == null)
        {
            return;
        }

        foreach (ItemEntry entry in quest.rewards.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            if (!TryGiveCurrency(entry))
            {
                inventory.Add(entry);
            }
        }

        PlayCatalogSfx(audio => audio.Catalog.coin);
    }

    // SO 원본을 변경하지 않도록 런타임 전용 인스턴스를 만든다.
    public Quest CreateQuestInstance(Quest source, int acceptedDay)
    {
        if (source == null)
        {
            return null;
        }

        Quest instance = ScriptableObject.CreateInstance<Quest>();
        instance.id = source.id;
        instance.title = source.title;
        instance.clientName = source.clientName;
        instance.content = source.content;
        instance.deadlineDays = source.deadlineDays;
        instance.currentleftDeadlineDays = source.deadlineDays;
        instance.requiredItems = CloneItemEntryList(source.requiredItems);
        instance.rewards = CloneItemEntryList(source.rewards);
        QuestRuntimeInfo sourceInfo = QuestRuntimeRegistry.GetOrCreate(source);
        QuestRuntimeRegistry.Register(
            instance,
            sourceInfo.CloneForAcceptedDay(acceptedDay));
        return instance;
    }

    public void OnDayAdvanced(int elapsedDays = 1)
    {
        if (elapsedDays <= 0)
        {
            return;
        }

        foreach (Quest quest in currentQuests)
        {
            if (quest == null || quest.currentleftDeadlineDays <= 0)
            {
                continue;
            }

            quest.currentleftDeadlineDays = Mathf.Max(
                0,
                quest.currentleftDeadlineDays - elapsedDays);
        }

        OnQuestsChanged?.Invoke();
    }

    public void ExpireQuest(Quest quest)
    {
        if (quest == null || !currentQuests.Remove(quest))
        {
            return;
        }

        OnQuestExpired?.Invoke(quest);
        RemoveQuestFromSession(quest);
        OnQuestsChanged?.Invoke();
        QuestRuntimeRegistry.Forget(quest);
        Destroy(quest);
    }

    public void RestoreQuest(Quest quest)
    {
        if (quest == null)
        {
            return;
        }

        currentQuests.Add(quest);
        SyncAcceptedQuestToSession(quest);
        OnQuestsChanged?.Invoke();
    }

    public void NotifyQuestsChanged()
    {
        OnQuestsChanged?.Invoke();
    }

    public void ClearActive()
    {
        foreach (Quest quest in currentQuests)
        {
            if (quest != null)
            {
                RemoveQuestFromSession(quest);
                QuestRuntimeRegistry.Forget(quest);
                Destroy(quest);
            }
        }

        currentQuests.Clear();
        OnQuestsChanged?.Invoke();
    }

    // NewGame에서는 이전 플레이의 수락 이력과 오늘의 풀까지 함께 비운다.
    // Save Import는 진행 중 목록만 교체해야 하므로 기존 ClearActive를 사용한다.
    public void ClearAllQuestState()
    {
        ClearActive();
        DestroyQuestList(availableQuestsToday);
        availableQuestsToday.Clear();
        acceptedQuestIds.Clear();
        OnQuestsChanged?.Invoke();
    }

    // ItemEntryList를 복사해 수락한 의뢰가 원본 SO와 독립적으로 동작하게 한다.
    private static ItemEntryList CloneItemEntryList(ItemEntryList source)
    {
        if (source == null)
        {
            return null;
        }

        var clone = new ItemEntryList
        {
            length = source.length
        };

        if (source.entries == null || source.entries.Length == 0)
        {
            clone.entries = System.Array.Empty<ItemEntry>();
            return clone;
        }

        clone.entries = new ItemEntry[source.entries.Length];
        for (int i = 0; i < source.entries.Length; i++)
        {
            ItemEntry entry = source.entries[i];
            if (entry == null)
            {
                continue;
            }

            clone.entries[i] = new ItemEntry
            {
                item = entry.item != null ? entry.item.Clone() : null,
                count = entry.count
            };
        }

        return clone;
    }

    // 요구 품목 전체를 충분히 보유했는지 확인한다.
    private static bool HasAllRequiredItems(Quest quest, PlayerInventory inventory)
    {
        foreach (ItemEntry entry in quest.requiredItems.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.item.Id))
            {
                return false;
            }

            if (inventory.GetCount(entry.item) < entry.count)
            {
                return false;
            }
        }

        return true;
    }

    public bool CanCompleteQuest(Quest quest)
    {
        return CanCompleteQuest(quest, GetPlayerInventory());
    }

    private static bool CanCompleteQuest(Quest quest, PlayerInventory inventory)
    {
        return quest != null
            && inventory != null
            && quest.requiredItems?.entries != null
            && HasAllRequiredItems(quest, inventory);
    }

    private static bool TryGiveCurrency(ItemEntry entry)
    {
        if (entry?.item == null || GameSessionState.Instance == null)
        {
            return false;
        }

        if (string.Equals(entry.item.Id, "gold", StringComparison.OrdinalIgnoreCase))
        {
            GameSessionState.Instance.AddGold(entry.count);
            return true;
        }

        if (string.Equals(entry.item.Id, "fame", StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.item.Id, "reputation", StringComparison.OrdinalIgnoreCase))
        {
            GameSessionState.Instance.AddReputation(entry.count);
            return true;
        }

        return false;
    }

    private static void SyncAcceptedQuestToSession(Quest quest)
    {
        string stableId = QuestRuntimeRegistry.GetStableId(quest);
        if (GameSessionState.Instance == null
            || !int.TryParse(stableId, out int id)
            || GameSessionState.Instance.quests.Exists(saved => saved.questId == id))
        {
            return;
        }

        GameSessionState.Instance.TryAcceptQuest(id, quest.title, playAudio: false);
    }

    private static void RemoveQuestFromSession(Quest quest)
    {
        string stableId = QuestRuntimeRegistry.GetStableId(quest);
        if (GameSessionState.Instance != null
            && int.TryParse(stableId, out int id))
        {
            GameSessionState.Instance.RemoveQuest(id);
        }
    }

    private PlayerInventory GetPlayerInventory()
    {
        return playerInventory != null
            ? playerInventory
            : PlayerInventory.Instance != null
                ? PlayerInventory.Instance
                : FindAnyObjectByType<PlayerInventory>();
    }

    private void BindSession()
    {
        GameSessionState candidate = GameSessionState.Instance;
        candidate ??= FindAnyObjectByType<GameSessionState>();
        if (candidate == boundSession)
        {
            return;
        }

        UnbindSession();
        boundSession = candidate;
        if (boundSession != null)
        {
            boundSession.OnNewGame += ClearAllQuestState;
        }
    }

    private void UnbindSession()
    {
        if (boundSession != null)
        {
            boundSession.OnNewGame -= ClearAllQuestState;
        }

        boundSession = null;
    }

    private static void DestroyQuestList(IEnumerable<Quest> quests)
    {
        foreach (Quest quest in quests)
        {
            if (quest != null)
            {
                QuestRuntimeRegistry.Forget(quest);
                Destroy(quest);
            }
        }
    }
}
