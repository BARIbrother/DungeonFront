using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestUnlockManager : MonoBehaviour
{
    [Serializable]
    public class UnlockRule
    {
        public string machineDefId;
        [Min(0)] public int requiredReputation;
    }

    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private UnlockRule[] rules = Array.Empty<UnlockRule>();
    [SerializeField] private bool persistWithPlayerPrefs = true;

    private readonly HashSet<string> unlocked = new(StringComparer.Ordinal);

    public event Action OnUnlocksChanged;

    private void Awake()
    {
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        if (!persistWithPlayerPrefs)
        {
            return;
        }

        foreach (UnlockRule rule in rules)
        {
            if (rule != null && PlayerPrefs.GetInt(GetKey(rule.machineDefId), 0) == 1)
            {
                unlocked.Add(rule.machineDefId);
            }
        }
    }

    public bool IsUnlocked(string machineDefId)
    {
        return !string.IsNullOrWhiteSpace(machineDefId) && unlocked.Contains(machineDefId);
    }

    public bool TryUnlock(string machineDefId)
    {
        UnlockRule rule = Array.Find(rules, candidate =>
            candidate != null && candidate.machineDefId == machineDefId);
        if (rule == null || economy == null || economy.Reputation < rule.requiredReputation)
        {
            return false;
        }

        if (!unlocked.Add(machineDefId))
        {
            return true;
        }

        if (persistWithPlayerPrefs)
        {
            PlayerPrefs.SetInt(GetKey(machineDefId), 1);
            PlayerPrefs.Save();
        }

        OnUnlocksChanged?.Invoke();
        return true;
    }

    public int GetRequiredReputation(string machineDefId)
    {
        UnlockRule rule = Array.Find(rules, candidate =>
            candidate != null && candidate.machineDefId == machineDefId);
        return rule?.requiredReputation ?? int.MaxValue;
    }

    private static string GetKey(string machineDefId)
    {
        return $"DungeonFront.Week3.Unlock.{machineDefId}";
    }
}
