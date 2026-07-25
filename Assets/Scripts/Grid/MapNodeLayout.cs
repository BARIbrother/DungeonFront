using UnityEngine;

// 새 게임·구역 해금 시 깔 자원 노드 설계도. 세이브 상태가 아니다.
[CreateAssetMenu(fileName = "MapNodeLayout", menuName = "DungeonFront/Map Node Layout")]
public class MapNodeLayout : ScriptableObject
{
    [SerializeField] private MapNodeLayoutEntry[] entries;

    public MapNodeLayoutEntry[] Entries => entries;

    // 기본 레이아웃. zone_start는 중앙 스폰(12,12)을 피해 좌·우 사이드 2개만 둔다.
    public static MapNodeLayoutEntry[] CreateDefaultEntries()
    {
        return new[]
        {
            new MapNodeLayoutEntry
            {
                zoneId = ZoneManager.CenterZoneId,
                gridCoord = new Vector2Int(8, 12),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = ZoneManager.CenterZoneId,
                gridCoord = new Vector2Int(15, 12),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(18, 11),
                itemId = "iron_ore"
            },
        };
    }
}
