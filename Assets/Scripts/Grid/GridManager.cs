using System;
using System.Collections.Generic;
using UnityEngine;

// 그리드 데이터(지형·occupation)·좌표 변환·그리드 배치를 담당한다.
public class GridManager : MonoBehaviour
{
    // Factory 맵 정본: 가로 3구역 × 세로 4구역 × 16칸 = 48×64
    [SerializeField] private int width = 48;
    [SerializeField] private int height = 64;
    [SerializeField] private GridPlane plane = GridPlane.XY;
    [SerializeField] private int tilePixelSize = 32;
    [SerializeField] private float pixelsPerUnit = 32f;
    [SerializeField] private Vector2 tilePivot = new Vector2(0.5f, 0.5f);

    // 자원 노드 Prefab (32×32px 스프라이트 = 그리드 1칸 footprint)
    [SerializeField] private GameObject resourceNodePrefab;

    // SmelterMachine Prefab (64×64px = 2×2)
    [SerializeField] private GameObject smelterMachinePrefab;

    // AssemblerMachine Prefab (64×64px = 2×2)
    [SerializeField] private GameObject assemblerMachinePrefab;

    // MinerMachine Prefab (32×32px = 1×1)
    [SerializeField] private GameObject minerMachinePrefab;

    // 배치된 자원 노드 GameObject를 모아 둘 씬 계층 부모
    [SerializeField] private Transform resourceNodesRoot;

    // 배치된 기계 placeholder GameObject를 모아 둘 씬 계층 부모
    [SerializeField] private Transform machinesRoot;

    private GridCell[,] cells;

    // 배치된 자원 노드 (조회·추후 제거용)
    private List<ResourceNode> placedResourceNodes;

    // 배치된 기계 (조회·추후 제거용)
    private List<Machine> placedMachines;

    public int Width => width;
    public int Height => height;
    public GridPlane Plane => plane;
    public int TilePixelSize => tilePixelSize;
    public float PixelsPerUnit => pixelsPerUnit;
    public float CellSize => tilePixelSize / pixelsPerUnit;

    public GameObject MinerMachinePrefab => minerMachinePrefab;
    public GameObject SmelterMachinePrefab => smelterMachinePrefab;
    public GameObject AssemblerMachinePrefab => assemblerMachinePrefab;

    // 셀 데이터가 변경될 때 발생한다. TilemapRenderer 등이 구독한다.
    public event Action<Vector2Int, GridCell> CellChanged;

    // 그리드 배열·배치 레지스트리를 초기화한다.
    private void Awake()
    {
        placedResourceNodes = new List<ResourceNode>();
        placedMachines = new List<Machine>();
        InitializeGrid();
    }

    // cells가 null이면 InitializeGrid를 호출한다. Awake 이전 접근 대비용.
    private void EnsureInitialized()
    {
        if (cells == null)
        {
            InitializeGrid();
        }
    }

