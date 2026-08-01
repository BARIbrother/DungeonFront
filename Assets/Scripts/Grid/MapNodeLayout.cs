using UnityEngine;

// 새 게임·구역 해금 시 깔 자원 노드 설계도. 세이브 상태가 아니다.
[CreateAssetMenu(fileName = "MapNodeLayout", menuName = "DungeonFront/Map Node Layout")]
public class MapNodeLayout : ScriptableObject
{
    [SerializeField] private MapNodeLayoutEntry[] entries;

    public MapNodeLayoutEntry[] Entries => entries;

    // Week5 맵 정본(16칸·가로3·세로4) 노드 좌표.
    public static MapNodeLayoutEntry[] CreateDefaultEntries()
    {
        return new[]
        {
            new MapNodeLayoutEntry
            {
                zoneId = "zone_start",
                gridCoord = new Vector2Int(2, 1),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_start",
                gridCoord = new Vector2Int(7, 2),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_start",
                gridCoord = new Vector2Int(11, 2),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_start",
                gridCoord = new Vector2Int(13, 11),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_start",
                gridCoord = new Vector2Int(2, 12),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(11, 18),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(3, 19),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(7, 19),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(13, 21),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(2, 22),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(1, 25),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(4, 27),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(13, 27),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(9, 28),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_1",
                gridCoord = new Vector2Int(13, 30),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_2",
                gridCoord = new Vector2Int(4, 34),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_2",
                gridCoord = new Vector2Int(0, 39),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_2",
                gridCoord = new Vector2Int(0, 45),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_2",
                gridCoord = new Vector2Int(5, 45),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_2",
                gridCoord = new Vector2Int(8, 46),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_3",
                gridCoord = new Vector2Int(2, 49),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_3",
                gridCoord = new Vector2Int(8, 50),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_3",
                gridCoord = new Vector2Int(13, 52),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_3",
                gridCoord = new Vector2Int(11, 56),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_0_3",
                gridCoord = new Vector2Int(10, 62),
                itemId = "iron_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_0",
                gridCoord = new Vector2Int(29, 3),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_0",
                gridCoord = new Vector2Int(17, 6),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_0",
                gridCoord = new Vector2Int(29, 8),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_0",
                gridCoord = new Vector2Int(21, 12),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_0",
                gridCoord = new Vector2Int(28, 13),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(22, 17),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(19, 18),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(30, 18),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(26, 19),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(18, 21),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(29, 22),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(18, 24),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(28, 25),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(19, 27),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_1",
                gridCoord = new Vector2Int(27, 29),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_2",
                gridCoord = new Vector2Int(28, 36),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_2",
                gridCoord = new Vector2Int(27, 42),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_2",
                gridCoord = new Vector2Int(21, 45),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_2",
                gridCoord = new Vector2Int(27, 45),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_2",
                gridCoord = new Vector2Int(18, 46),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_3",
                gridCoord = new Vector2Int(19, 52),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_3",
                gridCoord = new Vector2Int(18, 59),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_3",
                gridCoord = new Vector2Int(22, 61),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_3",
                gridCoord = new Vector2Int(25, 62),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_1_3",
                gridCoord = new Vector2Int(18, 63),
                itemId = "mana_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_0",
                gridCoord = new Vector2Int(42, 3),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_0",
                gridCoord = new Vector2Int(36, 6),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_0",
                gridCoord = new Vector2Int(35, 12),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(37, 17),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(34, 19),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(47, 19),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(43, 26),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(37, 29),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_2",
                gridCoord = new Vector2Int(39, 34),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_2",
                gridCoord = new Vector2Int(47, 36),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_2",
                gridCoord = new Vector2Int(35, 41),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_3",
                gridCoord = new Vector2Int(43, 50),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_3",
                gridCoord = new Vector2Int(45, 53),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_3",
                gridCoord = new Vector2Int(42, 61),
                itemId = "black_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_0",
                gridCoord = new Vector2Int(37, 0),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_0",
                gridCoord = new Vector2Int(33, 5),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_0",
                gridCoord = new Vector2Int(35, 9),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(40, 19),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(43, 19),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(44, 22),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(46, 26),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_1",
                gridCoord = new Vector2Int(44, 29),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_2",
                gridCoord = new Vector2Int(44, 34),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_2",
                gridCoord = new Vector2Int(35, 45),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_2",
                gridCoord = new Vector2Int(40, 45),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_3",
                gridCoord = new Vector2Int(37, 51),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_3",
                gridCoord = new Vector2Int(46, 57),
                itemId = "white_ore"
            },
            new MapNodeLayoutEntry
            {
                zoneId = "zone_2_3",
                gridCoord = new Vector2Int(38, 61),
                itemId = "white_ore"
            },
        };
    }
}
