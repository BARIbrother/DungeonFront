using System;

// 기계 해금 테크 트리. 명예를 소모한다. 시작 4종은 데이터만 있고 인게임에 그리지 않는다.
public static class TechTreeCatalog
{
    public sealed class Node
    {
        public string id;
        public string name;
        public string description;
        public int honor;
        public bool visibleInGame;
        public bool startUnlocked;
        public string machineDefId;
        public string[] parentIds;
        public float x;
        public float y;
        public bool isFuelTrack;
        public int dayMinutes;
    }

    public readonly struct Connection
    {
        public readonly string from;
        public readonly string to;

        public Connection(string from, string to)
        {
            this.from = from;
            this.to = to;
        }
    }

    public static readonly Node[] All =
    {
        NodeOf("m_drill_1", "기초 채굴",
            "철광석·목재 채굴. 시작 지급.",
            0, false, true, "Miner_1", 40f, 40f),
        NodeOf("m_warehouse_1", "물자 보관",
            "생산품 저장. 시작 지급.",
            0, false, true, "Warehouse_1", 40f, 230f),
        NodeOf("m_furnace_1", "기초 제련",
            "철광석 → 철 주괴. 철 판·막대 제련. 시작 지급.",
            0, false, true, "Smelter_1", 40f, 420f),
        NodeOf("m_crafter_1", "기초 제작",
            "철 장비와 전투망치. 시작 지급.",
            0, false, true, "Assembler_1", 40f, 610f),

        NodeOf("m_conveyor_1", "자동 운송",
            "채굴에서 가공까지 자동으로 옮긴다.",
            20, true, false, "ConveyerBelt_1", 360f, 230f,
            "m_drill_1", "m_furnace_1"),
        NodeOf("m_drill_2", "심층 굴착",
            "마력석·칠흑석·순백석 광맥. 마나 챕터와 흑·백강 원료의 문.",
            40, true, false, "Miner_2", 360f, 40f,
            "m_drill_1"),

        NodeOf("m_manaext_1", "마나 추출",
            "몬스터 정수에서 마나를 뽑는다.",
            30, true, false, "ManaExtractor_1", 610f, 230f,
            "m_drill_2"),
        NodeOf("m_manastore_1", "마나 비축",
            "마나 버퍼. 추출과 가공 사이.",
            20, true, false, "ManaStorage_1", 860f, 230f,
            "m_warehouse_1", "m_drill_2"),
        NodeOf("m_manacraft_1", "마나 가공",
            "마력석에서 코어·완드, 마나강 장비, 1단계 스크롤.",
            50, true, false, "ManaHandmade_1", 610f, 610f,
            "m_drill_2"),
        NodeOf("m_enchant_1", "마법 부여",
            "마나 가공으로 만든 장비·스크롤을 인챈트한다.",
            20, true, false, "Enchanting_1", 860f, 610f,
            "m_manacraft_1"),

        NodeOf("m_furnace_2", "더 좋은 용광로",
            "흑강·백강 합금과 철 Lv.2 주괴.",
            850, true, false, "Smelter_2", 1210f, 320f,
            "m_manacraft_1", "m_drill_2"),
        NodeOf("m_crafter_2", "고속 제작기",
            "흑강·백강·진강 판·막대·장비. 진강 제련은 고열 용광로.",
            850, true, false, "Assembler_2", 1210f, 510f,
            "m_manacraft_1", "m_drill_2"),

        NodeOf("m_manacraft_2", "정교한 마나제작",
            "2단계 스크롤, 마술사 로브, 흑·백마법 코어·로브·지팡이.",
            500, true, false, "ManaAssembler_2", 1660f, 610f,
            "m_furnace_2", "m_crafter_2"),
        NodeOf("m_foundry_1", "거대 주조 시설",
            "콘크리트와 거대 구조물(기둥·대들보·지붕).",
            450, true, false, "Foundry_1", 1660f, 230f,
            "m_furnace_2", "m_crafter_2"),
        NodeOf("m_crafter_3", "대형 조립",
            "철제 뼈대와 제단 조립. 진강 장비는 고속 제작기.",
            400, true, false, "Assembler_3", 1660f, 420f,
            "m_furnace_2", "m_crafter_2"),
        NodeOf("m_drill_3", "암반 분쇄",
            "심화 광맥과 돌. 진강 원료.",
            400, true, false, "Miner_3", 2110f, 40f,
            "m_furnace_2"),
        NodeOf("m_furnace_3", "고열 용광로",
            "철·칠흑석·순백석 Lv.2로 진강 주괴를 만든다.",
            600, true, false, "Smelter_3", 2110f, 420f,
            "m_furnace_2", "m_drill_3"),

        NodeOf("m_manacraft_3", "의식 세공",
            "철 Lv.3 정련, 대검 날, 의식 스크롤.",
            700, true, false, "ManaAssembler_3", 2560f, 610f,
            "m_manacraft_2", "m_furnace_3"),
        NodeOf("m_altar_1", "제단 건립",
            "의식 부여. 집행자의 대검을 전쟁에 물들인다.",
            800, true, false, "Altar_1", 2810f, 420f,
            "m_foundry_1", "m_crafter_3", "m_manacraft_3"),

        FuelOf("fuel_1", "추가 근무 1",
            "생산 하루 3분 → 4분.",
            80, 360f, 850f, 4),
        FuelOf("fuel_2", "추가 근무 2",
            "생산 하루 4분 → 5분. 추가 근무 1이 필요하다.",
            220, 1210f, 850f, 5, "fuel_1"),
    };

