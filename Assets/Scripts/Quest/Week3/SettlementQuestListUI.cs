using UnityEngine;

// Settlement 단계에서 수락한 의뢰와 납품 가능 여부를 보여 준다.
// Week 2의 QuestCard 프리팹을 재사용하므로 새 카드 디자인이 없어도 테스트 가능하다.
public class SettlementQuestListUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestCard questCardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject panel;

    private GameSessionState session;
    private PlayerInventory playerInventory;

    private void OnEnable()
    {
        QuestListLayoutFinalizer.Apply(content, panel, "진행 중인 의뢰");
        questManager ??= QuestManager.Instance;
        questManager ??= FindAnyObjectByType<QuestManager>();

        if (questManager != null)
        {
            questManager.OnQuestsChanged += Refresh;
        }

        session = GameSessionState.Instance;
        if (session != null)
        {
            session.OnPhaseChanged += HandlePhaseChanged;
            HandlePhaseChanged(session.Phase);
        }

        playerInventory = PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerInventory.OnItemsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (questManager != null)
        {
            questManager.OnQuestsChanged -= Refresh;
        }

        if (session != null)
        {
            session.OnPhaseChanged -= HandlePhaseChanged;
        }

        if (playerInventory != null)
        {
            playerInventory.OnItemsChanged -= Refresh;
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (panel != null)
        {
            panel.SetActive(phase == GamePhase.Settlement);
        }

        if (phase == GamePhase.Settlement)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        QuestListLayoutFinalizer.Apply(content, panel, "진행 중인 의뢰");
        if (questManager == null || questCardPrefab == null || content == null)
        {
            return;
        }

        foreach (GeneratedQuestCard generated
            in content.GetComponentsInChildren<GeneratedQuestCard>(true))
        {
            Destroy(generated.gameObject);
        }

        foreach (Quest quest in questManager.currentQuests)
        {
            QuestCard card = Instantiate(questCardPrefab, content);
            card.gameObject.AddComponent<GeneratedQuestCard>();
            card.SetQuest(quest);
            card.SetButtonLabel("제출");
            card.SetAcceptAction(() => TryDeliver(quest));
            card.SetAcceptButtonInteractable(questManager.CanCompleteQuest(quest));
        }
    }

    private void TryDeliver(Quest quest)
    {
        if (!questManager.progressQuest(quest))
        {
            Debug.LogWarning($"[Quest] 납품 재료가 부족합니다: {quest.title}", quest);
        }

        Refresh();
    }
}
