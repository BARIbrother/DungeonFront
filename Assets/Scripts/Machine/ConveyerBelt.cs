using UnityEngine;

public class ConveyerBelt : Machine
{
    public const int TicksPerCell = 10;

    [System.Serializable]
    public struct DirectionalSprite
    {
        public string id;
        public Sprite sprite;
    }

    [SerializeField] private Vector2Int flowDirection = Vector2Int.right;
    [SerializeField] private Vector2Int receiveDirection = Vector2Int.left;
    [SerializeField] private DirectionalSprite[] directionalSprites;

    private ItemEntry heldItem;
    private int cellProgressTicks;
    private ConveyerBeltItemView itemView;
    private GridManager cachedGridManager;
    private SpriteRenderer beltRenderer;
    private SpriteRenderer[] turnOverlays;
    // 디버깅용. flowDirection 기준 이전(입구) 기계. RefreshNeighbors가 갱신한다.
    [SerializeField] private Machine upstreamMachine;
    // 디버깅용. flowDirection 기준 다음(출구) 기계. RefreshNeighbors가 갱신한다.
    [SerializeField] private Machine downstreamMachine;

    public const int MaxTurnReceives = 2;

    private static readonly Vector2Int[] TurnReceiveScratch = new Vector2Int[MaxTurnReceives];

    public Vector2Int FlowDirection => flowDirection;

    public Vector2Int ReceiveDirection => receiveDirection;

    public override bool SupportsInventoryTransferUi() => false;