    public static readonly Connection[] Connections =
    {
        new Connection("m_drill_1", "m_drill_2"),
        new Connection("m_drill_1", "m_conveyor_1"),
        new Connection("m_warehouse_1", "m_manastore_1"),
        new Connection("m_furnace_1", "m_conveyor_1"),
        new Connection("m_drill_2", "m_manaext_1"),
        new Connection("m_drill_2", "m_manastore_1"),
        new Connection("m_drill_2", "m_manacraft_1"),
        new Connection("m_manacraft_1", "m_enchant_1"),
        new Connection("m_manacraft_1", "m_furnace_2"),
        new Connection("m_manacraft_1", "m_crafter_2"),
        new Connection("m_drill_2", "m_furnace_2"),
        new Connection("m_drill_2", "m_crafter_2"),
        new Connection("m_furnace_2", "m_manacraft_2"),
        new Connection("m_crafter_2", "m_manacraft_2"),
        new Connection("m_furnace_2", "m_foundry_1"),
        new Connection("m_crafter_2", "m_foundry_1"),
        new Connection("m_furnace_2", "m_crafter_3"),
        new Connection("m_crafter_2", "m_crafter_3"),
        new Connection("m_furnace_2", "m_drill_3"),
        new Connection("m_furnace_2", "m_furnace_3"),
        new Connection("m_drill_3", "m_furnace_3"),
        new Connection("m_manacraft_2", "m_manacraft_3"),
        new Connection("m_furnace_3", "m_manacraft_3"),
        new Connection("m_foundry_1", "m_altar_1"),
        new Connection("m_crafter_3", "m_altar_1"),
        new Connection("m_manacraft_3", "m_altar_1"),
        new Connection("fuel_1", "fuel_2"),
    };

    public static Node Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].id == id)
            {
                return All[i];
            }
        }

        return null;
    }

    public static string DisplayName(string id)
    {
        Node node = Get(id);
        return node != null ? node.name : id;
    }

    private static Node NodeOf(
        string id,
        string name,
        string description,
        int honor,
        bool visibleInGame,
        bool startUnlocked,
        string machineDefId,
        float x,
        float y,
        params string[] parentIds)
    {
        return new Node
        {
            id = id,
            name = name,
            description = description,
            honor = honor,
            visibleInGame = visibleInGame,
            startUnlocked = startUnlocked,
            machineDefId = machineDefId,
            parentIds = parentIds ?? Array.Empty<string>(),
            x = x,
            y = y,
        };
    }

    private static Node FuelOf(
        string id,
        string name,
        string description,
        int honor,
        float x,
        float y,
        int dayMinutes,
        params string[] parentIds)
    {
        return new Node
        {
            id = id,
            name = name,
            description = description,
            honor = honor,
            visibleInGame = true,
            startUnlocked = false,
            machineDefId = null,
            parentIds = parentIds ?? Array.Empty<string>(),
            x = x,
            y = y,
            isFuelTrack = true,
            dayMinutes = dayMinutes,
        };
    }
}
