using UnityEngine;

public class ConveyerBelt : Machine
{
    public const int TicksPerCell = 10;

    [SerializeField] private Vector2Int flowDirection = Vector2Int.right;

    private ItemEntry heldItem;
    private int progressTicks;
    private ConveyerBeltItemView itemView;
    private GridManager cachedGridManager;
    private Machine upstreamMachine;
    private Machine downstreamMachine;

    public Vector2Int FlowDirection => flowDirection;

    public bool HasHeldItem => heldItem != null && heldItem.item != null && heldItem.count > 0;

    public ItemDefinition HeldItemDefinition => heldItem?.item;

    // 벨트 칸 내 진행도 (0=입구, TicksPerCell=출구). 시각화에 사용한다.
    public int ProgressTicks => progressTicks;

    public float NormalizedProgress => HasHeldItem ? progressTicks / (float)TicksPerCell : 0f;

    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 1);

    public override OccupantKind GetOccupantKind() => OccupantKind.Belt;

    private void Awake()
    {
        size = GetFootprintSize();
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

    public void ClearNeighbors()
    {
        upstreamMachine = null;
        downstreamMachine = null;
    }

    public override void TickLogistics()
    {
        if (!HasHeldItem)
        {
            TryPullFromUpstreamMachine();
            return;
        }

        if (progressTicks >= TicksPerCell)
        {
            if (TryPushToDownstream())
            {
                heldItem = null;
                progressTicks = 0;
            }

            return;
        }

        progressTicks++;
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
        if (HasHeldItem || item == null || item.item == null || item.count <= 0)
        {
            return false;
        }

        heldItem = new ItemEntry { item = item.item, count = item.count };
        progressTicks = 0;

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
        return true;
    }

    // 캐시된 upstream 기계 outputPort에서만 당긴다.
    private void TryPullFromUpstreamMachine()
    {
        if (upstreamMachine?.outputPort == null)
        {
            return;
        }

        if (upstreamMachine.outputPort.TryTakeFirst(out ItemEntry taken))
        {
            heldItem = taken;
            progressTicks = 0;
            itemView?.InheritWorldPosition(GetItemWorldPosition(0f));
            itemView?.ApplyItemSprite(taken.item);
        }
    }

    private bool TryPushToDownstream()
    {
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
}
