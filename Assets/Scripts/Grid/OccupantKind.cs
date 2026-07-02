// 그리드에 올라가는 occupant의 종류. PlaceManager·물류 연결 시 Kind별 규칙을 적용한다.
public enum OccupantKind
{
    Machine = 0,
    Belt = 1,
    Outputter = 2,
    ResourceNode = 3,
    MachineOnResourceNode = 4,
}
