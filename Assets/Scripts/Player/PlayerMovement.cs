using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

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

    // 1키 기계 구매 UI에 쓰는 MachineDatabase.
    [SerializeField] private MachineDatabase machineDatabase;

    public MachineDatabase MachineDatabase => machineDatabase;

    // 0키로 철광석 노드 배치 모드를 토글한다.
    private bool isResourceNodePlacementMode;
    // 초당 이동 픽셀 수
    [SerializeField] private float pixelsPerSecond = 144f;
    // 픽셀당 월드 유닛 (GridManager PixelsPerUnit 기본값)
    [SerializeField] private float pixelsPerUnit = 32f;
    // 플레이어 발자국 충돌 반경(칸). Locked·노드 칸에 끼지 않도록 셀보다 작게 잡는다.
    // 이동 가능 판정은 중심 칸만 보므로, 이 값은 맵 경계 클램프에만 쓴다.
    [SerializeField] [Range(0.05f, 0.49f)] private float walkCollisionHalfExtentCells = 0.2f;

    private Vector2 lastFacing = Vector2.down;
    // 전진(P_MoveForth) 기준 스프라이트라, 오른쪽을 볼 때 flipX로 반전한다.
    private bool flipSpriteX;
    private SpriteRenderer spriteRenderer;
    private bool repairAnimPending;
    private float repairAnimUntil;
    private Machine pendingRepairMachine;
    private bool pendingRepairApplied;
    private const float RepairAnimTimeout = 1.35f;
    // P_Repair_10(망치 내려치며 멈춤) 시작 시점 = 9/12
    private const float RepairImpactNormalizedTime = 9f / 12f;
    private const float HammerSwingDuration = 1f;

    private enum HammerTargetKind
    {
        Air,
        HandmadeMachine,
        ManualMachine,
        NormalMachine,
    }
    private bool footstepsPlaying;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (gridManager != null)
        {
            // 시작 구역(zone_start) 중앙.
            Vector2Int startCenter = new Vector2Int(
                ZoneManager.CenterZoneX * ZoneManager.ZoneSize + ZoneManager.ZoneSize / 2,
                ZoneManager.CenterZoneY * ZoneManager.ZoneSize + ZoneManager.ZoneSize / 2);
            if (gridManager.IsInBounds(startCenter.x, startCenter.y))
            {
                transform.position = gridManager.GridToWorld(startCenter);
            }
            else
            {
                transform.position = gridManager.GetMapCenterWorld();
            }
        }

        UpdateAnimator(Vector2.zero);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        // 2: 구역 해금 UI (모달 중에도 토글 가능, 상단/키패드 2)
        if (keyboard != null
            && (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame))
        {
            ZoneExpansionUI.Toggle();
        }

        // T: 테크 트리
        if (keyboard != null && keyboard.tKey.wasPressedThisFrame)
        {
            TechTreeUI.Toggle();
        }

        // E: 인벤토리 토글 (다른 모달이 열려 있어도 가능)
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            InventoryUI.Toggle();
        }

        // K: 레시피북 토글 (다른 키와 겹치지 않는 키로 배정)
        if (keyboard != null && keyboard.kKey.wasPressedThisFrame)
        {
            RecipeBookUI.Toggle();
        }

        // 1: 기계 제작 UI 토글. Shift+1은 기존 지급 치트.
        if (keyboard != null
            && (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame))
        {
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                TryToggleMachineGrantUi();
            }
            else
            {
                TryToggleMachineCraftUi();
            }
        }

        // 모달이 열려 있으면 이동·상호작용을 잠근다.
        if (ProductionSummaryUI.IsOpen || MachineGrantUI.IsOpen || MachineCraftUI.IsOpen
            || ZoneExpansionUI.IsOpen || InventoryUI.IsOpen || TechTreeUI.IsOpen || RecipeBookUI.IsOpen
            || (QuestWindowController.Instance != null && QuestWindowController.Instance.IsOpen))
        {
            SetFootstepsPlaying(false);
            UpdateAnimator(Vector2.zero);
            return;
        }

        Vector2 input = Vector2.zero;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        }

        if (keyboard != null)
        {
            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                isResourceNodePlacementMode = !isResourceNodePlacementMode;
                Debug.Log($"[PlayerMovement] 철광석 노드 배치 모드: {(isResourceNodePlacementMode ? "ON" : "OFF")}");
            }

            // F: 생산 즉시 종료 (Space는 수리·수작업)
            if (keyboard.fKey.wasPressedThisFrame)
            {
                TryForceEndProduction();
            }
        }

        if (isResourceNodePlacementMode)
        {
            TryPlaceResourceNodeAtMouse();
        }

        // TEMP: 모션 검수용. 기계 없이 스페이스만 눌러도 수리 모션을 재생한다.
        TryInteractNearbyMachine(keyboard);

        if (IsPlayingRepair())
        {
            TryApplyPendingRepair();
            SetFootstepsPlaying(false);
            UpdateAnimator(Vector2.zero);
            return;
        }

        ClearStalePendingRepair();

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        SetFootstepsPlaying(input.sqrMagnitude > 0f);
        UpdateAnimator(input);

        float speed = pixelsPerSecond / pixelsPerUnit;
        Vector3 delta = new Vector3(input.x, input.y, 0f) * speed * Time.deltaTime;
        Vector3 next = transform.position + delta;

        if (gridManager != null)
        {
            next = ResolveWalkablePosition(next);
        }

        transform.position = next;
    }

    private void OnDisable()
    {
        SetFootstepsPlaying(false);
    }

    // 키보드 이동 입력이 있는 동안만 루프 발소리를 유지한다.
    // 매 프레임 Start/Stop을 호출하지 않도록 상태가 바뀔 때만 AudioManager에 전달한다.
    private void SetFootstepsPlaying(bool shouldPlay)
    {
        if (footstepsPlaying == shouldPlay)
        {
            return;
        }

        footstepsPlaying = shouldPlay;

        if (AudioManager.Instance == null)
        {
            return;
        }

        if (shouldPlay)
        {
            AudioManager.Instance.StartFootsteps();
        }
        else
        {
            AudioManager.Instance.StopFootsteps();
        }
    }

    // Locked·노드 칸을 뚫지 않도록 플레이어 월드 좌표를 보정한다. 막히면 축별 미끄러짐을 시도한다.
    private Vector3 ResolveWalkablePosition(Vector3 next)
    {
        next = ClampWorldPositionWithCollision(next);

        if (IsWalkableWorld(next))
        {
            return next;
        }

        Vector3 current = transform.position;
        Vector3 nextX = ClampWorldPositionWithCollision(new Vector3(next.x, current.y, current.z));
        Vector3 nextY = ClampWorldPositionWithCollision(new Vector3(current.x, next.y, current.z));

        if (IsWalkableWorld(nextX))
        {
            return nextX;
        }

        if (IsWalkableWorld(nextY))
        {
            return nextY;
        }

        return current;
    }

    // 발자국 충돌 반경을 반영해 맵 경계 안으로 월드 좌표를 제한한다.
    private Vector3 ClampWorldPositionWithCollision(Vector3 worldPosition)
    {
        float half = GetWalkCollisionHalfExtent();
        Vector3 min = gridManager.GridToWorld(0, 0);
        Vector3 max = gridManager.GridToWorld(gridManager.Width - 1, gridManager.Height - 1);

        // GridToWorld는 셀 중심이므로, 셀 경계까지 여유를 두고 half cell을 보정한다.
        float cellHalf = gridManager.CellSize * 0.5f;
        float minX = min.x - cellHalf + half;
        float maxX = max.x + cellHalf - half;
        float minY = min.y - cellHalf + half;
        float maxY = max.y + cellHalf - half;

        worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.y = Mathf.Clamp(worldPosition.y, minY, maxY);
        return worldPosition;
    }

    // 중심 칸만 Floor(또는 컨베이어)면 이동 가능하다.
    // 모서리까지 막으면 기계 옆에 끼었을 때 빠져나오기 어렵다.
    private bool IsWalkableWorld(Vector3 worldPosition)
    {
        return IsWalkablePoint(worldPosition);
    }

    private bool IsWalkablePoint(Vector3 worldPosition)
    {
        return gridManager.IsWalkable(gridManager.WorldToGrid(worldPosition));
    }

    private float GetWalkCollisionHalfExtent()
    {
        return gridManager.CellSize * walkCollisionHalfExtentCells;
    }

    // Space: 근접 1칸 내 고장 기계 수리 우선, 없으면 수작업 기계 진도.
    private void TryInteractNearbyMachine(Keyboard keyboard)
    {
        if (keyboard == null
            || !keyboard.spaceKey.wasPressedThisFrame
            || DialogueUI.IsOpen
            || TutorialPanelUI.IsOpen)
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
            // 고장은 애니 임팩트 프레임에 수리 적용. 휘두름음은 audio 쪽과 동일하게 재생.
            QueueRepairAtImpact(brokenTarget);
            PlayRepairMotion();
            PlayCatalogSfx(audio => audio.Catalog.hammerWhoosh);
            return;
        }

        ClearPendingRepair();
        Machine handmadeTarget = FindNearestMachineWithinOneCell(
            machine => machine is HandmadeMachine);
        if (handmadeTarget != null)
        {
            BeginHammerSwing(handmadeTarget, HammerTargetKind.HandmadeMachine);
            return;
        }

        Machine manualTarget = FindNearestMachineWithinOneCell(machine => machine.SupportsManualWorkClick());
        if (manualTarget != null)
        {
            BeginHammerSwing(manualTarget, HammerTargetKind.ManualMachine);
            return;
        }

        Machine nearbyMachine = FindNearestMachineWithinOneCell(machine => machine != null);
        if (nearbyMachine != null)
        {
            BeginHammerSwing(nearbyMachine, HammerTargetKind.NormalMachine);
            return;
        }

        BeginHammerSwing(null, HammerTargetKind.Air);
    }

    // 모든 망치 행동은 휘두름음/모션을 먼저 끝내고, 1초 뒤에만 실제 효과를 적용한다.
    private void BeginHammerSwing(Machine target, HammerTargetKind targetKind)
    {
        PlayRepairMotion();
        PlayCatalogSfx(audio => audio.Catalog.hammerWhoosh);
        StartCoroutine(ResolveHammerSwingAfterDelay(target, targetKind));
    }

    private IEnumerator ResolveHammerSwingAfterDelay(Machine target, HammerTargetKind targetKind)
    {
        yield return new WaitForSecondsRealtime(HammerSwingDuration);

        switch (targetKind)
        {
            case HammerTargetKind.HandmadeMachine:
                if (target != null && target.TryAdvanceManualClick())
                {
                    PlayCatalogSfx(audio => audio.Catalog.metalTap);
                }
                break;

            case HammerTargetKind.ManualMachine:
                if (target != null && target.TryAdvanceManualClick())
                {
                    TrySetAnimatorTrigger(WorkHash);
                    PlayCatalogSfx(audio => audio.Catalog.metalTap);
                }
                break;

            case HammerTargetKind.NormalMachine:
                if (target != null)
                {
                    PlayCatalogSfx(audio => audio.Catalog.metalTap);
                }
                break;
        }
    }

    private static void PlayCatalogSfx(System.Func<AudioManager, AudioCatalog.AudioEntry> selectEntry)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || audio.Catalog == null || selectEntry == null)
        {
            return;
        }

        audio.PlaySfx(selectEntry(audio));
    }

    private void QueueRepairAtImpact(Machine machine)
    {
        pendingRepairMachine = machine;
        pendingRepairApplied = false;
    }

    private void ClearPendingRepair()
    {
        pendingRepairMachine = null;
        pendingRepairApplied = false;
    }

    private void ClearStalePendingRepair()
    {
        if (pendingRepairMachine != null && !pendingRepairApplied)
        {
            ClearPendingRepair();
        }
    }

    private void TryApplyPendingRepair()
    {
        if (pendingRepairApplied || pendingRepairMachine == null || animator == null)
        {
            return;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash != RepairHash
            || current.normalizedTime < RepairImpactNormalizedTime)
        {
            return;
        }

        TryRepairNearbyMachine(pendingRepairMachine);
        pendingRepairApplied = true;
        pendingRepairMachine = null;
    }

    private void PlayRepairMotion()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null || !animator.isActiveAndEnabled)
        {
            return;
        }

        animator.Play(RepairHash, 0, 0f);
        animator.Update(0f);
        repairAnimPending = true;
        repairAnimUntil = Time.time + RepairAnimTimeout;
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
    private bool TrySetAnimatorTrigger(int triggerHash)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger
                && parameter.nameHash == triggerHash)
            {
                animator.SetTrigger(triggerHash);
                return true;
            }
        }

        return false;
    }

    private bool IsPlayingRepair()
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == RepairHash)
        {
            repairAnimPending = false;
            return true;
        }

        if (animator.IsInTransition(0)
            && animator.GetNextAnimatorStateInfo(0).shortNameHash == RepairHash)
        {
            return true;
        }

        if (repairAnimPending && Time.time > repairAnimUntil)
        {
            repairAnimPending = false;
        }

        return repairAnimPending;
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
    // 좌·우·아래는 P_MoveForth, 위는 P_MoveBack. 오른쪽을 보면 flipX로 반전한다.
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

            // 좌·우는 facing.x, 상·하는 함께 누른 좌우 입력으로 반전 여부를 정한다.
            if (facing.x != 0f)
            {
                flipSpriteX = facing.x > 0f;
            }
            else
            {
                flipSpriteX = input.x > 0f;
            }
        }

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetFloat(MoveXHash, facing.x);
        animator.SetFloat(MoveYHash, facing.y);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flipSpriteX;
        }
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
                    "Production일 때만 즉시 종료됩니다.");
                return;
            }

            GameSessionState.Instance.ForceEndProduction();
            return;
        }

        // ProductionScene만 단독 플레이할 때: 세션 없이 요약 모달만 연다.
        Debug.Log("[PlayerMovement] GameSessionState 없음 — 요약 UI만 표시합니다.");
        ProductionEndHandler.EndProduction();
    }

    // 해금된 기계를 골드+재료로 사는 UI를 토글한다.
    private void TryToggleMachineCraftUi()
    {
        if (machineDatabase == null)
        {
            Debug.LogWarning("[PlayerMovement] MachineDatabase가 할당되지 않아 제작 UI를 열 수 없습니다.");
            return;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[PlayerMovement] PlayerInventory가 없어 제작 UI를 열 수 없습니다.");
            return;
        }

        MachineCraftUI.Toggle(machineDatabase, inventory);
    }

    // MachineDatabase 목록으로 기계 지급 UI를 토글한다.
    private void TryToggleMachineGrantUi()
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

        MachineGrantUI.Toggle(machineDatabase, inventory);
    }

    private PlayerInventory GetPlayerInventory()
    {
        if (playerInventory != null)
        {
            return playerInventory;
        }

        playerInventory = PlayerInventory.GetOrFind();
        if (playerInventory != null)
        {
            return playerInventory;
        }

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory != null)
        {
            return playerInventory;
        }

        playerInventory = gameObject.AddComponent<PlayerInventory>();
        return playerInventory;
    }
}
