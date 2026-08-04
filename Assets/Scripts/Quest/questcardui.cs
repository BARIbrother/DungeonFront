using System.Collections;
using UnityEngine;

// Prepare 단계의 "받을 수 있는 의뢰" 목록과 수락 버튼을 담당한다.
public class QuestAcceptUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestPool questPool;
    [SerializeField] private QuestCard questCardPrefab;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject panel;

    private GameSessionState session;

    private int CurrentReputation
    {
        get
        {
            Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
            return economy != null
                ? economy.Reputation
                : GameSessionState.Instance != null
                    ? GameSessionState.Instance.reputation
                    : 0;
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (questManager != null)
        {
            questManager.OnQuestsChanged += Refresh;
        }

        session = GameSessionState.Instance;
        if (session != null)
        {
            session.OnPhaseChanged += HandlePhaseChanged;
            session.OnNewGame += HandleNewGame;
        }

        RefreshAvailableAndCards();
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
            session.OnNewGame -= HandleNewGame;
        }
    }

    private void HandleNewGame()
    {
        StartCoroutine(RefreshAfterNewGame());
    }

    private IEnumerator RefreshAfterNewGame()
    {
        // QuestManager도 같은 NewGame 이벤트에서 이전 목록을 비운다.
        // 한 프레임 뒤에 후보를 다시 만들면 구독 순서와 관계없이 최종 목록이 남는다.
        yield return null;
        RefreshAvailableAndCards();
    }

    private void ResolveReferences()
    {
        questManager ??= QuestManager.Instance;
        questManager ??= FindAnyObjectByType<QuestManager>();
        questPool ??= FindAnyObjectByType<QuestPool>();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        bool isPrepare = phase == GamePhase.Prepare;
        if (panel != null)
        {
            panel.SetActive(isPrepare);
        }

        if (isPrepare)
        {
            RefreshAvailableAndCards();
        }
    }

    public void RefreshAvailableAndCards()
    {
        ResolveReferences();
        questPool?.MakeAvailableQuestsToday(CurrentReputation);
        Refresh();
    }

    public void Refresh()
    {
        if (questManager == null || questCardPrefab == null || content == null)
        {
            return;
        }

        ClearGeneratedCards();
        foreach (Quest quest in questManager.availableQuestsToday)
        {
            QuestCard card = Instantiate(questCardPrefab, content);
            card.gameObject.AddComponent<GeneratedQuestCard>();
            card.SetQuest(quest);
            card.SetButtonLabel("수락");
            card.SetAcceptAction(() => TryAccept(quest));
            card.SetAcceptButtonInteractable(questManager.CanAcceptQuest(quest));
        }
    }

    private void TryAccept(Quest quest)
    {
        if (questManager.acceptQuest(quest))
        {
            RefreshAvailableAndCards();
        }
    }

    private void ClearGeneratedCards()
    {
        foreach (GeneratedQuestCard card
            in content.GetComponentsInChildren<GeneratedQuestCard>(true))
        {
            Destroy(card.gameObject);
        }
    }

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        RefreshAvailableAndCards();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}
