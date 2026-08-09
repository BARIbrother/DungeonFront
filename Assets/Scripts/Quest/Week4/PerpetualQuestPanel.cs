using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상시 의뢰 선택 → 가능한 배수 확인 → 슬라이더로 n 선택 → 납품 UI.
public class PerpetualQuestPanel : MonoBehaviour
{
    [SerializeField] private QuestPool questPool;
    [SerializeField] private PerpetualQuestService service;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private TMP_Dropdown questDropdown;
    [SerializeField] private Slider multiplierSlider;
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button deliverButton;

    private readonly List<Quest> perpetualQuests = new();

    private void OnEnable()
    {
        questPool ??= FindAnyObjectByType<QuestPool>();
        service ??= FindAnyObjectByType<PerpetualQuestService>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnItemsChanged += RefreshSelected;
        }

        RebuildQuestList();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnItemsChanged -= RefreshSelected;
        }

        DestroyQuestCopies();
    }

    public void RebuildQuestList()
    {
        DestroyQuestCopies();
        if (questPool == null)
        {
            SetFeedback("QuestPool이 연결되지 않았습니다.");
            return;
        }

        int reputation = economy != null ? economy.Reputation : 0;
        perpetualQuests.AddRange(questPool.CreatePerpetualQuestList(reputation));

        if (questDropdown != null)
        {
            questDropdown.ClearOptions();
            questDropdown.AddOptions(
                perpetualQuests.ConvertAll(quest => quest.title));
        }

        RefreshSelected();
    }

    public void RefreshSelected()
    {
        Quest quest = GetSelectedQuest();
        int maximum = quest != null && service != null
            ? service.GetMaxMultiplier(quest)
            : 0;

        if (multiplierSlider != null)
        {
            multiplierSlider.wholeNumbers = true;
            multiplierSlider.minValue = maximum > 0 ? 1 : 0;
            multiplierSlider.maxValue = Mathf.Max(1, maximum);
            multiplierSlider.value = maximum > 0
                ? Mathf.Clamp(multiplierSlider.value, 1, maximum)
                : 0;
        }

        RefreshTexts(quest, GetSelectedMultiplier());
        if (deliverButton != null)
        {
            deliverButton.interactable = maximum > 0;
        }
    }

    public void HandleMultiplierChanged(float _)
    {
        RefreshTexts(GetSelectedQuest(), GetSelectedMultiplier());
    }

    public void TryDeliverSelected()
    {
        Quest quest = GetSelectedQuest();
        int multiplier = GetSelectedMultiplier();
        bool delivered = service != null && service.TryDeliver(quest, multiplier);
        SetFeedback(delivered ? $"x{multiplier} 납품 완료" : "납품할 수 없습니다.");
        RefreshSelected();
    }

    private void RefreshTexts(Quest quest, int multiplier)
    {
        if (multiplierText != null)
        {
            multiplierText.text = $"납품 배수 x{multiplier}";
        }

        if (requirementText != null)
        {
            requirementText.text = BuildItemText("필요", quest?.requiredItems, multiplier);
        }

        if (rewardText != null)
        {
            rewardText.text = BuildItemText("보상", quest?.rewards, multiplier);
        }
    }

    private static string BuildItemText(
        string label,
        ItemEntryList list,
        int multiplier)
    {
        var builder = new StringBuilder(label);
        builder.AppendLine();

        foreach (ItemEntry entry in list?.entries ?? System.Array.Empty<ItemEntry>())
        {
            if (entry?.item == null)
            {
                continue;
            }

            string displayName = string.IsNullOrWhiteSpace(entry.item.DisplayName)
                ? entry.item.Id
                : entry.item.DisplayName;
            builder.AppendLine($"{displayName} x{entry.count * multiplier}");
        }

        return builder.ToString().TrimEnd();
    }

    private Quest GetSelectedQuest()
    {
        int index = questDropdown != null ? questDropdown.value : 0;
        return index >= 0 && index < perpetualQuests.Count
            ? perpetualQuests[index]
            : null;
    }

    private int GetSelectedMultiplier()
    {
        return multiplierSlider != null
            ? Mathf.RoundToInt(multiplierSlider.value)
            : 0;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }

    private void DestroyQuestCopies()
    {
        foreach (Quest quest in perpetualQuests)
        {
            if (quest != null)
            {
                QuestRuntimeRegistry.Forget(quest);
                Destroy(quest);
            }
        }

        perpetualQuests.Clear();
    }
}
