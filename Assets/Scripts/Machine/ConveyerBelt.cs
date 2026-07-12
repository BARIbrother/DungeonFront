using UnityEngine;

public class ConveyerBelt : Machine
{
    public const int TicksPerCell = 10;

    [SerializeField] private Vector2Int flowDirection = Vector2Int.right;

    private ItemEntry heldItem;
    private int cellProgressTicks;
    private ConveyerBeltItemView itemView;
    private GridManager cachedGridManager;
    // 디버깅용. flowDirection 기준 이전(입구) 기계. RefreshNeighbors가 갱신한다.
    [SerializeField] private Machine upstreamMachine;
    // 디버깅용. flowDirection 기준 다음(출구) 기계. RefreshNeighbors가 갱신한다.
    [SerializeField] private Machine downstreamMachine;

    public Vector2Int FlowDirection => flowDirection;

    // 배치 시 R키 회전 등으로 flowDirection을 설정한다. 스프라이트도 같은 각도로 돌린다.
    public void SetFlowDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        flowDirection = direction;
        transform.rotation = Quaternion.Euler(0f, 0f, GetVisualRotationZ(direction));
    }

    // 시계 방향으로 한 칸 회전한 flowDirection을 반환한다.
    public static Vector2Int RotateFlowDirectionClockwise(Vector2Int direction)
    {
        if (direction == Vector2Int.right)
        {
            return Vector2Int.down;
        }

        if (direction == Vector2Int.down)
        {
            return Vector2Int.left;
        }

        if (direction == Vector2Int.left)
        {
            return Vector2Int.up;
        }

        if (direction == Vector2Int.up)
        {
            return Vector2Int.right;
        }

        return Vector2Int.right;
    }

    // 오른쪽을 가리키는 스프라이트를 flowDirection에 맞게 돌릴 Z 각도.
    public static float GetVisualRotationZ(Vector2Int direction)
    {
        if (direction == Vector2Int.right)
        {
            return 0f;
        }

        if (direction == Vector2Int.down)
        {
            return -90f;
        }

        if (direction == Vector2Int.left)
        {
            return 180f;
        }

        if (direction == Vector2Int.up)
        {
            return 90f;
        }

        return 0f;
    }

    public bool HasHeldItem => heldItem != null && heldItem.item != null && heldItem.count > 0;

    public ItemDefinition HeldItemDefinition => heldItem?.item;

    // 벨트 칸 내 진행도 (0=입구, TicksPerCell=출구). 시각화에 사용한다.
    public int ProgressTicks => cellProgressTicks;

    public float NormalizedProgress => HasHeldItem ? cellProgressTicks / (float)TicksPerCell : 0f;

    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 1);

    public override OccupantKind GetOccupantKind() => OccupantKind.Belt;

    private void Awake()
    {
        size = GetFootprintSize();

        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }

        inputPort.length = 1;
        outputPort.length = 1;
        inputPort.Resize();
        outputPort.Resize();

        itemView = GetComponent<ConveyerBeltItemView>();
        if (itemView == null)
        {
            itemView = gameObject.AddComponent<ConveyerBeltItemView>();
        }
    }

    public override void ChangeRecipe(Recipe newRecipe)
    {
        currentRecipe = null;
    }

    public override void InitializeMachine()
    {
    }

    // 벨트 위 아이템을 인벤으로 돌린 뒤 포트 처리는 생략한다.
    public override void ReturnAllContentsToPlayerInventory()
    {
        if (HasHeldItem)
        {
            AddToPlayerInventory(heldItem);
            heldItem = null;
            cellProgressTicks = 0;
        }
    }

    // GridManager가 배치·제거 시 호출해 upstream/downstream을 캐시한다.
    public void RefreshNeighbors(GridManager gridManager)
    {
        if (gridManager == null)
        {
            ClearNeighbors();
            return;
        }

        cachedGridManager = gridManager;

        Vector2Int upstreamCoord = GridAnchor - flowDirection;
        Vector2Int downstreamCoord = GridAnchor + flowDirection;

        Machine upstream = gridManager.GetMachineAt(upstreamCoord);
        upstreamMachine = upstream != null && upstream is not ConveyerBelt ? upstream : null;

        downstreamMachine = gridManager.GetMachineAt(downstreamCoord);
    }

    // pull/push 직전에 flowDirection 기준 이웃 기계 캐시를 그리드에서 다시 읽는다.
    private void RefreshNeighborMachinesFromGrid()
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            upstreamMachine = null;
            downstreamMachine = null;
            return;
        }

        Vector2Int upstreamCoord = GridAnchor - flowDirection;
        Vector2Int downstreamCoord = GridAnchor + flowDirection;

        Machine upstream = gridManager.GetMachineAt(upstreamCoord);
        upstreamMachine = upstream != null && upstream is not ConveyerBelt ? upstream : null;
        downstreamMachine = gridManager.GetMachineAt(downstreamCoord);
    }

    public void ClearNeighbors()
    {
        upstreamMachine = null;
        downstreamMachine = null;
    }

    public override void TickLogistics()
    {
        if (IsBroken)
        {
            return;
        }

        if (!HasHeldItem)
        {
            TryPullFromUpstreamMachine();
            return;
        }

        if (cellProgressTicks >= TicksPerCell)
        {
            if (TryPushToDownstream())
            {
                heldItem = null;
                cellProgressTicks = 0;
            }

            return;
        }

        cellProgressTicks++;
    }

    // 물류 틱이 모두 끝난 뒤 TickManager가 호출한다.
    public void SyncItemVisual()
    {
        itemView?.SyncAfterLogisticsTick();
    }

    public Vector3? GetItemVisualWorldPosition()
    {
        if (itemView == null || !itemView.HasActiveVisual)
        {
            return null;
        }

        return itemView.CurrentWorldPosition;
    }

    // 벨트 간 전달 시 사용할 월드 좌표. 출구에 도달했으면 논리적 출구 좌표를 쓴다.
    public Vector3 GetHandoffWorldPosition()
    {
        if (ProgressTicks >= TicksPerCell)
        {
            return GetItemWorldPosition(1f);
        }

        return GetItemVisualWorldPosition() ?? GetItemWorldPosition(NormalizedProgress);
    }

    // normalizedProgress 0=입구, 1=출구에 해당하는 월드 좌표.
    public Vector3 GetItemWorldPosition(float normalizedProgress)
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            return transform.position;
        }

        Vector3 center = gridManager.GetFootprintCenterWorld(GridAnchor, GetFootprintSize());
        Vector3 flowWorld = GetFlowWorldDirection(gridManager);
        float offset = (normalizedProgress - 0.5f) * gridManager.CellSize;
        return center + flowWorld * offset;
    }

    public float GetCellSize()
    {
        GridManager gridManager = GetGridManager();
        return gridManager != null ? gridManager.CellSize : 1f;
    }

    // 다운스트림 벨트·기계로 아이템을 넘긴다. 입구(progress 0)에서만 수신한다.
    public bool ReceiveItem(ItemEntry item, ConveyerBelt sourceBelt = null)
    {
        if (HasHeldItem)
        {
            // Debug.Log($"[ConveyerBelt] 수신 거부 @ {GridAnchor} : 이미 아이템 보유 중");
            return false;
        }

        if (item == null || item.item == null || item.count <= 0)
        {
            // Debug.Log($"[ConveyerBelt] 수신 거부 @ {GridAnchor} : 유효하지 않은 아이템");
            return false;
        }

        heldItem = new ItemEntry { item = item.item, count = item.count };
        cellProgressTicks = 0;

        if (sourceBelt != null)
        {
            Vector3 handoffPosition = sourceBelt.GetHandoffWorldPosition();
            itemView?.InheritWorldPosition(handoffPosition);
        }
        else
        {
            itemView?.InheritWorldPosition(GetItemWorldPosition(0f));
        }

        itemView?.ApplyItemSprite(item.item);
        // Debug.Log($"[ConveyerBelt] 수신 성공 @ {GridAnchor} : {DescribeItemEntry(item)} from {sourceName}");
        return true;
    }

    // 캐시된 upstream 기계 outputPort에서만 당긴다.
    private void TryPullFromUpstreamMachine()
    {
        RefreshNeighborMachinesFromGrid();

        if (upstreamMachine == null)
        {
            // Debug.Log(
            //     $"[ConveyerBelt] pull 스킵 @ {GridAnchor} : upstream 기계 없음 "
            //     + $"(upstreamCoord={upstreamCoord}, flow={flowDirection}, occupant={DescribeOccupant(occupant)}{downstreamHint})");
            return;
        }

        if (upstreamMachine.outputPort == null)
        {
            // Debug.Log($"[ConveyerBelt] pull 스킵 @ {GridAnchor} : upstream outputPort 없음 ({upstreamMachine.name})");
            return;
        }

        if (upstreamMachine.outputPort.TryTakeFirst(out ItemEntry taken))
        {
            heldItem = taken;
            cellProgressTicks = 0;
            itemView?.InheritWorldPosition(GetItemWorldPosition(0f));
            itemView?.ApplyItemSprite(taken.item);
            // Debug.Log($"[ConveyerBelt] pull 성공 @ {GridAnchor} : {DescribeItemEntry(taken)} from {upstreamMachine.name}");
            return;
        }

        // Debug.Log($"[ConveyerBelt] pull 실패 @ {GridAnchor} : outputPort 비어 있음 ({upstreamMachine.name}, slots={upstreamMachine.outputPort.length})");
    }

    private bool TryPushToDownstream()
    {
        RefreshNeighborMachinesFromGrid();

        if (downstreamMachine == null)
        {
            return false;
        }

        if (downstreamMachine is ConveyerBelt frontBelt)
        {
            return frontBelt.ReceiveItem(heldItem, this);
        }

        var pushEntry = new ItemEntry { item = heldItem.item, count = heldItem.count };
        return downstreamMachine.inputPort != null
            && downstreamMachine.inputPort.TryAddToRecipeInput(pushEntry, downstreamMachine.currentRecipe);
    }

    private GridManager GetGridManager()
    {
        if (cachedGridManager == null)
        {
            cachedGridManager = FindAnyObjectByType<GridManager>();
        }

        return cachedGridManager;
    }

    private Vector3 GetFlowWorldDirection(GridManager gridManager)
    {
        Vector3 localFlow = gridManager.Plane == GridPlane.XY
            ? new Vector3(flowDirection.x, flowDirection.y, 0f)
            : new Vector3(flowDirection.x, 0f, flowDirection.y);

        Vector3 worldFlow = gridManager.transform.TransformDirection(localFlow);
        if (worldFlow.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.right;
        }

        return worldFlow.normalized;
    }

    private static string DescribeItemEntry(ItemEntry entry)
    {
        if (entry?.item == null || entry.count <= 0)
        {
            return "(없음)";
        }

        string itemName = string.IsNullOrEmpty(entry.item.displayName)
            ? entry.item.id
            : entry.item.displayName;
        return $"{itemName} x{entry.count}";
    }

    private static string DescribeOccupant(GameObject occupant)
    {
        if (occupant == null)
        {
            return "없음";
        }

        Machine machine = occupant.GetComponent<Machine>();
        if (machine == null)
        {
            return $"{occupant.name} (Machine 아님)";
        }

        string typeName = machine.GetType().Name;
        if (machine is ConveyerBelt)
        {
            return $"{occupant.name} ({typeName}, upstream pull 대상 아님)";
        }

        return $"{occupant.name} ({typeName})";
    }
}
