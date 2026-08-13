using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private QuestUnlockManager unlockManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform listRoot;
    [SerializeField] private Button rowPrefab;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text feedbackText;

    private void OnEnable()
    {
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        unlockManager ??= FindAnyObjectByType<QuestUnlockManager>();
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
        if (entry == null)
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

        if (entry.IsMachine)
        {
            if (!MachineCraftService.TryCraft(entry.machineDefId, out string error, entry.machineDefinition))
            {
                SetFeedback(string.IsNullOrEmpty(error) ? "구매에 실패했습니다." : error);
                return false;
            }

            SetFeedback($"{entry.displayName} 구매 완료");
            Refresh();
            return true;
        }

        if (economy == null || !economy.TrySpendGold(entry.price))
        {
            SetFeedback("골드가 부족합니다.");
            return false;
        }

        if (entry.item != null && playerInventory != null)
        {
            playerInventory.Add(new ItemEntry
            {
                item = Item.FromDefinition(entry.item),
                count = entry.count
            });
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
            if (entry == null)
            {
                continue;
            }

            if (entry.IsMachine)
            {
                MachineCraftCatalog.Recipe unlockRecipe = MachineCraftCatalog.Get(entry.machineDefId);
                if (unlockRecipe == null || !MachineCraftService.IsTechUnlocked(unlockRecipe))
                {
                    continue;
                }
            }

            Button row = Instantiate(rowPrefab, listRoot);
            row.gameObject.AddComponent<Week3GeneratedShopRow>();
            TMP_Text label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                MachineCraftCatalog.Recipe recipe = entry.IsMachine
                    ? MachineCraftCatalog.Get(entry.machineDefId)
                    : null;
                label.text = recipe != null
                    ? $"{entry.displayName}  {MachineCraftService.FormatCost(recipe)}"
                    : $"{entry.displayName}  {entry.price}G";
            }

            string id = entry.entryId;
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() => TryPurchase(id));
            bool canBuy = economy != null && economy.Gold >= entry.price;
            if (entry.IsMachine)
            {
                MachineCraftCatalog.Recipe recipe = MachineCraftCatalog.Get(entry.machineDefId);
                canBuy = recipe != null
                    && MachineCraftService.IsTechUnlocked(recipe)
                    && MachineCraftService.CanAfford(
                        recipe,
                        playerInventory,
                        economy != null ? economy.Gold : 0);
            }

            row.interactable = canBuy;
        }
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
