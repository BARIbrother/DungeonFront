using UnityEngine;

// 그리드에 배치되는 occupant(기계·벨트 등)의 런타임 인스턴스. PlaceManager가 생성·관리한다.
public class OccupantInstance : IOccupant
{
    public string InstanceId { get; }
    public OccupantKind Kind { get; }
    public Vector2Int Anchor { get; private set; }
    public Vector2Int Size { get; }

    // instanceId·kind·anchor(배치 기준 칸)·size(footprint)로 occupant를 생성한다.
    public OccupantInstance(string instanceId, OccupantKind kind, Vector2Int anchor, Vector2Int size)
    {
        InstanceId = instanceId;
        Kind = kind;
        Anchor = anchor;
        Size = size;
    }

    // 배치 기준 칸(anchor)을 갱신한다. 이동·재배치 시 PlaceManager가 호출한다.
    public void SetAnchor(Vector2Int anchor)
    {
        Anchor = anchor;
    }
}
