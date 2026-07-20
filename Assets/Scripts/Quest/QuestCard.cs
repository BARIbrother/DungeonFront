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
        titleText.text = quest.title;
        clientNameText.text = quest.clientName;
        deadlineText.text = "D-" + quest.deadlineDays;
        requireText.text = MakeItemString(quest.requiredItems);
        rewardsText.text = MakeItemString(quest.rewards);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        if(quest) SetQuest(quest);
    }


    public void SetAcceptButtonInteractable(bool interactable)
    {
        acceptButton.interactable = interactable;
    }

    public void SetAcceptAction(UnityEngine.Events.UnityAction action)
    {
        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(action);
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

            builder.Append(entry.item.name);
            builder.Append(" x");
            builder.Append(entry.count);
            builder.Append('\n');
        }

        return builder.ToString().TrimEnd();
    }

}
