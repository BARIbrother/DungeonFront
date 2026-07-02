using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Animator animator;
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

        if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame && gridManager != null)
        {
            Vector2Int playerGrid = gridManager.WorldToGrid(transform.position);
            gridManager.TryPlaceResourceNode(playerGrid);
        }

        if (keyboard != null && gridManager != null)
        {
            Vector3 mouseWorld = GetMouseWorldPosition();

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                gridManager.TryPlaceSmelterMachine(mouseWorld);
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                gridManager.TryPlaceAssemblerMachine(mouseWorld);
            }

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                gridManager.TryPlaceMinerMachine(mouseWorld);
            }
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

    // 마우스 스크린 좌표를 XY 평면 월드 좌표로 변환한다.
    private Vector3 GetMouseWorldPosition()
    {
        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera == null)
        {
            return transform.position;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return transform.position;
        }

        Vector3 screen = mouse.position.ReadValue();
        screen.z = -camera.transform.position.z;
        Vector3 world = camera.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }
}
