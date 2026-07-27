using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private UnlockManager unlockManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform listRoot;
    [SerializeField] private Button rowPrefab;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text feedbackText;

    private void OnEnable()
    {
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        unlockManager ??= FindAnyObjectByType<UnlockManager>();
        playerInventory ??= FindAnyObjectByType<PlayerInventory>();

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

    public bool TryPurchase(string entryId)
    {
        ShopEntry entry = catalog != null ? catalog.Get(entryId) : null;
        if (entry == null || economy == null)
        {
            SetFeedback("구매 정보를 찾을 수 없습니다.");
            return false;
        }

        if (GameSessionState.Instance != null
            && GameSessionState.Instance.phase != GamePhase.Prepare)
        {
            SetFeedback("준비 단계에서만 구매할 수 있습니다.");
            return false;
        }

        if (entry.IsMachine
            && (unlockManager == null || !unlockManager.IsUnlocked(entry.machineDefId)))
        {
            SetFeedback("아직 해금되지 않은 기계입니다.");
            return false;
        }

        if (!economy.TrySpendGold(entry.price))
        {
            SetFeedback("골드가 부족합니다.");
            return false;
        }

        if (entry.IsMachine)
        {
            if (!TryAddMachine(entry))
            {
                economy.AddGold(entry.price);
                SetFeedback("기계를 지급할 수 없습니다.");
                return false;
            }
        }
        else if (entry.item != null && playerInventory != null)
        {
            playerInventory.Add(new ItemEntry { item = entry.item, count = entry.count });
        }
        else
        {
            economy.AddGold(entry.price);
            SetFeedback("구매 대상을 지급할 수 없습니다.");
            return false;
        }

        SetFeedback($"{entry.displayName} 구매 완료");
        return true;
    }

    public void Refresh()
    {
        if (goldText != null)
        {
            goldText.text = $"골드 {economy?.Gold ?? 0}";
        }

        if (listRoot == null || rowPrefab == null || catalog == null)
        {
            return;
        }

        foreach (Week3GeneratedShopRow row in listRoot.GetComponentsInChildren<Week3GeneratedShopRow>(true))
        {
            Destroy(row.gameObject);
        }

        foreach (ShopEntry entry in catalog.entries)
        {
            if (entry == null || (entry.IsMachine
                && (unlockManager == null || !unlockManager.IsUnlocked(entry.machineDefId))))
            {
                continue;
            }

            Button row = Instantiate(rowPrefab, listRoot);
            row.gameObject.AddComponent<Week3GeneratedShopRow>();
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = $"{entry.displayName}  {entry.price}G";
            }

            string id = entry.entryId;
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() => TryPurchase(id));
            row.interactable = economy != null && economy.Gold >= entry.price;
        }
    }

    private bool TryAddMachine(ShopEntry entry)
    {
        if (playerInventory == null || entry.machineDefinition == null)
        {
            return false;
        }

        playerInventory.AddMachine(entry.machineDefinition);
        return true;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }

        Debug.Log($"[Shop] {message}", this);
    }
}

public sealed class Week3GeneratedShopRow : MonoBehaviour
{
}
