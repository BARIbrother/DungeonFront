using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 개별 퀘스트 데이터 클래스 (멀티데이 납기 추적 포함)
/// </summary>
[System.Serializable]
public class QuestData
{
    public string questId;          // 예: "Quest_01"
    public string questTitle;       // 예: "강철 기어 10개 생산"
    public int deadlineDays;        // 초기 부여된 총 기한 (예: 3일)
    public int remainingDays;       // 현재 남은 일수 (예: 3, 2, 1, 0...)
    public bool isCompleted;        // 납기 완료 여부

    public QuestData(string id, string title, int deadline)
    {
        this.questId = id;
        this.questTitle = title;
        this.deadlineDays = deadline;
        this.remainingDays = deadline;
        this.isCompleted = false;
    }

    /// <summary>
    /// UI 표시용 D-Day 텍스트 반환
    /// </summary>
    public string GetDDayText()
    {
        if (remainingDays < 0)
        {
            return "<color=red>기한 초과 (미납)</color>";
        }
        else if (remainingDays == 0)
        {
            return "<color=red>D-Day (오늘 마감!)</color>";
        }
        else
        {
            return $"D-{remainingDays}";
        }
    }
}

/// <summary>
/// 씬 간 퀘스트 데이터를 유지하고 멀티데이 납기 일수를 관리하는 싱글톤 클래스
/// </summary>
public class QuestDataStore : MonoBehaviour
{
    public static QuestDataStore Instance { get; private set; }

    [Header("[수락하여 진행 중인 퀘스트 목록]")]
    [SerializeField] private List<QuestData> activeQuests = new List<QuestData>();

    private void Awake()
    {
        // 씬 전환 시 파괴되지 않도록 싱글톤 구성
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 새로운 퀘스트 수락 (Prepare 단계)
    /// 예: QuestDataStore.Instance.AcceptQuest("Quest_01", "강철 기어 생산", 3);
    /// </summary>
    public void AcceptQuest(string id, string title, int deadlineDays)
    {
        // 이미 진행 중인 퀘스트인지 확인 후 추가
        if (!activeQuests.Exists(q => q.questId == id))
        {
            QuestData newQuest = new QuestData(id, title, deadlineDays);
            activeQuests.Add(newQuest);
            Debug.Log($"[QuestDataStore] 퀘스트 수락됨: {title} | 남은 기한: {newQuest.GetDDayText()}");
        }
    }

    /// <summary>
    /// 단순 문자열 리스트로 저장했던 기존 방식과의 호환용 함수 (기본 기한 3일로 설정)
    /// </summary>
    public void SaveSelectedQuests(List<string> questNames)
    {
        activeQuests.Clear();
        foreach (var name in questNames)
        {
            AcceptQuest(name, name, 3); // 기본 3일 기한 부여
        }
    }

    /// <summary>
    /// 하루가 지날 때 호출 (Next Day / AdvanceDay 시) -> 모든 퀘스트 남은 일수 1 차감
    /// </summary>
    public void AdvanceDay()
    {
        foreach (var quest in activeQuests)
        {
            if (!quest.isCompleted)
            {
                quest.remainingDays--;
                Debug.Log($"[QuestDataStore] {quest.questTitle} 기한 갱신 -> {quest.GetDDayText()}");
            }
        }
    }

    /// <summary>
    /// 현재 진행 중인 전체 퀘스트 데이터 리스트 반환 (Settlement 씬 등에서 호출)
    /// </summary>
    public List<QuestData> GetActiveQuests()
    {
        return activeQuests;
    }

    /// <summary>
    /// 기존 호환용: 퀘스트 ID/이름 리스트로 반환
    /// </summary>
    public List<string> GetSelectedQuests()
    {
        List<string> names = new List<string>();
        foreach (var q in activeQuests)
        {
            names.Add($"{q.questTitle} ({q.GetDDayText()})");
        }
        return names;
    }

    /// <summary>
    /// 특정 퀘스트 완료 처리
    /// </summary>
    public void CompleteQuest(string questId)
    {
        QuestData quest = activeQuests.Find(q => q.questId == questId);
        if (quest != null)
        {
            quest.isCompleted = true;
            Debug.Log($"[QuestDataStore] {quest.questTitle} 완료 처리됨!");
        }
    }

    /// <summary>
    /// 진행 중인 퀘스트 데이터 전체 초기화
    /// </summary>
    public void ClearAllQuests()
    {
        activeQuests.Clear();
        Debug.Log("[QuestDataStore] 퀘스트 목록이 초기화되었습니다.");
    }
}