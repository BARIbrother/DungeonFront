using System;
using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    public event Action OnUnlocksChanged;

    private readonly HashSet<string> unlockedNodeIds = new HashSet<string>();

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

    private void Start()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnNewGame -= ResetUnlockedNodes;
            GameSessionState.Instance.OnNewGame += ResetUnlockedNodes;
        }
    }

    private void OnDestroy()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnNewGame -= ResetUnlockedNodes;
        }

        if (Instance == this)
        {
            Instance = null;
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
        return !string.IsNullOrEmpty(nodeId) && unlockedNodeIds.Contains(nodeId);
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

        if (node.parentIds == null)
        {
            return true;
        }

        for (int i = 0; i < node.parentIds.Length; i++)
        {
            if (!IsUnlocked(node.parentIds[i]))
            {
                return false;
            }
        }

        return true;
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
        if (node.parentIds == null || node.parentIds.Length == 0)
        {
            return "선행 기술을 먼저 해금해야 합니다.";
        }

        var names = new List<string>();
        for (int i = 0; i < node.parentIds.Length; i++)
        {
            string parentId = node.parentIds[i];
            if (Instance != null && Instance.IsUnlocked(parentId))
            {
                continue;
            }

            TechTreeCatalog.Node parent = TechTreeCatalog.Get(parentId);
            if (parent != null && parent.visibleInGame)
            {
                names.Add(parent.name);
            }
        }

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
