using UnityEngine;

// 그리드 한 칸의 로직 데이터. 지형 타입과 배치된 occupant를 보관한다.
[System.Serializable]
public struct GridCell
{
    public GridCellType Type;
    public GameObject Occupant;
    public OccupantKind OccupantKind;

    // Occupant가 있으면 true.
    public bool IsOccupied => Occupant != null;

    // type·occupant·occupantKind로 셀을 생성한다.
    public GridCell(GridCellType type, GameObject occupant = null, OccupantKind occupantKind = default)
    {
        Type = type;
        Occupant = occupant;
        OccupantKind = occupantKind;
    }
}
