using UnityEngine;

[CreateAssetMenu(fileName = "TechNode", menuName = "DungeonFront/TechNode")]
public class TechNodeSO : ScriptableObject
{
    public string techId;               // 노드 고유 ID (예: "Machine_Tier2")
    public string techName;             // 노드 이름 (예: "고급 추출기")
    [TextArea] public string description; // 설명

    [Header("[해금 비용]")]
    public int requiredGold;            // 필요 골드
    public int requiredReputation;      // 필요 명성

    [Header("[선행 노드 조건]")]
    public TechNodeSO parentNode;       // 이 노드를 해금하기 위해 먼저 해금되어야 하는 아래쪽 노드 (없으면 null)

    [Header("[런타임 상태]")]
    public bool isUnlocked = false;     // 해금 완료 여부
}