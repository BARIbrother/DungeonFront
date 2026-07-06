using UnityEngine;

// 의뢰 정적 데이터. Recipe와 같이 ItemEntryList로 품목을 표현한다.
[CreateAssetMenu(fileName = "Quest", menuName = "DungeonFront/Quest")]
public class Quest : ScriptableObject
{
    // 의뢰 제목
    public string title;

    // 의뢰인 이름
    public string clientName;

    // 의뢰 본문
    [TextArea]
    public string content;

    // 납품해야 할 품목 목록
    public ItemEntryList requiredItems;

    // 완료 시 지급할 보상 목록
    public ItemEntryList rewards;

    // 의뢰 기한(일). SO 원본에 설정된 값
    public int deadlineDays;

    // 수락 후 남은 기한(일). acceptQuest 시 deadlineDays로 초기화된다
    public int currentleftDeadlineDays;
}
