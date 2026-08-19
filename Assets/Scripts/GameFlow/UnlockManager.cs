using System;
using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    public event Action OnUnlocksChanged;

    private readonly HashSet<string> unlockedNodeIds = new HashSet<string>();
    private QuestProgressionService boundProgression;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<UnlockManager>() != null)
        {
            return;
        }

        var managerObject = new GameObject("UnlockManager");
        managerObject.AddComponent<UnlockManager>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetUnlockedNodes();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        BindProgression();
    }

    private void Start()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnNewGame -= ResetUnlockedNodes;
            GameSessionState.Instance.OnNewGame += ResetUnlockedNodes;
        }

        BindProgression();
        GrantCompletedQuestTechs();
    }

    private void Update()
    {
        if (boundProgression == null)
        {
            BindProgression();
            if (boundProgression != null)
            {
                GrantCompletedQuestTechs();
            }
        }
    }

    private void OnDisable()
    {
        UnbindProgression();
    }

    private void OnDestroy()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnNewGame -= ResetUnlockedNodes;
        }

        UnbindProgression();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BindProgression()
    {
        QuestProgressionService candidate = QuestProgressionService.Instance
            ?? FindAnyObjectByType<QuestProgressionService>();
        if (candidate == boundProgression)
        {
            return;
        }

        UnbindProgression();
        boundProgression = candidate;
        if (boundProgression != null)
        {
            boundProgression.OnProgressionChanged += HandleProgressionChanged;
        }
    }

    private void UnbindProgression()
    {
        if (boundProgression != null)
        {
            boundProgression.OnProgressionChanged -= HandleProgressionChanged;
        }

        boundProgression = null;
    }

    private void HandleProgressionChanged()
    {
        GrantCompletedQuestTechs();
    }

    // Q002 완료 기록이 있으면 열 2·3 마나 테크를 지급한다.
    private void GrantCompletedQuestTechs()
    {
        BindProgression();
        QuestProgressionService progression = boundProgression
            ?? QuestProgressionService.Instance
            ?? FindAnyObjectByType<QuestProgressionService>();
        if (progression == null)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node node = TechTreeCatalog.All[i];
            if (string.IsNullOrEmpty(node.grantOnQuestId)
                || !progression.IsCompleted(node.grantOnQuestId))
            {
                continue;
            }

            if (unlockedNodeIds.Add(node.id))
            {
                changed = true;
            }
        }

        if (changed)
        {
            OnUnlocksChanged?.Invoke();
        }
    }

    public void ResetUnlockedNodes()
    {
        unlockedNodeIds.Clear();
        for (int i = 0; i < TechTreeCatalog.All.Length; i++)
        {
            TechTreeCatalog.Node node = TechTreeCatalog.All[i];
            if (node.startUnlocked)
            {
                unlockedNodeIds.Add(node.id);
            }
        }

        OnUnlocksChanged?.Invoke();
    }

    public void GrantUnlocked(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !unlockedNodeIds.Add(nodeId))
        {
            return;
        }

        OnUnlocksChanged?.Invoke();
    }

    public bool IsUnlocked(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return false;
        }

        if (unlockedNodeIds.Contains(nodeId))
        {
            return true;
        }

        // 시작 지급 테크는 해금 집합에 빠져 있어도 열린 것으로 본다.
        TechTreeCatalog.Node node = TechTreeCatalog.Get(nodeId);
        return node != null && node.startUnlocked;
    }

    public bool CanUnlock(string techId)
    {
        return CanUnlock(TechTreeCatalog.Get(techId));
    }

    public bool CanUnlock(TechTreeCatalog.Node node)
    {
        if (node == null || IsUnlocked(node.id))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(node.grantOnQuestId))
        {
            return false;
        }

        bool blocked = false;
        TechTreeCatalog.ForEachIncomingParent(node.id, parentId =>
        {
            TechTreeCatalog.Node parent = TechTreeCatalog.Get(parentId);
            if (parent != null && !parent.visibleInGame)
            {
                return;
            }

            if (!IsUnlocked(parentId))
            {
                blocked = true;
            }
        });

        return !blocked;
    }

    public bool CanUnlock(TechNodeSO node)
    {
        if (node == null)
        {
            return false;
        }

        TechTreeCatalog.Node catalogNode = TechTreeCatalog.Get(node.techId);
        if (catalogNode != null)
        {
            return CanUnlock(catalogNode);
        }

        if (IsUnlocked(node.techId))
        {
            return false;
        }

        return node.parentNode == null || IsUnlocked(node.parentNode.techId);
    }

    public bool TryUnlock(string techId, out string error)
    {
        error = null;
        TechTreeCatalog.Node node = TechTreeCatalog.Get(techId);
        if (node == null)
        {
            error = "없는 기술입니다.";
            return false;
        }

        if (IsUnlocked(node.id))
        {
            error = "이미 해금된 기술입니다.";
            return false;
        }

        if (!string.IsNullOrEmpty(node.grantOnQuestId))
        {
            error = "레이의 의뢰를 마치면 해금됩니다.";
            return false;
        }

        if (!CanUnlock(node))
        {
            error = FormatMissingParents(node);
            return false;
        }

        if (!TrySpendHonor(node.honor))
        {
            error = $"명예가 부족합니다. (필요 {node.honor})";
            return false;
        }

        unlockedNodeIds.Add(node.id);
        OnUnlocksChanged?.Invoke();
        return true;
    }

    public bool TryUnlock(TechNodeSO node, ref int currentGold, ref int currentReputation)
    {
        if (node == null)
        {
            return false;
        }

        TechTreeCatalog.Node catalogNode = TechTreeCatalog.Get(node.techId);
        if (catalogNode != null)
        {
            bool unlocked = TryUnlock(catalogNode.id, out _);
            if (unlocked)
            {
                currentReputation = GetHonor();
            }

            return unlocked;
        }

        if (IsUnlocked(node.techId) || !CanUnlock(node))
        {
            return false;
        }

        if (currentGold < node.requiredGold || currentReputation < node.requiredReputation)
        {
            return false;
        }

        currentGold -= node.requiredGold;
        currentReputation -= node.requiredReputation;
        unlockedNodeIds.Add(node.techId);
        OnUnlocksChanged?.Invoke();
        return true;
    }

    public int GetProductionMinutes()
    {
        if (IsUnlocked("fuel_2"))
        {
            return 5;
        }

        if (IsUnlocked("fuel_1"))
        {
            return 4;
        }

        return 3;
    }

    public int GetProductionTicks()
    {
        return GetProductionMinutes() * 600;
    }

    public float GetProductionSeconds()
    {
        return GetProductionMinutes() * 60f;
    }

    private static string FormatMissingParents(TechTreeCatalog.Node node)
    {
        var names = new List<string>();
        TechTreeCatalog.ForEachIncomingParent(node.id, parentId =>
        {
            if (Instance != null && Instance.IsUnlocked(parentId))
            {
                return;
            }

            TechTreeCatalog.Node parent = TechTreeCatalog.Get(parentId);
            if (parent != null && parent.visibleInGame)
            {
                names.Add(parent.name);
            }
        });

        if (names.Count == 0)
        {
            return "선행 기술을 먼저 해금해야 합니다.";
        }

        return $"{string.Join(", ", names)}을(를) 먼저 해금해야 합니다.";
    }

    private static bool TrySpendHonor(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
        if (economy != null)
        {
            return economy.TrySpendReputation(amount);
        }

        if (GameSessionState.Instance == null || GameSessionState.Instance.reputation < amount)
        {
            return false;
        }

        GameSessionState.Instance.AddReputation(-amount);
        return true;
    }

    private static int GetHonor()
    {
        Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
        if (economy != null)
        {
            return economy.Reputation;
        }

        return GameSessionState.Instance != null ? GameSessionState.Instance.reputation : 0;
    }
}
