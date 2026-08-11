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
    private bool frameApplied;

    private void OnEnable()
    {
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

        ApplyPanelFrame();
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
            card.SetButtonLabel("납품");
            card.SetAcceptAction(() => TryDeliver(quest));
            card.SetAcceptButtonInteractable(questManager.CanCompleteQuest(quest));
        }
    }

    // LightFantasy 패널 프레임을 결산 의뢰 목록에 적용한다.
    private void ApplyPanelFrame()
    {
        if (frameApplied)
        {
            return;
        }

        UiPanelFrame.ApplyTo(panel);
        UiButtonStyle.ApplyInChildren(panel);
        frameApplied = panel != null;
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
