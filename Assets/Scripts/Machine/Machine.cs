using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Machine : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    // 그리드 footprint 크기 (가로×세로 셀 수)
    public Vector2Int size = new Vector2Int(1, 1);

    // GridManager가 배치한 footprint 좌하단 anchor
    private Vector2Int gridAnchor;

    // 배치 시 인벤 MachineInventoryEntry와 연결해 회수 시 동일 instanceId를 복원한다.
    private string inventoryInstanceId;
    private ItemDef_Machine machineDefinition;

    public Vector2Int GridAnchor => gridAnchor;
    public bool HasInventoryBinding =>
        !string.IsNullOrEmpty(inventoryInstanceId) && machineDefinition != null;

    // 배치·점유 계산에 쓰는 footprint 크기. 서브클래스에서 고정값을 반환한다.
    public virtual Vector2Int GetFootprintSize() => size;

    public ItemEntryList inputPort;
    public ItemEntryList outputPort;
    public Recipe currentRecipe;

    // 이 기계가 선택할 수 있는 레시피 목록. 그중 하나만 currentRecipe로 사용한다.
    [SerializeField] private RecipePool AvailableRecipes;
    [SerializeField] private Recipe selectedRecipe;

    // 자동 생산 WIP. 시작 시 입력 소비, 완료 시 출력 생성.
    protected int progressTicks;
    protected bool hasActiveWip;
    private bool isBroken;

    // SetBroken 틴트 전 원래 SpriteRenderer 색.
    private Color? defaultSpriteColor;
    private static readonly Color BrokenTintColor = new Color(1f, 0.35f, 0.35f, 1f);

    // 1틱(또는 수작업 1클릭)마다 더해지는 진행도.
    public int workSpeed = 1;

    public bool IsBroken => isBroken;

    public abstract void InitializeMachine();

    public virtual void PutintoInputPort(ItemEntry IE)
    {
        inputPort?.TryAddToRecipeInput(IE, currentRecipe);
    }

    public virtual void TakeoutOutputPort(ItemEntry IE)
    {
        outputPort?.TryTake(IE);
    }

    // 물류 페이즈 훅. 컨베이어 등이 override한다.
    public virtual void TickLogistics()
    {
        if (isBroken)
        {
            return;
        }
    }

    public void SetBroken(bool broken)
    {
        isBroken = broken;
        ApplyBrokenVisual();
    }

    // 고장 시 붉은 틴트, 수리 시 원래 색으로 되돌린다.
    private void ApplyBrokenVisual()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        if (!defaultSpriteColor.HasValue)
        {
            defaultSpriteColor = spriteRenderer.color;
        }

        if (isBroken)
        {
            Color baseColor = defaultSpriteColor.Value;
            spriteRenderer.color = new Color(
                BrokenTintColor.r,
                BrokenTintColor.g,
                BrokenTintColor.b,
                baseColor.a);
            return;
        }

        spriteRenderer.color = defaultSpriteColor.Value;
    }

    // 생산 완료 페이즈 공통 로직. 이번 틱에 출력이 산출되면 true를 반환한다.
    protected bool CompleteProductionTick()
    {
        return AdvanceProductionWork(workSpeed);
    }

    // workAmount만큼 진행도를 올리고, recipeTime 이상이면 출력을 산출한다.
    protected bool AdvanceProductionWork(int workAmount)
    {
        if (isBroken)
        {
            return false;
        }

        if (!hasActiveWip || currentRecipe == null)
        {
            return false;
        }

        if (progressTicks < currentRecipe.recipeTime)
        {
            if (workAmount > 0)
            {
                progressTicks += workAmount;
            }

            if (progressTicks < currentRecipe.recipeTime)
            {
                return false;
            }
        }

        if (outputPort == null || !outputPort.CanFit(currentRecipe.outputEntryList))
        {
            return false;
        }

        if (!outputPort.TryAddOutputs(currentRecipe.outputEntryList))
        {
            return false;
        }

        progressTicks = 0;
        hasActiveWip = false;
        return true;
    }

    // 수작업 기계가 클릭을 레시피 UI 대신 가로채는지 여부. 기본은 레시피 UI 허용.
    public virtual bool HandlesClickAsManualWork() => false;

    // 플레이어 키 입력으로 진행도를 올리는 수작업 기계인지 여부.
    public virtual bool SupportsManualWorkClick() => false;

    // 수작업 1회분 진행. 수작업 기계만 override한다. 기본은 무시.
    public virtual bool TryAdvanceManualClick() => false;

    // 생산 시작 페이즈 공통 로직.
    protected void StartProductionTick()
    {
        if (isBroken)
        {
            return;
        }

        if (hasActiveWip || currentRecipe == null)
        {
            return;
        }

        if (inputPort == null || !inputPort.MatchesRecipe(currentRecipe))
        {
            return;
        }

        if (outputPort == null || !outputPort.CanFit(currentRecipe.outputEntryList))
        {
            return;
        }

        if (!inputPort.TryConsume(currentRecipe.inputEntryList))
        {
            return;
        }

        hasActiveWip = true;
        progressTicks = 0;
    }

    protected void ResetProductionWip()
    {
        progressTicks = 0;
        hasActiveWip = false;
    }

    // 맵에서 회수할 때 포트·진행 중 WIP 재료를 플레이어 인벤으로 돌린다.
    public virtual void ReturnAllContentsToPlayerInventory()
    {
        RefundActiveWipToPlayerInventory();
        ReturnPortContentsToPlayerInventory(inputPort);
        ReturnPortContentsToPlayerInventory(outputPort);
        ResetProductionWip();
    }

    // 생산 종료 요약용: outputPort에 있는 완성품을 복사해 반환한다. 포트는 비우지 않는다.
    public virtual System.Collections.Generic.List<ItemEntry> CollectFinishedGoodsSnapshot()
    {
        return outputPort != null ? outputPort.CopyAllEntries() : new System.Collections.Generic.List<ItemEntry>();
    }

    // 생산 종료: 완성품(outputPort)을 인벤으로 옮기고 포트를 비운다.
    public virtual void TransferFinishedGoodsToPlayerInventory()
    {
        ReturnPortContentsToPlayerInventory(outputPort);
    }

    // 생산 종료: WIP 재료 환원·입력 포트 잔여 반환·WIP 초기화. 완성품은 별도 처리.
    public virtual void RefundNonFinishedContentsToPlayerInventory()
    {
        RefundActiveWipToPlayerInventory();
        ReturnPortContentsToPlayerInventory(inputPort);
        ResetProductionWip();
    }

    // WIP 시작 시 inputPort에서 소비된 레시피 입력을 환원한다.
    // 생산 중 재료는 포트에 남지 않고 hasActiveWip + currentRecipe로만 존재한다.
    protected virtual void RefundActiveWipToPlayerInventory()
    {
        if (!hasActiveWip || currentRecipe?.inputEntryList?.entries == null)
        {
            return;
        }

        foreach (ItemEntry input in currentRecipe.inputEntryList.entries)
        {
            if (input == null || input.item == null || input.count <= 0)
            {
                continue;
            }

            AddToPlayerInventory(new ItemEntry { item = input.item, count = input.count });
        }
    }

    // GridManager가 배치 직후 그리드 anchor를 주입한다.
    public void Initialize(Vector2Int anchor)
    {
        gridAnchor = anchor;
    }

    // PlacementController가 배치 직후 인벤 엔트리를 연결한다.
    public void BindInventoryEntry(MachineInventoryEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        inventoryInstanceId = entry.instanceId;
        machineDefinition = entry.definition;
    }

    // 회수 시 PlayerInventory로 되돌릴 인벤 엔트리를 만든다.
    public MachineInventoryEntry CreateInventoryEntryForPickup()
    {
        if (!HasInventoryBinding)
        {
            return null;
        }

        return new MachineInventoryEntry
        {
            instanceId = inventoryInstanceId,
            definition = machineDefinition,
        };
    }

    // footprint의 coord 칸에 이 기계를 배치할 수 있는지 확인한다.
    public virtual bool IsAvailableCellForMachine(GridManager gridManager, Vector2Int coord)
    {
        GridCell cell = gridManager.GetCell(coord);
        return cell.Type == GridCellType.Floor && !cell.IsOccupied;
    }

    // 그리드에 기록될 occupant 종류.
    public virtual OccupantKind GetOccupantKind() => OccupantKind.Machine;

    private void Start()
    {
        EnsureClickCollider();
    }

    // 클릭 시 레시피 선택 UI를 띄우거나(지원 기계) 포트 내용을 로그한다.
    private void OnMouseDown()
    {
        if (ProductionSummaryUI.IsOpen || IsPointerOverUi() || IsPlacementInteractionBlockingClick())
        {
            return;
        }

        // Production 중 수작업 기계는 PlayerMovement 좌클릭 진도에 맡긴다.
        if (HandlesClickAsManualWork())
        {
            return;
        }

        if (SupportsRecipeSelectionUi())
        {
            MachineRecipeUI.ShowFor(this);
            return;
        }

        LogPortContents();
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // 배치·회수 모드 중에는 기계 클릭을 배치 시스템에 맡긴다.
    private static bool IsPlacementInteractionBlockingClick()
    {
        PlacementController placementController = FindAnyObjectByType<PlacementController>();
        return placementController != null && placementController.IsPlacementMode;
    }

    private void LogPortContents()
    {
        var log = new StringBuilder();
        log.AppendLine("InputPort:");
        AppendPortLines(log, inputPort);
        log.AppendLine("OutputPort:");
        AppendPortLines(log, outputPort);
        Debug.Log(log.ToString());
    }

    private static void AppendPortLines(StringBuilder log, ItemEntryList port)
    {
        if (port?.entries == null || port.entries.Length == 0)
        {
            return;
        }

        foreach (ItemEntry entry in port.entries)
        {
            if (entry == null || entry.item == null)
            {
                continue;
            }

            string itemName = string.IsNullOrEmpty(entry.item.displayName)
                ? entry.item.id
                : entry.item.displayName;
            log.AppendLine($"{itemName} : {entry.count}개");
        }
    }

    // 로그용으로 포트·레시피 출력 슬롯을 한 줄 문자열로 만든다.
    protected static string DescribePortEntries(ItemEntryList list)
    {
        if (list?.entries == null)
        {
            return "(없음)";
        }

        var builder = new StringBuilder();
        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            string itemName = string.IsNullOrEmpty(entry.item.displayName)
                ? entry.item.id
                : entry.item.displayName;
            builder.Append($"{itemName} x{entry.count}");
        }

        return builder.Length > 0 ? builder.ToString() : "(없음)";
    }

    // OnMouseDown용 BoxCollider2D가 없으면 footprint 크기에 맞춰 추가한다.
    private void EnsureClickCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        var boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(size.x, size.y);
    }

    public RecipePool GetAvailableRecipes() => AvailableRecipes;

    public Recipe GetSelectedRecipe() => selectedRecipe;

    // 레시피 선택 UI를 띄울 수 있는지 여부. 채굴기 등은 override로 막는다.
    public virtual bool SupportsRecipeSelectionUi()
    {
        RecipePool pool = GetAvailableRecipes();
        return pool != null && pool.recipes != null && pool.recipes.Length > 0;
    }

    public string GetMachineDisplayName()
    {
        if (machineDefinition != null && !string.IsNullOrEmpty(machineDefinition.displayName))
        {
            return machineDefinition.displayName;
        }

        return name;
    }

    // AvailableRecipes에 포함된 레시피만 선택해 currentRecipe로 적용한다.
    public virtual void SelectRecipe(Recipe recipe)
    {
        if (recipe != null && AvailableRecipes != null && !AvailableRecipes.Contains(recipe))
        {
            Debug.LogWarning($"[Machine] 레시피 '{recipe.id}'는 AvailableRecipes에 없습니다.");
            return;
        }

        selectedRecipe = recipe;
        ChangeRecipe(recipe);
    }

    // selectedRecipe 또는 풀의 첫 레시피를 적용한다.
    protected void ApplySelectedRecipe()
    {
        Recipe recipe = selectedRecipe;
        if (recipe == null && AvailableRecipes != null)
        {
            recipe = AvailableRecipes.GetFirst();
        }

        if (recipe != null)
        {
            SelectRecipe(recipe);
        }
    }

    public virtual void ChangeRecipe(Recipe newRecipe)
    {
        if (newRecipe != null && AvailableRecipes != null && !AvailableRecipes.Contains(newRecipe))
        {
            Debug.LogWarning($"[Machine] 레시피 '{newRecipe.id}'는 AvailableRecipes에 없습니다.");
            return;
        }

        var savedInputItems = inputPort != null ? inputPort.CopyAllEntries() : null;
        var savedOutputItems = outputPort != null ? outputPort.CopyAllEntries() : null;
        ResetProductionWip();

        currentRecipe = newRecipe;

        EnsurePortLists();

        int inputLength = newRecipe != null && newRecipe.inputEntryList != null
            ? newRecipe.inputEntryList.length
            : 0;
        int outputLength = newRecipe != null && newRecipe.outputEntryList != null
            ? newRecipe.outputEntryList.length
            : 0;

        inputPort.length = inputLength;
        outputPort.length = outputLength;
        inputPort.Resize();
        outputPort.Resize();

        RestoreInputPortItems(savedInputItems);
        RestoreOutputPortItems(savedOutputItems);
    }

    private void EnsurePortLists()
    {
        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }
    }

    private void RestoreInputPortItems(System.Collections.Generic.List<ItemEntry> savedItems)
    {
        if (savedItems == null)
        {
            return;
        }

        for (int i = 0; i < savedItems.Count; i++)
        {
            ItemEntry entry = savedItems[i];
            if (!inputPort.TryAddToRecipeInput(entry, currentRecipe))
            {
                ReturnEntryToPlayerInventory(entry);
            }
        }
    }

    private void RestoreOutputPortItems(System.Collections.Generic.List<ItemEntry> savedItems)
    {
        if (savedItems == null)
        {
            return;
        }

        for (int i = 0; i < savedItems.Count; i++)
        {
            ItemEntry entry = savedItems[i];
            if (!outputPort.TryAdd(entry))
            {
                ReturnEntryToPlayerInventory(entry);
            }
        }
    }

    private void ReturnEntryToPlayerInventory(ItemEntry entry)
    {
        if (entry == null || entry.item == null || entry.count <= 0)
        {
            return;
        }

        var inventory = playerInventory != null
            ? playerInventory
            : FindAnyObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning($"[Machine] 포트 아이템을 플레이어 인벤으로 돌릴 수 없어 손실됨: {entry.item.id} x{entry.count}");
            return;
        }

        inventory.Add(entry);
    }

    private void ReturnPortContentsToPlayerInventory(ItemEntryList port)
    {
        if (port?.entries == null)
        {
            return;
        }

        foreach (var entry in port.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            AddToPlayerInventory(new ItemEntry { item = entry.item, count = entry.count });
            entry.item = null;
            entry.count = 0;
        }
    }

    protected void AddToPlayerInventory(ItemEntry entry)
    {
        var inventory = playerInventory != null
            ? playerInventory
            : FindAnyObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning($"[Machine] PlayerInventory가 없어 아이템을 돌릴 수 없음: {entry.item.id} x{entry.count}");
            return;
        }

        inventory.Add(entry);
    }
}
