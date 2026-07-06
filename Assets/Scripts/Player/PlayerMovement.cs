using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Animator animator;

    // 디버그용 기계 정의 SO. 1·2·3키로 인벤에 1개씩 추가한다.
    [SerializeField] private ItemDef_Machine key1MachineItem;
    [SerializeField] private ItemDef_Machine key2MachineItem;
    [SerializeField] private ItemDef_Machine key3MachineItem;
    // 4키로 철광석 노드 배치 모드를 토글한다.
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
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                TryAddMachineItem(key1MachineItem);
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                TryAddMachineItem(key2MachineItem);
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                TryAddMachineItem(key3MachineItem);
            }

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                isResourceNodePlacementMode = !isResourceNodePlacementMode;
                Debug.Log($"[PlayerMovement] 철광석 노드 배치 모드: {(isResourceNodePlacementMode ? "ON" : "OFF")}");
            }
        }

        if (isResourceNodePlacementMode)
        {
            TryPlaceResourceNodeAtMouse();
        }

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

    // 4키 배치 모드에서 마우스가 가리키는 그리드 칸에 철광석 노드를 놓는다.
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

    // ItemDef_Machine 정의로 인벤토리에 기계 인스턴스 1개 추가한다.
    private void TryAddMachineItem(ItemDef_Machine definition)
    {
        if (definition == null || definition.machinePrefab == null)
        {
            return;
        }

        PlayerInventory inventory = GetPlayerInventory();
        if (inventory == null)
        {
            return;
        }

        int countBefore = inventory.Machines.Count;
        inventory.AddMachine(definition);

        if (inventory.Machines.Count <= countBefore)
        {
            return;
        }

        MachineInventoryEntry added = inventory.Machines[inventory.Machines.Count - 1];
        Debug.Log($"[PlayerMovement] 기계 지급 성공: {definition.displayName} ({definition.id}), instanceId={added.instanceId}, 인벤 총 {inventory.Machines.Count}대");
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
