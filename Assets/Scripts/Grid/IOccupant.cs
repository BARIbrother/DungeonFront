using UnityEngine;

// 그리드에 배치되는 occupant의 공통 인터페이스. PlaceManager가 배치·조회 시 사용한다.
public interface IOccupant
{
    // occupant 인스턴스 고유 ID (GUID 등).
    string InstanceId { get; }

    // occupant 종류 (기계·벨트·출력기 등).
    OccupantKind Kind { get; }

    // 배치 기준 그리드 좌표 (footprint 좌하단 등).
    Vector2Int Anchor { get; }

    // footprint 크기 (가로×세로 셀 수).
    Vector2Int Size { get; }
}
