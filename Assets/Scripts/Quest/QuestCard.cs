using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class QuestCard : MonoBehaviour
{
    [SerializeField] private Quest quest;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text clientNameText;
    [SerializeField] private TMP_Text deadlineText;
    [SerializeField] private TMP_Text requireText;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private Button acceptButton;

    public void SetQuest(Quest new_quest)
    {
        quest = new_quest;
        if (quest == null)
        {
            return;
        }

        if (titleText != null) titleText.text = quest.title;
        if (clientNameText != null) clientNameText.text = quest.clientName;
        if (deadlineText != null) deadlineText.text = quest.GetDeadlineDisplayText();
        if (requireText != null) requireText.text = MakeItemString(quest.requiredItems);
        if (rewardsText != null && rewardsText != requireText)
        {
            rewardsText.text = MakeItemString(quest.rewards);
        }
    }

    // GUILayout 등 TMP color 태그가 불필요한 표시용.
    public static string FormatDeadline(Quest target)
    {
        if (target == null || target.currentleftDeadlineDays <= 0)
        {
            return "오늘 마감";
        }

        return $"납기 D-{target.currentleftDeadlineDays}";
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        if(quest) SetQuest(quest);
    }


    public void SetAcceptButtonInteractable(bool interactable)
    {
        if (acceptButton != null)
        {
            acceptButton.interactable = interactable;
        }
    }

    public void SetAcceptAction(UnityEngine.Events.UnityAction action)
    {
        if (acceptButton == null)
        {
            return;
        }

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(action);
    }

    public void SetButtonLabel(string label)
    {
        TMP_Text text = acceptButton != null
            ? acceptButton.GetComponentInChildren<TMP_Text>()
            : null;
        if (text != null)
        {
            text.text = label;
        }
    }

    private string MakeItemString(ItemEntryList list)
    {
        if (list == null || list.entries == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();

        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null)
            {
                continue;
            }

            builder.Append(string.IsNullOrWhiteSpace(entry.item.displayName)
                ? entry.item.id
                : entry.item.displayName);
            builder.Append(" x");
            builder.Append(entry.count);
            builder.Append('\n');
        }

        return builder.ToString().TrimEnd();
    }

}

// 동적 목록을 갱신할 때 Dev2가 생성한 카드만 골라 지우기 위한 표식.
public sealed class GeneratedQuestCard : MonoBehaviour
{
}
