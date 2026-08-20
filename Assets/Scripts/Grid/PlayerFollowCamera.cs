using UnityEngine;

// 플레이어를 따라가되, 카메라 시야가 맵(48×64) 바깥을 비추지 않도록 경계 안으로 제한한다.
[RequireComponent(typeof(Camera))]
public class PlayerFollowCamera : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform target;

    // 0이면 즉시 추적, 값이 클수록 부드럽게 따라간다.
    [SerializeField] private float smoothTime = 0.12f;

    private Camera targetCamera;
    private Vector3 followVelocity;

    // 참조를 찾고, Player 자식이면 계층에서 떼어낸다.
    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        targetCamera.orthographic = true;

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

    // 카메라 시야 절반만큼 맵 경계에서 안쪽으로 물린 위치를 돌려준다. z는 유지한다.
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
        Vector3 min = gridManager.transform.TransformPoint(Vector3.zero);
        Vector3 max = gridManager.transform.TransformPoint(
            new Vector3(gridManager.Width * cellSize, gridManager.Height * cellSize, 0f));

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
