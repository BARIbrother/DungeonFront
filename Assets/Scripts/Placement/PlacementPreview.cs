using UnityEngine;

// 배치 모드 고스트 미리보기. 선택한 기계 프리팹 스프라이트를 반투명으로 표시한다.
public class PlacementPreview : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(0.55f, 1f, 0.55f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.35f, 0.35f, 0.5f);
    [SerializeField] private int sortingOrderOffset = 10;

    private GameObject ghostObject;
    private SpriteRenderer ghostRenderer;
    private GameObject currentPrefab;

    public void Hide()
    {
        if (ghostObject != null)
        {
            ghostObject.SetActive(false);
        }
    }

    // mouseWorld 기준으로 고스트 위치·색(가능/불가)을 갱신한다.
    public void UpdatePreview(GridManager gridManager, GameObject machinePrefab, Vector3 mouseWorld)
    {
        if (gridManager == null || machinePrefab == null)
        {
            Hide();
            return;
        }

        Machine machine = machinePrefab.GetComponent<Machine>();
        if (machine == null)
        {
            Hide();
            return;
        }

        if (!EnsureGhostVisual(machinePrefab))
        {
            Hide();
            return;
        }

        Vector2Int footprintSize = machine.GetFootprintSize();
        Vector2Int anchor = gridManager.GetAnchorForCenteredFootprint(mouseWorld, footprintSize);
        Vector3 centerWorld = gridManager.GetFootprintCenterWorld(anchor, footprintSize);
        bool canPlace = gridManager.CanPlaceFootprintAt(anchor, footprintSize, machine);

        ghostObject.transform.position = centerWorld;
        ghostRenderer.color = canPlace ? validColor : invalidColor;
        ghostObject.SetActive(true);
    }

    private bool EnsureGhostVisual(GameObject machinePrefab)
    {
        if (ghostObject == null)
        {
            ghostObject = new GameObject("PlacementGhost");
            ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        }

        if (currentPrefab == machinePrefab)
        {
            return ghostRenderer.sprite != null;
        }

        SpriteRenderer sourceRenderer = machinePrefab.GetComponentInChildren<SpriteRenderer>();
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return false;
        }

        ghostRenderer.sprite = sourceRenderer.sprite;
        ghostRenderer.flipX = sourceRenderer.flipX;
        ghostRenderer.flipY = sourceRenderer.flipY;
        ghostRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;
        ghostObject.transform.localScale = sourceRenderer.transform.lossyScale;
        currentPrefab = machinePrefab;
        return true;
    }
}