    // 48×64 셀 배열을 만들고 시작 구역(0,0)만 Floor, 나머지는 Locked로 채운다.
    public void InitializeGrid()
    {
        width = ZoneManager.ZonesX * ZoneManager.ZoneSize;
        height = ZoneManager.ZonesY * ZoneManager.ZoneSize;
        cells = new GridCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int zone = ZoneManager.GetZoneIndex(x, y);
                bool isStartZone = zone.x == ZoneManager.CenterZoneX
                    && zone.y == ZoneManager.CenterZoneY;
                cells[x, y] = new GridCell(isStartZone ? GridCellType.Floor : GridCellType.Locked);
            }
        }
    }

    // (x, y)가 그리드 범위 안인지 확인한다.
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // (x, y) 셀이 이동 가능한지 확인한다. Floor만 허용하며,
    // occupant가 있어도 컨베이어(Belt)는 지나갈 수 있다.
    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return false;
        }

        GridCell cell = GetCell(x, y);
        if (cell.Type != GridCellType.Floor)
        {
            return false;
        }

        if (cell.IsOccupied && cell.OccupantKind != OccupantKind.Belt)
        {
            return false;
        }

        return true;
    }

    // coord 셀이 이동 가능한지 확인한다.
    public bool IsWalkable(Vector2Int coord)
    {
        return IsWalkable(coord.x, coord.y);
    }

    // (x, y) 셀 데이터를 반환한다. 범위 밖이면 default를 반환한다.
    public GridCell GetCell(int x, int y)
    {
        EnsureInitialized();

        if (!IsInBounds(x, y))
        {
            return default;
        }

        return cells[x, y];
    }

    // coord 셀 데이터를 반환한다.
    public GridCell GetCell(Vector2Int coord)
    {
        return GetCell(coord.x, coord.y);
    }

    // (x, y) 셀 데이터를 갱신하고 CellChanged 이벤트를 발생시킨다.
    public void SetCell(int x, int y, GridCell cell)
    {
        EnsureInitialized();

        if (!IsInBounds(x, y))
        {
            return;
        }

        cells[x, y] = cell;
        CellChanged?.Invoke(new Vector2Int(x, y), cell);
    }

    // coord 셀 데이터를 갱신한다.
    public void SetCell(Vector2Int coord, GridCell cell)
    {
        SetCell(coord.x, coord.y, cell);
    }

    // coord에 배치된 occupant GameObject를 반환한다. 없으면 null.
    public GameObject GetOccupantAt(Vector2Int coord)
    {
        return GetCell(coord).Occupant;
    }

    // coord에 배치된 Machine을 반환한다. occupant가 없으면 footprint로도 찾는다.
    public Machine GetMachineAt(Vector2Int coord)
    {
        GameObject occupant = GetOccupantAt(coord);
        if (occupant != null)
        {
            Machine occupantMachine = occupant.GetComponent<Machine>();
            if (occupantMachine != null)
            {
                return occupantMachine;
            }
        }

        for (int i = 0; i < placedMachines.Count; i++)
        {
            Machine machine = placedMachines[i];
            if (machine == null)
            {
                continue;
            }

            if (FootprintContainsCoord(machine.GridAnchor, machine.GetFootprintSize(), coord))
            {
                return machine;
            }
        }

        return null;
    }

    // 그리드 좌표 (x, y)를 월드 좌표로 변환한다. tilePivot·CellSize·plane을 반영한다.
    public Vector3 GridToWorld(int x, int y)
    {
        float localX = (x + tilePivot.x) * CellSize;
        float localY = (y + tilePivot.y) * CellSize;

        Vector3 local = plane == GridPlane.XY
            ? new Vector3(localX, localY, 0f)
            : new Vector3(localX, 0f, localY);

        return transform.TransformPoint(local);
    }

    // coord를 월드 좌표로 변환한다.
    public Vector3 GridToWorld(Vector2Int coord)
    {
        return GridToWorld(coord.x, coord.y);
    }

    // 월드 좌표를 그리드 좌표로 변환한다. FloorToInt로 셀 인덱스를 구한다.
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float cellSize = CellSize;

        float axis1 = plane == GridPlane.XY ? local.y : local.z;
        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(axis1 / cellSize);
        return new Vector2Int(x, y);
    }

    // 월드 좌표를 그리드 범위 안으로 제한한다.
    public Vector3 ClampWorldPosition(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float cellSize = CellSize;
        float maxX = width * cellSize;
        float maxAxis1 = height * cellSize;

        local.x = Mathf.Clamp(local.x, 0f, maxX);
        if (plane == GridPlane.XY)
        {
            local.y = Mathf.Clamp(local.y, 0f, maxAxis1);
        }
        else
        {
            local.z = Mathf.Clamp(local.z, 0f, maxAxis1);
        }

        return transform.TransformPoint(local);
    }

    // width×height 맵의 기하학적 중앙 월드 좌표를 반환한다.
    public Vector3 GetMapCenterWorld()
    {
        float cellSize = CellSize;
        float localX = width * cellSize * 0.5f;
        float localAxis1 = height * cellSize * 0.5f;

        Vector3 local = plane == GridPlane.XY
            ? new Vector3(localX, localAxis1, 0f)
            : new Vector3(localX, 0f, localAxis1);

        return transform.TransformPoint(local);
    }

    // gridCoord에 기본 철광석 자원 노드(1칸)를 배치한다. 성공 시 true.
    public bool TryPlaceResourceNode(Vector2Int gridCoord)
    {
        return TryPlaceResourceNode(gridCoord, null);
    }

    // gridCoord에 자원 노드를 배치한다. 선택적으로 itemId를 주입한다. 성공 시 true.
    public bool TryPlaceResourceNode(Vector2Int gridCoord, string itemId)
    {
        if (!CanPlaceResourceNodeAt(gridCoord))
        {
            return false;
        }

        if (resourceNodePrefab == null)
        {
            Debug.LogWarning("GridManager: resourceNodePrefab이 할당되지 않았습니다.");
            return false;
        }

        Transform parent = GetResourceNodesRoot();
        Vector3 worldPosition = GridToWorld(gridCoord);

        GameObject instance = Instantiate(resourceNodePrefab, worldPosition, Quaternion.identity, parent);
        instance.name = $"ResourceNode_{gridCoord.x}_{gridCoord.y}";

        ResourceNode node = instance.GetComponent<ResourceNode>();
        if (node == null)
        {
            node = instance.AddComponent<ResourceNode>();
        }

        node.Initialize(gridCoord, itemId);
        placedResourceNodes.Add(node);
        SetResourceNodeSpriteVisible(instance, false);

        GridCell cell = GetCell(gridCoord);
        cell.Occupant = instance;
        cell.OccupantKind = OccupantKind.ResourceNode;
        cell.ResourceItemId = string.IsNullOrEmpty(itemId) ? "iron_ore" : itemId;
        cell.ResourceNodeVisible = true;
        SetCell(gridCoord, cell);

        return true;
    }

    // 미해금 구역 등에서 광석 타일·노드 표시를 토글한다.
    public void SetResourceNodeVisible(Vector2Int gridCoord, bool visible)
    {
        if (!IsInBounds(gridCoord.x, gridCoord.y))
        {
            return;
        }

        GridCell cell = GetCell(gridCoord);
        if (!cell.HasResourceNode)
        {
            return;
        }

        cell.ResourceNodeVisible = visible;
        SetCell(gridCoord, cell);
    }

    // gridCoord의 자원 노드를 제거하고 지형을 Floor/Locked로 되돌린다. 성공 시 true.
    // 해금되지 않은 구역에 채굴기가 있으면 먼저 기계부터 제거한다.
    public bool TryRemoveResourceNode(Vector2Int gridCoord)
    {
        if (!TryGetResourceNodeAt(gridCoord, out ResourceNode resourceNode))
        {
            return false;
        }

        GridCell cell = GetCell(gridCoord);
        if (cell.OccupantKind == OccupantKind.MachineOnResourceNode)
        {
            Machine machine = GetMachineAt(gridCoord);
            if (machine != null && !TryRemoveMachine(machine))
            {
                return false;
            }
        }

        placedResourceNodes.Remove(resourceNode);
        Destroy(resourceNode.gameObject);

        cell = GetCell(gridCoord);
        cell.Occupant = null;
        cell.OccupantKind = default;
        cell.ResourceItemId = null;
        cell.ResourceNodeVisible = false;
        cell.Type = GetGroundTypeForCoord(gridCoord);
        SetCell(gridCoord, cell);

        return true;
    }

    // gridCoord에 자원 노드를 놓을 수 있는지 확인한다 (범위·Floor/Locked·미점유).
    // Locked에도 허용하는 이유: 해금 전 미리 배치해 두고, 해금 후 채굴만 연다.
    public bool CanPlaceResourceNodeAt(Vector2Int gridCoord)
    {
        if (!IsInBounds(gridCoord.x, gridCoord.y))
        {
            return false;
        }

        GridCell cell = GetCell(gridCoord);
        if (cell.IsOccupied)
        {
            return false;
        }

        if (cell.HasResourceNode)
        {
            return false;
        }

        return cell.Type == GridCellType.Floor || cell.Type == GridCellType.Locked;
    }

    // coord의 바닥 타일 종류. 광석 노드는 별도 타일맵 레이어로 표시한다.
    private static GridCellType GetGroundTypeForCoord(Vector2Int coord)
    {
        return IsCoordInUnlockedZone(coord) ? GridCellType.Floor : GridCellType.Locked;
    }

    private static void SetResourceNodeSpriteVisible(GameObject instance, bool visible)
    {
        if (instance == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }

    // coord가 속한 구역이 해금됐는지 확인한다. ZoneManager가 없으면 시작 구역만 해금으로 본다.
    private static bool IsCoordInUnlockedZone(Vector2Int coord)
    {
        Vector2Int zone = ZoneManager.GetZoneIndex(coord.x, coord.y);
        if (ZoneManager.Instance != null)
        {
            return ZoneManager.Instance.IsZoneUnlocked(zone.x, zone.y);
        }

        return zone.x == ZoneManager.CenterZoneX && zone.y == ZoneManager.CenterZoneY;
    }

    // worldPosition이 가리키는 그리드 셀을 기준으로 footprint anchor(좌하단)를 반환한다.
    // 홀수 footprint는 셀 중심, 짝수 footprint는 그리드 경계(셀 교차점)에 맞춘다.
    public Vector2Int GetAnchorForCenteredFootprint(Vector3 worldPosition, Vector2Int footprintSize)
    {
        Vector2Int gridCell = WorldToGrid(worldPosition);
        return new Vector2Int(
            gridCell.x - footprintSize.x / 2,
            gridCell.y - footprintSize.y / 2);
    }

    // anchor·footprintSize 영역의 배치 기준 월드 좌표를 반환한다.
    // 홀수 footprint는 중앙 셀 중심, 짝수 footprint는 그리드 경계 교차점에 맞춘다.
    public Vector3 GetFootprintCenterWorld(Vector2Int anchor, Vector2Int footprintSize)
    {
        float cellSize = CellSize;
        float localX = GetFootprintCenterLocalAxis(anchor.x, footprintSize.x, tilePivot.x, cellSize);
        float localAxis1 = GetFootprintCenterLocalAxis(anchor.y, footprintSize.y, tilePivot.y, cellSize);

        Vector3 local = plane == GridPlane.XY
            ? new Vector3(localX, localAxis1, 0f)
            : new Vector3(localX, 0f, localAxis1);

        return transform.TransformPoint(local);
    }

    // footprint 축별 배치 기준 로컬 좌표. 홀수는 셀 중심, 짝수는 그리드 경계.
    private static float GetFootprintCenterLocalAxis(int anchorAxis, int footprintAxis, float pivot, float cellSize)
    {
        if (footprintAxis % 2 == 1)
        {
            int centerCell = anchorAxis + footprintAxis / 2;
            return (centerCell + pivot) * cellSize;
        }

        return (anchorAxis + footprintAxis / 2) * cellSize;
    }

    // worldPosition(마우스 등) 중심으로 SmelterMachine을 배치한다.
    public bool TryPlaceSmelterMachine(Vector3 worldPosition)
    {
        return TryPlaceMachine(smelterMachinePrefab, worldPosition);
    }

    // worldPosition(마우스 등) 중심으로 AssemblerMachine을 배치한다.
    public bool TryPlaceAssemblerMachine(Vector3 worldPosition)
    {
        return TryPlaceMachine(assemblerMachinePrefab, worldPosition);
    }

    // worldPosition(마우스 등) 중심으로 MinerMachine을 배치한다.
    public bool TryPlaceMinerMachine(Vector3 worldPosition)
    {
        return TryPlaceMachine(minerMachinePrefab, worldPosition);
    }

    // worldPosition이 가리키는 그리드 셀에 배치된 기계를 반환한다.
    public bool TryGetMachineAtWorldPosition(Vector3 worldPosition, out Machine machine)
    {
        machine = null;
        Vector2Int gridCoord = WorldToGrid(worldPosition);

        if (!IsInBounds(gridCoord.x, gridCoord.y))
        {
            return false;
        }

        GridCell cell = GetCell(gridCoord);
        if (cell.Occupant == null)
        {
            return false;
        }

        machine = cell.Occupant.GetComponent<Machine>();
        return machine != null;
    }

    // 맵에서 기계를 제거하고 그리드 점유를 해제한다. 성공 시 true.
    public bool TryRemoveMachine(Machine machine)
    {
        if (machine == null)
        {
            return false;
        }

        Vector2Int anchor = machine.GridAnchor;
        Vector2Int footprintSize = machine.GetFootprintSize();
        OccupantKind clearedKind = machine.GetOccupantKind();

        if (machine is ConveyerBelt removedBelt)
        {
            removedBelt.ClearNeighbors();
        }

        ClearFootprint(anchor, footprintSize, clearedKind);
        placedMachines.Remove(machine);
        RefreshBeltNeighborsAtFootprint(anchor, footprintSize);
        TickManager.Instance?.UnregisterMachine(machine);
        Destroy(machine.gameObject);
        return true;
    }

    // worldPosition(마우스 등) 중심으로 기계를 배치한다. 성공 시 true.
    public bool TryPlaceMachine(
        GameObject machinePrefab,
        Vector3 worldPosition,
        MachineInventoryEntry inventoryEntry = null,
        Vector2Int? beltFlowDirection = null)
    {
        if (machinePrefab == null)
        {
            Debug.LogWarning("GridManager: machinePrefab이 할당되지 않았습니다.");
            return false;
        }

        Machine prefabMachine = machinePrefab.GetComponent<Machine>();
        if (prefabMachine == null)
        {
            Debug.LogWarning("GridManager: machinePrefab에 Machine 컴포넌트가 없습니다.");
            return false;
        }

        Vector2Int footprintSize = prefabMachine.GetFootprintSize();

        Vector2Int anchor = GetAnchorForCenteredFootprint(worldPosition, footprintSize);

        if (!CanPlaceFootprintAt(anchor, footprintSize, prefabMachine))
        {
            return false;
        }

        Transform parent = GetMachinesRoot();
        Vector3 centerWorld = GetFootprintCenterWorld(anchor, footprintSize);

        GameObject instance = Instantiate(machinePrefab, centerWorld, Quaternion.identity, parent);
        instance.name = $"Machine_{anchor.x}_{anchor.y}";

        Machine machine = instance.GetComponent<Machine>();
        if (machine == null)
        {
            machine = instance.AddComponent<PlaceholderMachine>();
        }

        machine.Initialize(anchor);
        if (inventoryEntry != null)
        {
            machine.BindInventoryEntry(inventoryEntry);
        }

        if (beltFlowDirection.HasValue && machine is ConveyerBelt placedBelt)
        {
            placedBelt.SetFlowDirection(beltFlowDirection.Value);
        }

        // 배치 직후 기계별 초기화 훅 호출 (예: 드릴의 전용 레시피 자동 설정).
        machine.InitializeMachine();

        placedMachines.Add(machine);
        TickManager.Instance?.RegisterMachine(machine);

        OccupyFootprint(anchor, footprintSize, instance, machine.GetOccupantKind());
        RefreshBeltNeighborsForMachine(machine);

        return true;
    }

    // anchor부터 footprintSize 영역에 placementMachine을 놓을 수 있는지 확인한다.
    // 벨트 외 기계는 플레이어가 서 있는 칸과 겹치면 배치할 수 없다.
    public bool CanPlaceFootprintAt(Vector2Int anchor, Vector2Int footprintSize, Machine placementMachine)
    {
        bool blocksPlayerCell = placementMachine != null
            && placementMachine.GetOccupantKind() != OccupantKind.Belt;
        Vector2Int? playerCell = blocksPlayerCell ? TryGetPlayerGridCell() : null;

        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                int cellX = anchor.x + x;
                int cellY = anchor.y + y;

                if (!IsInBounds(cellX, cellY))
                {
                    return false;
                }

                Vector2Int coord = new Vector2Int(cellX, cellY);
                if (!placementMachine.IsAvailableCellForMachine(this, coord))
                {
                    return false;
                }

                if (playerCell.HasValue && playerCell.Value == coord)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // 플레이어가 있는 그리드 칸. 없으면 null.
    private Vector2Int? TryGetPlayerGridCell()
    {
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player == null)
        {
            return null;
        }

        return WorldToGrid(player.transform.position);
    }

    // footprint 영역의 그리드 점유를 해제한다. 바닥 타일(Floor/Locked)은 구역 기준으로 유지한다.
    private void ClearFootprint(Vector2Int anchor, Vector2Int footprintSize, OccupantKind clearedKind)
    {
        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int coord = new Vector2Int(anchor.x + x, anchor.y + y);
                GridCell cell = GetCell(coord);

                if (clearedKind == OccupantKind.MachineOnResourceNode
                    && TryGetResourceNodeAt(coord, out ResourceNode resourceNode))
                {
                    cell.Occupant = resourceNode.gameObject;
                    cell.OccupantKind = OccupantKind.ResourceNode;
                    cell.Type = GetGroundTypeForCoord(coord);
                }
                else
                {
                    cell.Occupant = null;
                    cell.OccupantKind = default;
                    cell.Type = GetGroundTypeForCoord(coord);
                }

                SetCell(coord, cell);
            }
        }
    }

    // placedResourceNodes에서 coord에 놓인 자원 노드를 찾는다.
    private bool TryGetResourceNodeAt(Vector2Int coord, out ResourceNode resourceNode)
    {
        for (int i = 0; i < placedResourceNodes.Count; i++)
        {
            ResourceNode node = placedResourceNodes[i];
            if (node != null && node.GridAnchor == coord)
            {
                resourceNode = node;
                return true;
            }
        }

        resourceNode = null;
        return false;
    }

    // footprint 영역 전체 셀이 같은 occupant GameObject를 가리키도록 기록한다. 바닥 타일은 바꾸지 않는다.
    private void OccupyFootprint(Vector2Int anchor, Vector2Int footprintSize, GameObject occupant, OccupantKind occupantKind)
    {
        for (int x = 0; x < footprintSize.x; x++)
        {
            for (int y = 0; y < footprintSize.y; y++)
            {
                Vector2Int coord = new Vector2Int(anchor.x + x, anchor.y + y);
                GridCell cell = GetCell(coord);
                cell.Occupant = occupant;
                cell.OccupantKind = occupantKind;
                SetCell(coord, cell);
            }
        }
    }

    // 기계 배치·제거 시 인접 벨트의 upstream/downstream 캐시와
    // 비벨트 기계의 출력 벨트 목록을 갱신한다.
    private void RefreshBeltNeighborsForMachine(Machine machine)
    {
        RefreshBeltNeighborsAtFootprint(machine.GridAnchor, machine.GetFootprintSize(), machine);
    }

    private void RefreshBeltNeighborsAtFootprint(Vector2Int anchor, Vector2Int footprintSize, Machine changedMachine = null)
    {
        if (changedMachine is ConveyerBelt placedBelt)
        {
            placedBelt.RefreshNeighbors(this);
        }

        for (int i = 0; i < placedMachines.Count; i++)
        {
            if (placedMachines[i] is not ConveyerBelt belt || belt == changedMachine)
            {
                continue;
            }

            Vector2Int upstreamCoord = belt.GridAnchor - belt.FlowDirection;
            Vector2Int downstreamCoord = belt.GridAnchor + belt.FlowDirection;
            if (FootprintContainsCoord(anchor, footprintSize, upstreamCoord)
                || FootprintContainsCoord(anchor, footprintSize, downstreamCoord))
            {
                belt.RefreshNeighbors(this);
            }
        }

        RefreshOutboundBeltsAtFootprint(anchor, footprintSize, changedMachine);
    }

    // 변경 footprint에 인접한 비벨트 기계의 출력 벨트 캐시를 다시 만든다.
    private void RefreshOutboundBeltsAtFootprint(
        Vector2Int anchor,
        Vector2Int footprintSize,
        Machine changedMachine)
    {
        if (changedMachine != null && changedMachine is not ConveyerBelt)
        {
            changedMachine.RefreshOutboundBelts(this);
        }

        for (int i = 0; i < placedMachines.Count; i++)
        {
            Machine machine = placedMachines[i];
            if (machine == null || machine is ConveyerBelt || machine == changedMachine)
            {
                continue;
            }

            if (!IsFootprintAdjacentOrOverlapping(
                    machine.GridAnchor,
                    machine.GetFootprintSize(),
                    anchor,
                    footprintSize))
            {
                continue;
            }

            machine.RefreshOutboundBelts(this);
        }
    }

    // 두 footprint가 겹치거나 한 칸이라도 맞닿아 있으면 true.
    private static bool IsFootprintAdjacentOrOverlapping(
        Vector2Int anchorA,
        Vector2Int sizeA,
        Vector2Int anchorB,
        Vector2Int sizeB)
    {
        int aMinX = anchorA.x;
        int aMaxX = anchorA.x + sizeA.x - 1;
        int aMinY = anchorA.y;
        int aMaxY = anchorA.y + sizeA.y - 1;
        int bMinX = anchorB.x;
        int bMaxX = anchorB.x + sizeB.x - 1;
        int bMinY = anchorB.y;
        int bMaxY = anchorB.y + sizeB.y - 1;

        bool separated =
            aMaxX < bMinX - 1
            || bMaxX < aMinX - 1
            || aMaxY < bMinY - 1
            || bMaxY < aMinY - 1;
        return !separated;
    }

    private static bool FootprintContainsCoord(Vector2Int anchor, Vector2Int footprintSize, Vector2Int coord)
    {
        return coord.x >= anchor.x
            && coord.x < anchor.x + footprintSize.x
            && coord.y >= anchor.y
            && coord.y < anchor.y + footprintSize.y;
    }

    // resourceNodesRoot가 없으면 런타임에 빈 부모를 만든다.
    private Transform GetResourceNodesRoot()
    {
        if (resourceNodesRoot != null)
        {
            return resourceNodesRoot;
        }

        var rootObject = new GameObject("ResourceNodes");
        resourceNodesRoot = rootObject.transform;
        return resourceNodesRoot;
    }

    // machinesRoot가 없으면 런타임에 빈 부모를 만든다.
    private Transform GetMachinesRoot()
    {
        if (machinesRoot != null)
        {
            return machinesRoot;
        }

        var rootObject = new GameObject("Machines");
        machinesRoot = rootObject.transform;
        return machinesRoot;
    }
}
