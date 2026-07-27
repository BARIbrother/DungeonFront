using System.Collections.Generic;
using UnityEngine;

public class QuestDeadlineController : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private GameOverController gameOverController;

    private GameSessionState session;
    private int observedDay;

    private void OnEnable()
    {
        questManager ??= FindAnyObjectByType<QuestManager>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        gameOverController ??= FindAnyObjectByType<GameOverController>();
        session = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();

        if (questManager != null)
        {
            questManager.OnQuestCompleted += HandleCompleted;
        }

        if (session != null)
        {
            observedDay = session.day;
            session.OnPhaseChanged += HandlePhaseChanged;
            session.OnNewGame += HandleNewGame;
        }
    }

    private void OnDisable()
    {
        if (questManager != null)
        {
            questManager.OnQuestCompleted -= HandleCompleted;
        }

        if (session != null)
        {
            session.OnPhaseChanged -= HandlePhaseChanged;
            session.OnNewGame -= HandleNewGame;
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (questManager == null || session == null)
        {
            return;
        }

        if (phase == GamePhase.Prepare && session.day > observedDay)
        {
            // D-0은 "오늘 결산까지 제출 가능"이다.
            // 다음 날로 넘어올 때 먼저 어제 D-0이었던 의뢰를 미납 처리한 뒤
            // 나머지 의뢰의 D-day를 한 칸 줄인다.
            EvaluateExpiredQuests();
            questManager.OnDayAdvanced(session.day - observedDay);
            observedDay = session.day;
        }
    }

    public void EvaluateExpiredQuests()
    {
        if (questManager == null)
        {
            return;
        }

        var expired = new List<Quest>();
        foreach (Quest quest in questManager.currentQuests)
        {
            if (quest != null && quest.currentleftDeadlineDays <= 0)
            {
                expired.Add(quest);
            }
        }

        foreach (Quest quest in expired)
        {
            QuestRuntimeInfo info = QuestRuntimeRegistry.GetOrCreate(quest);
            if (info.isMandatory)
            {
                gameOverController?.TriggerGameOver();
            }
            else if (economy != null)
            {
                int penalty = Mathf.RoundToInt(info.rewardReputation * 0.5f);
                economy.AddReputation(-penalty);
                Debug.Log($"[Quest] {quest.title} 미납 — 명성 {penalty} 차감", quest);
            }

            questManager.ExpireQuest(quest);
        }
    }

    private void HandleCompleted(Quest quest)
    {
        if (quest != null && economy != null)
        {
            QuestRuntimeInfo info = QuestRuntimeRegistry.GetOrCreate(quest);
            economy.AddReputation(info.rewardReputation);
        }
    }

    private void HandleNewGame()
    {
        observedDay = session != null ? session.day : 1;
    }
}
