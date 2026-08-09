using UnityEngine;

// 그리드 한 칸의 로직 데이터. 지형 타입과 배치된 occupant를 보관한다.
[System.Serializable]
public struct GridCell
{
    public GridCellType Type;
    public GameObject Occupant;
    public OccupantKind OccupantKind;

    // 타일맵 광석 레이어용 itemId (예: iron_ore). 비어 있으면 광석 타일 없음.
    public string ResourceItemId;

    // 미해금 구역 등에서 광석 타일·노드 표시 여부.
    public bool ResourceNodeVisible;

    // Occupant가 있으면 true.
    public bool IsOccupied => Occupant != null;

    // type·occupant·occupantKind로 셀을 생성한다.
    public GridCell(GridCellType type, GameObject occupant = null, OccupantKind occupantKind = default)
    {
        Type = type;
        Occupant = occupant;
        OccupantKind = occupantKind;
        ResourceItemId = null;
        ResourceNodeVisible = false;
    }

    public bool HasResourceNode => !string.IsNullOrEmpty(ResourceItemId);
}
