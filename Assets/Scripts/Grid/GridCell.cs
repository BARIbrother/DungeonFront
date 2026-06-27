// 그리드 한 칸의 로직 데이터. 지형 타입과 occupation 포인터(instanceId)를 보관한다.
[System.Serializable]
public struct GridCell
{
    public GridCellType Type;
    public string OccupantInstanceId;

    // OccupantInstanceId가 있으면 true. PlaceManager가 배치 시 설정한다.
    public bool IsOccupied => !string.IsNullOrEmpty(OccupantInstanceId);

    // type과 occupantInstanceId로 셀을 생성한다. occupantInstanceId 기본값은 null.
    public GridCell(GridCellType type, string occupantInstanceId = null)
    {
        Type = type;
        OccupantInstanceId = occupantInstanceId;
    }
}
