using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnlockUI : MonoBehaviour
{
    [SerializeField] private UnlockManager unlockManager;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private string machineDefId;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private Button unlockButton;

    private void OnEnable()
    {
        unlockManager ??= FindAnyObjectByType<UnlockManager>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        if (economy != null)
        {
            economy.OnEconomyChanged += Refresh;
        }

        if (unlockManager != null)
        {
            unlockManager.OnUnlocksChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (economy != null)
        {
            economy.OnEconomyChanged -= Refresh;
        }

        if (unlockManager != null)
        {
            unlockManager.OnUnlocksChanged -= Refresh;
        }
    }

    public void TryUnlock()
    {
        unlockManager?.TryUnlock(machineDefId);
        Refresh();
    }

    public void Refresh()
    {
        if (unlockManager == null)
        {
            return;
        }

        bool unlocked = unlockManager.IsUnlocked(machineDefId);
        int required = unlockManager.GetRequiredReputation(machineDefId);
        if (conditionText != null)
        {
            conditionText.text = unlocked ? "해금 완료" : $"명성 {required} 필요";
        }

        if (unlockButton != null)
        {
            unlockButton.interactable = !unlocked
                && economy != null
                && economy.Reputation >= required;
        }
    }
}
