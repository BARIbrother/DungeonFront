using UnityEngine;
using System.Collections.Generic;

public class QuestWindowController : MonoBehaviour
{
    [Header("[UI 오브젝트 참조]")]
    [Tooltip("화면에 항상(또는 Prepare 단계에) 떠 있을 '의뢰창 열기' 버튼")]
    [SerializeField] private GameObject questOpenButton;

    [Tooltip("눌렀을 때 켜질 크게 만든 의뢰창 패널 (OrderWindow)")]
    [SerializeField] private GameObject orderWindowPanel;

    [Header("[현재 임시 선택된 퀘스트 목록 (최대 3개)]")]
    [SerializeField] private List<string> currentSelectedQuests = new List<string>();

    private void Start()
    {
        if (questOpenButton != null) questOpenButton.SetActive(true);
        if (orderWindowPanel != null) orderWindowPanel.SetActive(false);
    }

    public void OpenQuestWindow()
    {
        if (orderWindowPanel != null)
        {
            orderWindowPanel.SetActive(true);
            Debug.Log("[QuestUI] 의뢰창을 열었습니다.");
        }
    }

    public void CloseQuestWindow()
    {
        if (orderWindowPanel != null)
        {
            orderWindowPanel.SetActive(false);
            Debug.Log("[QuestUI] 의뢰창을 닫았습니다.");
        }
    }

    /// <summary>
    /// 퀘스트 1~5 버튼 클릭 시 선택 / 해제 토글 (최대 3개 제한)
    /// 예: OnToggleQuest("Quest 1")
    /// </summary>
    public void OnToggleQuest(string questName)
    {
        if (currentSelectedQuests.Contains(questName))
        {
            currentSelectedQuests.Remove(questName);
            Debug.Log($"[Quest] {questName} 선택 해제 (현재 {currentSelectedQuests.Count}/3)");
        }
        else
        {
            if (currentSelectedQuests.Count < 3)
            {
                currentSelectedQuests.Add(questName);
                Debug.Log($"[Quest] {questName} 선택 완료 (현재 {currentSelectedQuests.Count}/3)");
            }
            else
            {
                Debug.LogWarning("[Quest] 최대 3개까지만 선택할 수 있습니다!");
            }
        }
    }

    /// <summary>
    /// [선택 완료] 버튼 눌렀을 때 호출 -> QuestDataStore로 데이터 전달 후 창 닫기
    /// </summary>
    public void OnConfirmSelection()
    {
        if (QuestDataStore.Instance != null)
        {
            QuestDataStore.Instance.SaveSelectedQuests(currentSelectedQuests);
        }
        else
        {
            Debug.LogWarning("[QuestUI] QuestDataStore 인스턴스가 씬에 없습니다!");
        }

        CloseQuestWindow();
    }
}

//  // Settlement 씬에서 가져올 수 있게 코드 살짝 짜봤습니다.
//  List<string> myQuests = QuestDataStore.Instance.GetSelectedQuests();

//  foreach (string quest in myQuests)
//  {
//      Debug.Log($"Settlement 씬에서 불러온 퀘스트: {quest}");
//  }