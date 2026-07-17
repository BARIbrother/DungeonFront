using UnityEngine;

public class QuestAcceptUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestPool questPool;
    [SerializeField] private QuestCard questCardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject panel;

    private void Start()
    {
        if (questPool != null)
        {
            questPool.MakeAvailableQuestsToday(4);   // 테스트용
            Refresh();
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (questManager == null || questCardPrefab == null || content == null)
        {
            Debug.LogWarning("QuestAcceptUI reference is missing.");
            return;
        }

        ClearCards();


        Debug.Log("Refresh");

        Debug.Log(questManager.availableQuestsToday.Count);

        foreach (Quest quest in questManager.availableQuestsToday)
        {
            Debug.Log("Create Card : " + quest.title);

            QuestCard card = Instantiate(questCardPrefab, content);

            Debug.Log(card.name);

            card.SetQuest(quest);

            card.SetAcceptAction(() =>
            {
                bool accepted = questManager.acceptQuest(quest);

                if (accepted)
                {
                    questPool.MakeAvailableQuestsToday(4);    // 테스트용
                    Refresh();
                }
            });

            card.SetAcceptButtonInteractable(questManager.currentQuests.Count < 3);
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