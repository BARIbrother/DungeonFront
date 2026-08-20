using System.Collections.Generic;
using UnityEngine;

public class QuestDeadlineController : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private QuestFailController gameOverController;

    private GameSessionState session;
    private int observedDay;

    private void OnEnable()
    {
        questManager ??= FindAnyObjectByType<QuestManager>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        gameOverController ??= FindAnyObjectByType<QuestFailController>();
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
                GameOverController.Instance?.TriggerGameOver("필수 의뢰를 완료하지 못했습니다");
            }
            else
            {
                ApplyOverduePenalty(quest, info);
            }

            questManager.ExpireQuest(quest);
        }
    }

    // 일반 의뢰 미납: 보상 명성의 0.5배를 차감한다.
    // economy가 없어도 세션에 직접 반영해, 페널티 없이 조용히 사라지지 않게 한다.
    private void ApplyOverduePenalty(Quest quest, QuestRuntimeInfo info)
    {
        int penalty = Mathf.RoundToInt(info.rewardReputation * 0.5f);
        if (penalty <= 0)
        {
            Debug.LogWarning(
                $"[Quest] {quest.title} 미납 — 보상 명성이 {info.rewardReputation}이라 차감할 값이 없습니다. "
                + "questline.json의 fame 보상 또는 QuestRuntimeInfo 등록을 확인하세요.",
                quest);
            return;
        }

        if (economy != null)
        {
            economy.AddReputation(-penalty);
        }
        else if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.AddReputation(-penalty);
        }
        else
        {
            Debug.LogWarning($"[Quest] {quest.title} 미납 — 명성을 반영할 대상이 없습니다.", quest);
            return;
        }

        Debug.Log($"[Quest] {quest.title} 미납 — 명성 {penalty} 차감", quest);
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
