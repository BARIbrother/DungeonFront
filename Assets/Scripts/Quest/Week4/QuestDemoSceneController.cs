using UnityEngine;

// 독립 데모 씬을 Play했을 때 팀 세션을 초기화하고 퀘스트 목록을 준비한다.
public class QuestDemoSceneController : MonoBehaviour
{
    private void Start()
    {
        GameSessionState.Instance?.NewGame();

        QuestPool pool = FindAnyObjectByType<QuestPool>();
        Week3EconomyService economy = FindAnyObjectByType<Week3EconomyService>();
        pool?.MakeAvailableQuestsToday(economy != null ? economy.Reputation : 0);
    }
}
