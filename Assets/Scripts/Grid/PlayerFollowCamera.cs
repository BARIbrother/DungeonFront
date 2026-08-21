using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어를 따라가되, 카메라 시야가 (맵 + 미확인 테두리) 바깥을 비추지 않도록 제한한다.
[RequireComponent(typeof(Camera))]
public class PlayerFollowCamera : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform target;

    // 0이면 즉시 추적, 값이 클수록 부드럽게 따라간다.
    [SerializeField] private float smoothTime = 0.12f;

    // 체감 크기 배율(면적 감 → ortho에 √배율). 이후 조율로 0.8배를 한 번 더 곱한다.
    [SerializeField] private float appearLargerBy = 1.4f;
    [SerializeField] private float sizeScale = 0.8f;
    [SerializeField] private float baseOrthographicSize = 5f;

    // 기본 ortho 대비 스크롤 줌 한계(작을수록 확대). 기본≈5.28 → 약 3.5~7.9.
    [SerializeField] private float minOrthoSizeFactor = 0.65f;
    [SerializeField] private float maxOrthoSizeFactor = 1.5f;
    // 휠 노치(±120)당 ortho 변화량.
    [SerializeField] private float scrollOrthoPerNotch = 19.8f;

    // 맵 밖 미확인 타일(구역 단위) 두께. 카메라 클램프도 이만큼 확장한다.
    [SerializeField] private int exteriorPadZones = 1;

    private Camera targetCamera;
    private Vector3 followVelocity;
    private float defaultOrthographicSize;
    private float minOrthographicSize;
    private float maxOrthographicSize;
    private PlacementController placementController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOnMainCamera()
    {
        Camera main = Camera.main;
        if (main == null || main.GetComponent<PlayerFollowCamera>() != null)
        {
            return;
        }

        main.gameObject.AddComponent<PlayerFollowCamera>();
    }

    // 참조를 찾고, Player 자식이면 계층에서 떼어낸다.
    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.orthographic = true;

        // 카메라 Transform 스케일이 남아 있으면 ortho와 겹쳐 체감 줌이 과해진다.
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
        }

        float sizeFeel = Mathf.Max(0.01f, appearLargerBy);
        float linearZoom = Mathf.Sqrt(sizeFeel) * Mathf.Max(0.01f, sizeScale);
        defaultOrthographicSize = baseOrthographicSize / linearZoom;
        minOrthographicSize = defaultOrthographicSize * Mathf.Max(0.05f, minOrthoSizeFactor);
        maxOrthographicSize = defaultOrthographicSize * Mathf.Max(minOrthoSizeFactor, maxOrthoSizeFactor);
        targetCamera.orthographicSize = defaultOrthographicSize;

        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (target == null)
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
            }
        }

        placementController = FindAnyObjectByType<PlacementController>();

        // 자식으로 두면 부모 이동과 클램프가 서로 덮어써서 떨림이 생긴다.
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }
    }

    // PlayerMovement.Start가 스폰 위치를 잡은 뒤라 첫 프레임부터 어긋나지 않는다.
    private void Start()
    {
        SnapToTarget();
    }

    private void Update()
    {
        TryHandleScrollZoom();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = ClampToMap(target.position);
        transform.position = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smoothTime)
            : desired;
    }

    // 다른 UI가 열려 있지 않을 때만 휠로 ortho를 조절한다.
    private void TryHandleScrollZoom()
    {
        if (IsCameraZoomBlocked())
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        float scrollY = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scrollY) < 0.01f)
        {
            return;
        }

        // Input System 휠은 보통 노치당 ±120.
        float notches = scrollY / 120f;
        float nextSize = targetCamera.orthographicSize - notches * scrollOrthoPerNotch;
        targetCamera.orthographicSize = Mathf.Clamp(nextSize, minOrthographicSize, maxOrthographicSize);
    }

    private bool IsCameraZoomBlocked()
    {
        if (GamePauseService.IsPaused
            || ProductionSummaryUI.IsOpen
            || MachineGrantUI.IsOpen
            || MachineCraftUI.IsOpen
            || ZoneExpansionUI.IsOpen
            || InventoryUI.IsOpen
            || TechTreeUI.IsOpen
            || RecipeBookUI.IsOpen
            || DialogueUI.IsOpen
            || TutorialPanelUI.IsOpen
            || (QuestWindowController.Instance != null && QuestWindowController.Instance.IsOpen)
            || (GameOverController.Instance != null && GameOverController.Instance.IsGameOver))
        {
            return true;
        }

        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }

        return placementController != null && placementController.IsPlacementMode;
    }

    // 보간 없이 즉시 대상 위치로 맞춘다. 씬 전환·순간이동 후 호출한다.
    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        followVelocity = Vector3.zero;
        transform.position = ClampToMap(target.position);
    }

    // 카메라 시야 절반만큼 맵(+미확인 테두리) 경계에서 안쪽으로 물린 위치를 돌려준다.
    private Vector3 ClampToMap(Vector3 desired)
    {
        float z = transform.position.z;
        if (gridManager == null)
        {
            return new Vector3(desired.x, desired.y, z);
        }

        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;

        float cellSize = gridManager.CellSize;
        int padCells = Mathf.Max(0, exteriorPadZones) * ZoneManager.ZoneSize;
        // 카메라 반경보다 테두리가 얇으면 시야에 맞춰 최소 패드를 키운다.
        int cameraPadCells = Mathf.CeilToInt(Mathf.Max(halfWidth, halfHeight) / cellSize) + 1;
        padCells = Mathf.Max(padCells, cameraPadCells);

        float pad = padCells * cellSize;
        Vector3 min = gridManager.transform.TransformPoint(new Vector3(-pad, -pad, 0f));
        Vector3 max = gridManager.transform.TransformPoint(
            new Vector3(
                gridManager.Width * cellSize + pad,
                gridManager.Height * cellSize + pad,
                0f));

        return new Vector3(
            ClampAxis(desired.x, min.x, max.x, halfWidth),
            ClampAxis(desired.y, min.y, max.y, halfHeight),
            z);
    }

    // 맵이 시야보다 좁은 축은 가운데로 고정하고, 넓은 축만 경계 안으로 제한한다.
    private static float ClampAxis(float value, float min, float max, float halfExtent)
    {
        if (max - min <= halfExtent * 2f)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(value, min + halfExtent, max - halfExtent);
    }
}
