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

    public bool IsPlacementMode => isPlacementMode;
    public MachineInventoryEntry SelectedMachine => selectedMachine;

    public event Action<bool> OnPlacementModeChanged;
    public event Action OnInventoryChanged;

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

        if (!isPlacementMode || selectedMachine == null || gridManager == null)
        {
            return;
        }

        if (selectedMachine.definition == null || selectedMachine.definition.machinePrefab == null)
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
        if (gridManager.TryPlaceMachine(selectedMachine.definition.machinePrefab, mouseWorld))
        {
            playerInventory.TryRemoveMachine(selectedMachine.instanceId, out _);
            selectedMachine = null;
            placementPreview?.Hide();
            OnInventoryChanged?.Invoke();
            placementUI.Refresh();
        }
    }

    private void UpdatePlacementPreview()
    {
        if (placementPreview == null)
        {
            return;
        }

        bool shouldShow = isPlacementMode
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
            GetMouseWorldPosition());
    }

    // Dev 인벤 UI·PlacementUI에서 기계 행 클릭 시 호출한다.
    public void SelectMachine(string instanceId)
    {
        if (!isPlacementMode || playerInventory == null || string.IsNullOrEmpty(instanceId))
        {
            return;
        }

        foreach (MachineInventoryEntry machine in playerInventory.Machines)
        {
            if (machine.instanceId == instanceId)
            {
                selectedMachine = machine;
                placementUI.Refresh();
                return;
            }
        }
    }

    private void SetPlacementMode(bool active)
    {
        isPlacementMode = active;

        if (!active)
        {
            selectedMachine = null;
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

        if (GameFlowController.Instance != null)
        {
            return GameFlowController.Instance.currentPhase == GamePhase.Prepare;
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
}
