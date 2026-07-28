using UnityEngine;
using UnityEngine.UI;

public class QuestButton : MonoBehaviour
{
    public int questId;      
    public string questName; 

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickQuest);
        }
    }

    private void OnClickQuest()
    {
        if (GameSessionState.Instance == null) return;

        // 누르기 전, 이미 수락된 상태였는지 미리 확인
        bool alreadyAccepted = GameSessionState.Instance.quests.Exists(q => q.questId == questId);

        // 의뢰 처리 요청
        bool success = GameSessionState.Instance.TryAcceptQuest(questId, questName);

        if (success)
        {
            if (alreadyAccepted)
            {
                // 이미 수락했던 상태에서 눌렀으므로 취소 성공
                Debug.Log($"<color=orange>[UI 알림] {questName} 취소 완료!</color>");
            }
            else
            {
                // 새로 수락 성공
                Debug.Log($"<color=green>[UI 알림] {questName} 수락 완료!</color>");
            }
        }
        else
        {
            // 3개 초과로 인한 실패
            Debug.LogWarning($"<color=red>[UI 알림] {questName} 수락 실패! (최대 3개 제한)</color>");
        }
    }
}