    // 보내는 방향을 설정한다. 이 벨트가 가리키는 목표 벨트 텍스처도 다시 맞춘다.
    public void SetFlowDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        flowDirection = direction;
        receiveDirection = ConveyorBeltArt.StraightReceive(direction);
        transform.rotation = Quaternion.identity;
        RefreshNeighborMachinesFromGrid();
        ApplyBeltVisual();
        RefreshAdjacentBeltVisuals();
        TickManager.Instance?.MarkBeltOrderDirty();
    }

    public void SetDirectionalSprites(DirectionalSprite[] sprites)
    {
        directionalSprites = sprites;
        ApplyBeltVisual();
    }

    public Sprite GetSpriteForDirections(Vector2Int send, Vector2Int? receive = null)
    {
        Vector2Int receiveDir = receive ?? ConveyorBeltArt.StraightReceive(send);
        string id = ConveyorBeltArt.SpriteId(receiveDir, send);
        Sprite sprite = FindDirectionalSprite(id);
        if (sprite != null)
        {
            return sprite;
        }

        return FindDirectionalSprite(ConveyorBeltArt.SpriteId(
            ConveyorBeltArt.StraightReceive(send),
            send));
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

    public bool HasHeldItem => heldItem != null && heldItem.item != null && heldItem.count > 0;

    public Item HeldItem => heldItem?.item;

    public ItemDefinition HeldItemDefinition => heldItem?.item?.definition;

    // 벨트 칸 내 진행도 (0=입구, TicksPerCell=출구). 시각화에 사용한다.
    public new int ProgressTicks => cellProgressTicks;

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

        if (receiveDirection == Vector2Int.zero)
        {
            receiveDirection = ConveyorBeltArt.StraightReceive(flowDirection);
        }

        beltRenderer = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.identity;
        ApplyBeltVisual();

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

    // 생산 종료 요약용: heldItem을 복사해 반환한다. 벨트는 비우지 않는다.
    public override System.Collections.Generic.List<ItemEntry> CollectFinishedGoodsSnapshot()
    {
        var result = new System.Collections.Generic.List<ItemEntry>();
        if (!HasHeldItem)
        {
            return result;
        }

        result.Add(new ItemEntry { item = heldItem.item.Clone(), count = heldItem.count });
        return result;
    }

    // 결산 때 벨트 아이템은 필드에 남긴다. 인벤 이관은 창고만 한다.
    public override void TransferFinishedGoodsToPlayerInventory()
    {
    }

    // 벨트는 WIP·입력 포트가 없으므로 생산 종료 시 추가 환원할 내용이 없다.
    public override void RefundNonFinishedContentsToPlayerInventory()
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
        receiveDirection = ResolveReceiveDirection(gridManager, GridAnchor, flowDirection);
        ApplyBeltVisual();
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
        receiveDirection = ResolveReceiveDirection(gridManager, GridAnchor, flowDirection);
        ApplyBeltVisual();
    }

    private void RefreshAdjacentBeltVisuals()
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            return;
        }

        for (int i = 0; i < ConveyorBeltArt.Cardinals.Length; i++)
        {
            Vector2Int neighbor = GridAnchor + ConveyorBeltArt.Cardinals[i];
            if (gridManager.GetMachineAt(neighbor) is ConveyerBelt other && other != this)
            {
                other.RefreshNeighbors(gridManager);
            }
        }
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

    // normalizedProgress 0=입구, 1=출구에 해당하는 월드 좌표. 코너면 중심을 꺾는다.
    public Vector3 GetItemWorldPosition(float normalizedProgress)
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            return transform.position;
        }

        Vector3 center = gridManager.GetFootprintCenterWorld(GridAnchor, GetFootprintSize());
        float half = gridManager.CellSize * 0.5f;
        Vector2Int receive = receiveDirection == Vector2Int.zero
            ? ConveyorBeltArt.StraightReceive(flowDirection)
            : receiveDirection;
        Vector3 entry = center + ToWorldDirection(gridManager, receive) * half;
        Vector3 exit = center + ToWorldDirection(gridManager, flowDirection) * half;
        float t = Mathf.Clamp01(normalizedProgress);
        if (t <= 0.5f)
        {
            return Vector3.Lerp(entry, center, t * 2f);
        }

        return Vector3.Lerp(center, exit, (t - 0.5f) * 2f);
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

        heldItem = new ItemEntry { item = item.item.Clone(), count = item.count };
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
        if (upstreamMachine == null)
        {
            return;
        }

        if (upstreamMachine.outputPort == null)
        {
            return;
        }

        if (upstreamMachine.TryProvideOutputToBelt(this, out ItemEntry taken))
        {
            heldItem = taken;
            cellProgressTicks = 0;
            itemView?.InheritWorldPosition(GetItemWorldPosition(0f));
            itemView?.ApplyItemSprite(taken.item);
            return;
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

        var pushEntry = new ItemEntry { item = heldItem.item.Clone(), count = heldItem.count };
        return downstreamMachine.PutintoInputPort(pushEntry);
    }

    private GridManager GetGridManager()
    {
        if (cachedGridManager == null)
        {
            cachedGridManager = FindAnyObjectByType<GridManager>();
        }

        return cachedGridManager;
    }

    // 직각으로 들어오는 피더의 입구 방향을 모은다. 순서는 위 → 오른쪽 → 아래 → 왼쪽.
    public static int CollectTurnReceiveDirections(
        GridManager gridManager,
        Vector2Int anchor,
        Vector2Int send,
        Vector2Int[] into)
    {
        if (gridManager == null || into == null || into.Length == 0)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < ConveyorBeltArt.Cardinals.Length && count < into.Length; i++)
        {
            Vector2Int from = ConveyorBeltArt.Cardinals[i];
            Vector2Int feederSend = -from;
            if (!ConveyorBeltArt.IsTurnFeed(feederSend, send))
            {
                continue;
            }

            if (gridManager.GetMachineAt(anchor + from) is ConveyerBelt feeder
                && feeder.FlowDirection == feederSend)
            {
                into[count] = from;
                count++;
                continue;
            }

            if (gridManager.GetMachineAt(anchor + from) is Extractor extractor
                && extractor.FlowDirection == feederSend)
            {
                into[count] = from;
                count++;
            }
        }

        return count;
    }

    // 아이템 경로용 입구. 직각 피더가 있으면 그중 첫 번째, 없으면 직진.
    public static Vector2Int ResolveReceiveDirection(
        GridManager gridManager,
        Vector2Int anchor,
        Vector2Int send)
    {
        int count = CollectTurnReceiveDirections(gridManager, anchor, send, TurnReceiveScratch);
        if (count > 0)
        {
            return TurnReceiveScratch[0];
        }

        return ConveyorBeltArt.StraightReceive(send);
    }

    private void ApplyBeltVisual()
    {
        if (beltRenderer == null)
        {
            beltRenderer = GetComponent<SpriteRenderer>();
        }

        if (beltRenderer == null)
        {
            return;
        }

        Vector2Int straight = ConveyorBeltArt.StraightReceive(flowDirection);
        Sprite baseSprite = GetSpriteForDirections(flowDirection, straight);
        if (baseSprite != null)
        {
            beltRenderer.sprite = baseSprite;
            beltRenderer.color = Color.white;
        }

        transform.rotation = Quaternion.identity;

        int turnCount = 0;
        GridManager gridManager = GetGridManager();
        if (gridManager != null && gridManager.GetMachineAt(GridAnchor) == this)
        {
            turnCount = CollectTurnReceiveDirections(
                gridManager,
                GridAnchor,
                flowDirection,
                TurnReceiveScratch);
        }

        ApplyTurnOverlays(turnCount, TurnReceiveScratch);
    }

    private void ApplyTurnOverlays(int turnCount, Vector2Int[] receives)
    {
        if (turnOverlays == null)
        {
            turnOverlays = new SpriteRenderer[MaxTurnReceives];
        }

        for (int i = 0; i < MaxTurnReceives; i++)
        {
            bool active = i < turnCount;
            SpriteRenderer overlay = turnOverlays[i];
            if (!active)
            {
                if (overlay != null)
                {
                    overlay.enabled = false;
                }

                continue;
            }

            if (overlay == null)
            {
                overlay = CreateTurnOverlay(i);
                turnOverlays[i] = overlay;
            }

            Sprite sprite = GetSpriteForDirections(flowDirection, receives[i]);
            overlay.sprite = sprite;
            overlay.color = Color.white;
            overlay.drawMode = SpriteDrawMode.Simple;
            overlay.sortingLayerID = beltRenderer.sortingLayerID;
            overlay.sortingOrder = beltRenderer.sortingOrder + 1 + i;
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one;
            overlay.enabled = sprite != null;
        }
    }

    private SpriteRenderer CreateTurnOverlay(int index)
    {
        var overlayObject = new GameObject($"TurnOverlay_{index}");
        overlayObject.transform.SetParent(transform, false);
        SpriteRenderer overlay = overlayObject.AddComponent<SpriteRenderer>();
        overlay.color = Color.white;
        overlay.drawMode = SpriteDrawMode.Simple;
        return overlay;
    }

    private Sprite FindDirectionalSprite(string id)
    {
        if (directionalSprites == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < directionalSprites.Length; i++)
        {
            if (directionalSprites[i].id == id && directionalSprites[i].sprite != null)
            {
                return directionalSprites[i].sprite;
            }
        }

        return null;
    }

    private static Vector3 ToWorldDirection(GridManager gridManager, Vector2Int direction)
    {
        Vector3 local = gridManager.Plane == GridPlane.XY
            ? new Vector3(direction.x, direction.y, 0f)
            : new Vector3(direction.x, 0f, direction.y);

        Vector3 world = gridManager.transform.TransformDirection(local);
        if (world.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector3.right;
        }

        return world.normalized;
    }

    private static string DescribeItemEntry(ItemEntry entry)
    {
        if (entry?.item == null || entry.count <= 0)
        {
            return "(없음)";
        }

        string itemName = string.IsNullOrEmpty(entry.item.DisplayName)
            ? entry.item.Id
            : entry.item.DisplayName;
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
