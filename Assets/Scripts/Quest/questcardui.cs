using UnityEngine;

public class QuestAcceptUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestCard questCardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject panel;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        Debug.Log("Refresh");
        ClearCards();

        if (questManager == null || questCardPrefab == null || content == null)
        {
            Debug.LogWarning("QuestAcceptUI reference is missing.");
            return;
        }

        foreach (Quest quest in questManager.availableQuestsToday)
        {
            QuestCard card = Instantiate(questCardPrefab, content);
            card.SetQuest(quest);

            card.SetAcceptAction(() =>
            {
                bool accepted = questManager.acceptQuest(quest);

                if (accepted)
                {
                    Refresh();
                }
            });

            bool canAccept = questManager.currentQuests.Count < 3;
            card.SetAcceptButtonInteractable(canAccept);
        }
    }

    private void ClearCards()
    {
        if (content == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        Refresh();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}