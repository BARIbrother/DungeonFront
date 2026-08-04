using System;
using System.Collections.Generic;
using UnityEngine;

// Week 4의 "클리어 라인 lock"만 담당한다.
// QuestManager는 납품을 처리하고, 이 클래스는 무엇이 열리는지만 판단한다.
public class QuestProgressionService : MonoBehaviour
{
    public static QuestProgressionService Instance { get; private set; }

    [SerializeField] private QuestManager questManager;
    [SerializeField] private string endingStoryEventId = "001E99999";
    [SerializeField] private List<string> completedQuestIds = new();

    private readonly HashSet<string> completed =
        new(StringComparer.Ordinal);

    private bool endingRaised;
    private QuestManager boundManager;
    private GameSessionState boundSession;

    public event Action OnProgressionChanged;
    public event Action OnBackCaveCleared;

    public IReadOnlyCollection<string> CompletedQuestIds => completed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        completed.Clear();
        foreach (string questId in completedQuestIds)
        {
            if (!string.IsNullOrWhiteSpace(questId))
            {
                completed.Add(questId);
            }
        }
    }

    private void OnEnable()
    {
        BindManager();
        BindSession();
    }

    private void Start()
    {
        BindManager();
        BindSession();
    }

    private void OnDisable()
    {
        UnbindManager();
        UnbindSession();
    }

    private void OnDestroy()
    {
        UnbindManager();
        UnbindSession();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanOffer(string questId, string unlockAfterQuestId)
    {
        if (string.IsNullOrWhiteSpace(questId) || completed.Contains(questId))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(unlockAfterQuestId)
            || completed.Contains(unlockAfterQuestId);
    }

    public bool IsCompleted(string questId)
    {
        return !string.IsNullOrWhiteSpace(questId) && completed.Contains(questId);
    }

    public void Restore(IEnumerable<string> questIds, bool wasEndingRaised = false)
    {
        completed.Clear();
        completedQuestIds.Clear();

        if (questIds != null)
        {
            foreach (string questId in questIds)
            {
                if (!string.IsNullOrWhiteSpace(questId) && completed.Add(questId))
                {
                    completedQuestIds.Add(questId);
                }
            }
        }

        endingRaised = wasEndingRaised;
        OnProgressionChanged?.Invoke();
    }

    public void ResetProgression()
    {
        Restore(Array.Empty<string>());
    }

    private void BindManager()
    {
        QuestManager candidate = questManager != null
            ? questManager
            : QuestManager.Instance;
        candidate ??= FindAnyObjectByType<QuestManager>();

        if (candidate == boundManager)
        {
            return;
        }

        UnbindManager();
        boundManager = candidate;
        if (boundManager != null)
        {
            boundManager.OnQuestCompleted += HandleQuestCompleted;
        }
    }

    private void UnbindManager()
    {
        if (boundManager != null)
        {
            boundManager.OnQuestCompleted -= HandleQuestCompleted;
        }

        boundManager = null;
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
            boundSession.OnNewGame += ResetProgression;
        }
    }

    private void UnbindSession()
    {
        if (boundSession != null)
        {
            boundSession.OnNewGame -= ResetProgression;
        }

        boundSession = null;
    }

    private void HandleQuestCompleted(Quest quest)
    {
        QuestRuntimeInfo info = QuestRuntimeRegistry.Get(quest);
        string questId = QuestRuntimeRegistry.GetStableId(quest);
        if (quest == null || info == null || string.IsNullOrWhiteSpace(questId))
        {
            return;
        }

        if (completed.Add(questId))
        {
            completedQuestIds.Add(questId);
            OnProgressionChanged?.Invoke();
        }

        if (!info.isMainStoryQuest
            || !info.triggersBackCaveEnding
            || endingRaised)
        {
            return;
        }

        endingRaised = true;
        OnBackCaveCleared?.Invoke();
        StoryEventBus.Raise(endingStoryEventId);
        Debug.Log($"[Quest] 뒷산 메인 의뢰 완료 — 엔딩 이벤트 {endingStoryEventId}");
    }
}
