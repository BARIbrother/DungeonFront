using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int WorkHash = Animator.StringToHash("Work");
    private static readonly int RepairHash = Animator.StringToHash("Repair");

    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Animator animator;

    // 1키 기계 지급 UI에 쓰는 MachineDatabase.
    [SerializeField] private MachineDatabase machineDatabase;

    // 0키로 철광석 노드 배치 모드를 토글한다.
    private bool isResourceNodePlacementMode;
    // 초당 이동 픽셀 수
    [SerializeField] private float pixelsPerSecond = 96f;
    // 픽셀당 월드 유닛 (GridManager PixelsPerUnit 기본값)
    [SerializeField] private float pixelsPerUnit = 32f;

    private Vector2 lastFacing = Vector2.down;

    void Start()
    {
        if (gridManager != null)
        {
            transform.position = gridManager.GetMapCenterWorld();
        }

        UpdateAnimator(Vector2.zero);
    }

    void Update()
    {
        // 모달이 열려 있으면 이동·상호작용을 잠근다.
        if (ProductionSummaryUI.IsOpen || MachineGrantUI.IsOpen)
        {
            UpdateAnimator(Vector2.zero);
            return;
        }

        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        }

        if (keyboard != null)
        {
            // 1: MachineDatabase 기반 기계 지급 UI
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                TryOpenMachineGrantUi();
            }

            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                isResourceNodePlacementMode = !isResourceNodePlacementMode;
                Debug.Log($"[PlayerMovement] 철광석 노드 배치 모드: {(isResourceNodePlacementMode ? "ON" : "OFF")}");
            }

            // F: 생산 즉시 종료 (E는 수리·수작업)
            if (keyboard.fKey.wasPressedThisFrame)
            {
                TryForceEndProduction();
            }
        }

        if (isResourceNodePlacementMode)
        {
            TryPlaceResourceNodeAtMouse();
        }

        TryInteractNearbyMachine(keyboard);

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        UpdateAnimator(input);

        float speed = pixelsPerSecond / pixelsPerUnit;
        Vector3 delta = new Vector3(input.x, input.y, 0f) * speed * Time.deltaTime;
        Vector3 next = transform.position + delta;

        if (gridManager != null)
        {
            next = gridManager.ClampWorldPosition(next);
        }

        transform.position = next;
    }

    // E키: 근접 1칸 내 고장 기계 수리 우선, 없으면 수작업 기계 진도.
    private void TryInteractNearbyMachine(Keyboard keyboard)
    {
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (IsPlacementInteractionBlocking())
        {
            return;
        }

        if (gridManager == null)
        {
            return;
        }

        Machine brokenTarget = FindNearestMachineWithinOneCell(machine => machine.IsBroken);
        if (brokenTarget != null)
        {
            if (TryRepairNearbyMachine(brokenTarget))
            {
                TrySetAnimatorTrigger(RepairHash);
            }

            return;
        }

        Machine manualTarget = FindNearestMachineWithinOneCell(machine => machine.SupportsManualWorkClick());
        if (manualTarget == null)
        {
            return;
        }

        if (!manualTarget.TryAdvanceManualClick())
        {
            return;
        }

        TrySetAnimatorTrigger(WorkHash);
    }

    private static bool TryRepairNearbyMachine(Machine machine)
    {
        if (ProductionEventManager.Instance != null)
        {
            return ProductionEventManager.Instance.TryRepairMachine(machine);
        }

        if (machine == null || !machine.IsBroken)
        {
            return false;
        }

        machine.SetBroken(false);
        return true;
    }

    // Animator에 해당 Trigger 파라미터가 있을 때만 설정한다.
    private void TrySetAnimatorTrigger(int triggerHash)
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger
                && parameter.nameHash == triggerHash)
            {
                animator.SetTrigger(triggerHash);
                return;
            }
        }
    }

    // 플레이어 셀 기준 Chebyshev 거리 1 이내이며 predicate를 만족하는 가장 가까운 기계.
    private Machine FindNearestMachineWithinOneCell(System.Func<Machine, bool> predicate)
    {
        Vector2Int playerCell = gridManager.WorldToGrid(transform.position);
        Machine nearest = null;
        int bestDistanceSq = int.MaxValue;

        System.Collections.Generic.IReadOnlyList<Machine> machines =
            TickManager.Instance != null
                ? TickManager.Instance.MachinesOnGrid
                : FindObjectsByType<Machine>(FindObjectsInactive.Exclude);

        for (int i = 0; i < machines.Count; i++)
        {
            Machine machine = machines[i];
            if (machine == null || predicate == null || !predicate(machine))
            {
                continue;
            }

            if (!TryGetChebyshevDistanceToFootprint(playerCell, machine, out int distance))
            {
                continue;
            }

            if (distance > 1)
            {
                continue;
            }

            Vector2Int footprintCenter = machine.GridAnchor
                + new Vector2Int(
                    (machine.GetFootprintSize().x - 1) / 2,
                    (machine.GetFootprintSize().y - 1) / 2);
            int dx = playerCell.x - footprintCenter.x;
            int dy = playerCell.y - footprintCenter.y;
            int distanceSq = dx * dx + dy * dy;

            if (nearest == null || distanceSq < bestDistanceSq)
            {
                nearest = machine;
                bestDistanceSq = distanceSq;
            }
        }

        return nearest;
    }

    // 플레이어 셀과 기계 footprint 임의 셀 사이의 최소 Chebyshev 거리를 구한다.
    private static bool TryGetChebyshevDistanceToFootprint(
        Vector2Int playerCell,
        Machine machine,
        out int distance)
    {
        distance = int.MaxValue;
        if (machine == null)
        {
            return false;
        }

        Vector2Int anchor = machine.GridAnchor;
        Vector2Int footprint = machine.GetFootprintSize();
        for (int x = 0; x < footprint.x; x++)
        {
            for (int y = 0; y < footprint.y; y++)
            {
                int chebyshev = Mathf.Max(
                    Mathf.Abs(playerCell.x - (anchor.x + x)),
                    Mathf.Abs(playerCell.y - (anchor.y + y)));
                if (chebyshev < distance)
                {
                    distance = chebyshev;
                }
            }
        }

        return distance != int.MaxValue;
    }

    private static bool IsPlacementInteractionBlocking()
    {
        PlacementController placementController = FindAnyObjectByType<PlacementController>();
        return placementController != null && placementController.IsPlacementMode;
    }

    // MoveX/MoveY로 4방향 idle·walk를 Animator에 전달한다. 대각선은 지배 축 하나만 사용한다.
    private void UpdateAnimator(Vector2 input)
    {
        if (animator == null)
        {
            return;
        }

        bool isMoving = input.sqrMagnitude > 0f;
        Vector2 facing = lastFacing;

        if (isMoving)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                facing = new Vector2(Mathf.Sign(input.x), 0f);
            }
            else
            {
                facing = new Vector2(0f, Mathf.Sign(input.y));
            }

            lastFacing = facing;
        }

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetFloat(MoveXHash, facing.x);
        animator.SetFloat(MoveYHash, facing.y);
    }

    // 0키 배치 모드에서 마우스가 가리키는 그리드 칸에 철광석 노드를 놓는다.
    private void TryPlaceResourceNodeAtMouse()
    {
        if (gridManager == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector3 screen = mouse.position.ReadValue();
        screen.z = -camera.transform.position.z;
        Vector3 mouseWorld = camera.ScreenToWorldPoint(screen);
        mouseWorld.z = 0f;

        Vector2Int gridCoord = gridManager.WorldToGrid(mouseWorld);
        if (gridManager.TryPlaceResourceNode(gridCoord))
        {
            Debug.Log($"[PlayerMovement] 철광석 노드 배치 성공: ({gridCoord.x}, {gridCoord.y})");
        }
    }

    // F키로 생산을 즉시 종료한다. GameSessionState가 없어도 요약 UI는 띄운다.
    private void TryForceEndProduction()
    {
        if (ProductionSummaryUI.IsOpen)
        {
            return;
        }

        if (GameSessionState.Instance != null)
        {
            if (GameSessionState.Instance.Phase != GamePhase.Production)
            {
                Debug.LogWarning(
                    $"[PlayerMovement] F키 무시: 현재 페이즈={GameSessionState.Instance.Phase}. " +
                    "Production일 때만 즉시 종료됩니다. (시작 버튼 또는 T키)");
                return;
            }

            GameSessionState.Instance.ForceEndProduction();
            return;
        }

        // ProductionScene만 단독 플레이할 때: 세션 없이 요약 모달만 연다.
        Debug.Log("[PlayerMovement] GameSessionState 없음 — 요약 UI만 표시합니다.");
        ProductionEndHandler.EndProduction();
    }

    // MachineDatabase 목록으로 기계 지급 UI를 연다.
    private void TryOpenMachineGrantUi()
    {
        if (machineDatabase == null)
        {
            Debug.LogWarning("[PlayerMovement] MachineDatabase가 할당되지 않아 지급 UI를 열 수 없습니다.");
            return;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[PlayerMovement] PlayerInventory가 없어 지급 UI를 열 수 없습니다.");
            return;
        }

        MachineGrantUI.Show(machineDatabase, inventory);
    }

    private PlayerInventory GetPlayerInventory()
    {
        if (playerInventory != null)
        {
            return playerInventory;
        }

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            return playerInventory;
        }

        playerInventory = FindAnyObjectByType<PlayerInventory>();
        if (playerInventory != null)
        {
            return playerInventory;
        }

        playerInventory = gameObject.AddComponent<PlayerInventory>();
        return playerInventory;
    }
}
