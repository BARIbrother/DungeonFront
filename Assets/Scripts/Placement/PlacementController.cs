using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Prepare 단계 B키 배치 모드. 인벤 MachineInventoryEntry 선택 후 GridManager 마우스 위치로 배치한다.
public class PlacementController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Camera mainCamera;

    private PlacementUI placementUI;
    private PlacementPreview placementPreview;
    private MachineInventoryEntry selectedMachine;
    private bool isPlacementMode;
    private bool isPickupMode;
    // 컨베이어 선택 시 R키로 바꿀 배치 방향. 기본은 오른쪽.
    private Vector2Int pendingBeltFlowDirection = Vector2Int.right;

    public bool IsPlacementMode => isPlacementMode;
    public bool IsPickupMode => isPickupMode;
    public MachineInventoryEntry SelectedMachine => selectedMachine;

    public event Action<bool> OnPlacementModeChanged;
    public event Action OnInventoryChanged;
    // 배치 성공 시 machineTypeId(definition.id)와 footprint anchor 그리드 좌표.
    public event Action<string, Vector2Int> OnMachinePlaced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<PlacementController>() != null)
        {
            return;
        }

        var systemObject = new GameObject("PlacementSystem");
        systemObject.AddComponent<PlacementController>();
    }

    private void Awake()
    {
        placementUI = GetComponent<PlacementUI>();
        if (placementUI == null)
        {
            placementUI = gameObject.AddComponent<PlacementUI>();
        }

        placementPreview = GetComponent<PlacementPreview>();
        if (placementPreview == null)
        {
            placementPreview = gameObject.AddComponent<PlacementPreview>();
        }
    }

    private void Start()
    {
        ResolveReferences();
        placementUI.Initialize(this, playerInventory);
        placementUI.SetVisible(false, true);

        if (playerInventory != null)
        {
            playerInventory.OnMachinesChanged += HandleInventoryChanged;
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnMachinesChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
        placementUI.Refresh();
    }

    private void Update()
    {
        if (!IsPreparePhase())
        {
            if (isPlacementMode)
            {
                SetPlacementMode(false);
            }

            placementPreview?.Hide();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
        {
            SetPlacementMode(!isPlacementMode);
        }

        UpdatePlacementPreview();

        if (!isPlacementMode)
        {
            return;
        }

        if (isPickupMode)
        {
            TryPickupMachineAtMouse();
            return;
        }

        if (selectedMachine == null || gridManager == null)
        {
            return;
        }

        if (selectedMachine.definition == null || selectedMachine.definition.machinePrefab == null)
        {
            return;
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && IsSelectedConveyerBelt())
        {
            pendingBeltFlowDirection = ConveyerBelt.RotateFlowDirectionClockwise(pendingBeltFlowDirection);
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (IsPointerOverUi())
        {
            return;
        }

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector2Int? beltFlowDirection = IsSelectedConveyerBelt() ? pendingBeltFlowDirection : null;
        Machine prefabMachine = selectedMachine.definition.machinePrefab.GetComponent<Machine>();
        Vector2Int placeAnchor = prefabMachine != null
            ? gridManager.GetAnchorForCenteredFootprint(mouseWorld, prefabMachine.GetFootprintSize())
            : gridManager.WorldToGrid(mouseWorld);

        if (gridManager.TryPlaceMachine(
            selectedMachine.definition.machinePrefab,
            mouseWorld,
            selectedMachine,
            beltFlowDirection))
        {
            string placedDefinitionId = selectedMachine.definition.id;
            playerInventory.TryRemoveMachine(selectedMachine.instanceId, out _);
            selectedMachine = FindFirstMachineOfType(placedDefinitionId);

            if (selectedMachine == null)
            {
                placementPreview?.Hide();
            }

            OnInventoryChanged?.Invoke();
            OnMachinePlaced?.Invoke(placedDefinitionId, placeAnchor);
            placementUI.Refresh();
        }
    }

    // 회수 모드에서 마우스가 가리키는 맵 기계를 인벤으로 되돌린다.
    private void TryPickupMachineAtMouse()
    {
        if (gridManager == null || playerInventory == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (IsPointerOverUi())
        {
            return;
        }

        Vector3 mouseWorld = GetMouseWorldPosition();
        if (!gridManager.TryGetMachineAtWorldPosition(mouseWorld, out Machine machine))
        {
            return;
        }

        if (TryPickupMachine(machine))
        {
            OnInventoryChanged?.Invoke();
            placementUI.Refresh();
        }
    }

    // 맵 기계를 회수해 인벤으로 되돌린다. 성공 시 true.
    public bool TryPickupMachine(Machine machine)
    {
        if (machine == null || gridManager == null || playerInventory == null)
        {
            return false;
        }

        MachineInventoryEntry entry = machine.CreateInventoryEntryForPickup();
        if (entry == null)
        {
            return false;
        }

        TransferMachineContentsToInventory(machine);

        if (!gridManager.TryRemoveMachine(machine))
        {
            return false;
        }

        playerInventory.ReturnMachine(entry);
        return true;
    }

    // 기계 내부 inputPort·outputPort·WIP 재료를 플레이어 인벤으로 반환한다.
    private void TransferMachineContentsToInventory(Machine machine)
    {
        if (machine == null)
        {
            return;
        }

        machine.ReturnAllContentsToPlayerInventory();
    }

    private void UpdatePlacementPreview()
    {
        if (placementPreview == null)
        {
            return;
        }

        bool shouldShow = isPlacementMode
            && !isPickupMode
            && selectedMachine != null
            && selectedMachine.definition != null
            && selectedMachine.definition.machinePrefab != null
            && gridManager != null
            && !IsPointerOverUi();

        if (!shouldShow)
        {
            placementPreview.Hide();
            return;
        }

        placementPreview.UpdatePreview(
            gridManager,
            selectedMachine.definition.machinePrefab,
            GetMouseWorldPosition(),
            IsSelectedConveyerBelt() ? pendingBeltFlowDirection : null);
    }

    // PlacementUI에서 기계 종류 버튼 클릭 시 호출한다.
    public void SelectMachineDefinition(string definitionId)
    {
        if (!isPlacementMode || playerInventory == null || string.IsNullOrEmpty(definitionId))
        {
            return;
        }

        SetPickupMode(false);

        MachineInventoryEntry machine = FindFirstMachineOfType(definitionId);
        if (machine == null)
        {
            return;
        }

        ResetPendingBeltFlowDirectionIfNeeded(machine.definition);
        selectedMachine = machine;
        placementUI.Refresh();
    }

    // PlacementUI 회수 버튼에서 회수 모드를 토글한다.
    public void TogglePickupMode()
    {
        if (!isPlacementMode)
        {
            return;
        }

        SetPickupMode(!isPickupMode);
    }

    private void SetPickupMode(bool active)
    {
        isPickupMode = active;

        if (active)
        {
            selectedMachine = null;
            placementPreview?.Hide();
        }

        placementUI.Refresh();
    }

    private void SetPlacementMode(bool active)
    {
        isPlacementMode = active;

        if (!active)
        {
            selectedMachine = null;
            isPickupMode = false;
            ResetPendingBeltFlowDirection();
            placementPreview?.Hide();
        }

        placementUI.SetVisible(active);
        placementUI.Refresh();
        OnPlacementModeChanged?.Invoke(active);
    }

    private void ResolveReferences()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("[PlacementController] PlayerInventory를 찾을 수 없습니다. Player에 PlayerInventory를 붙여 주세요.");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private bool IsPreparePhase()
    {
        if (GameSessionState.Instance != null)
        {
            return GameSessionState.Instance.Phase == GamePhase.Prepare;
        }

        return true;
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera == null)
        {
            return Vector3.zero;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return Vector3.zero;
        }

        Vector3 screen = mouse.position.ReadValue();
        screen.z = -camera.transform.position.z;
        Vector3 world = camera.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }

    private bool IsSelectedConveyerBelt()
    {
        return selectedMachine?.definition?.machinePrefab?.GetComponent<ConveyerBelt>() != null;
    }

    private void ResetPendingBeltFlowDirection()
    {
        pendingBeltFlowDirection = Vector2Int.right;
    }

    // 컨베이어 연속 배치 시 회전을 유지하고, 다른 종류 선택 시에만 초기화한다.
    private void ResetPendingBeltFlowDirectionIfNeeded(ItemDef_Machine newDefinition)
    {
        bool newIsBelt = newDefinition?.machinePrefab?.GetComponent<ConveyerBelt>() != null;
        if (!newIsBelt || !IsSelectedConveyerBelt())
        {
            ResetPendingBeltFlowDirection();
        }
    }

    private MachineInventoryEntry FindFirstMachineOfType(string definitionId)
    {
        if (playerInventory == null || string.IsNullOrEmpty(definitionId))
        {
            return null;
        }

        foreach (MachineInventoryEntry machine in playerInventory.Machines)
        {
            if (machine?.definition != null && machine.definition.id == definitionId)
            {
                return machine;
            }
        }

        return null;
    }
}
