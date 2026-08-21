using UnityEngine;

// 배치 모드 고스트 미리보기. 선택한 기계 프리팹 스프라이트를 반투명으로 표시한다.
public class PlacementPreview : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(0.55f, 1f, 0.55f, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.35f, 0.35f, 0.5f);
    [SerializeField] private int sortingOrderOffset = 10;

    private GameObject ghostObject;
    private SpriteRenderer ghostRenderer;
    private SpriteRenderer[] ghostTurnOverlays;
    private GameObject currentPrefab;
    private readonly Vector2Int[] ghostTurnReceives = new Vector2Int[ConveyerBelt.MaxTurnReceives];

    public void Hide()
    {
        if (ghostObject != null)
        {
            ghostObject.SetActive(false);
        }
    }

    // mouseWorld 기준으로 고스트 위치·색(가능/불가)을 갱신한다. beltFlowDirection은 컨베이어 미리보기 스프라이트에 쓴다.
    public void UpdatePreview(
        GridManager gridManager,
        GameObject machinePrefab,
        Vector3 mouseWorld,
        Vector2Int? beltFlowDirection = null)
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
        Color tint = canPlace ? validColor : invalidColor;

        ghostObject.transform.position = centerWorld;
        ghostObject.transform.rotation = Quaternion.identity;
        if (machine is ConveyerBelt belt && beltFlowDirection.HasValue)
        {
            ApplyBeltGhostSprites(belt, gridManager, anchor, beltFlowDirection.Value, tint);
        }
        else if (machine is Extractor extractor && beltFlowDirection.HasValue)
        {
            SetGhostTurnOverlayCount(0);
            ApplyExtractorGhostSprite(extractor, beltFlowDirection.Value, tint);
        }
        else
        {
            SetGhostTurnOverlayCount(0);
            ghostRenderer.flipX = false;
            ghostRenderer.color = tint;
        }

        ghostObject.SetActive(true);
    }

    private void ApplyExtractorGhostSprite(Extractor extractor, Vector2Int send, Color tint)
    {
        Sprite sprite = extractor.GetSpriteForDirection(send);
        if (sprite != null)
        {
            ghostRenderer.sprite = sprite;
        }

        ghostRenderer.flipX = ExtractorArt.FlipXForDirection(send);
        ghostRenderer.flipY = false;
        ghostRenderer.color = tint;
    }

    private void ApplyBeltGhostSprites(
        ConveyerBelt belt,
        GridManager gridManager,
        Vector2Int anchor,
        Vector2Int send,
        Color tint)
    {
        Vector2Int straight = ConveyorBeltArt.StraightReceive(send);
        Sprite baseSprite = belt.GetSpriteForDirections(send, straight);
        if (baseSprite != null)
        {
            ghostRenderer.sprite = baseSprite;
        }

        ghostRenderer.flipX = false;
        ghostRenderer.flipY = false;
        ghostRenderer.color = tint;

        int turnCount = ConveyerBelt.CollectTurnReceiveDirections(
            gridManager,
            anchor,
            send,
            ghostTurnReceives);
        SetGhostTurnOverlayCount(turnCount);
        for (int i = 0; i < turnCount; i++)
        {
            Sprite overlaySprite = belt.GetSpriteForDirections(send, ghostTurnReceives[i]);
            SpriteRenderer overlay = ghostTurnOverlays[i];
            overlay.sprite = overlaySprite;
            overlay.color = tint;
            overlay.sortingLayerID = ghostRenderer.sortingLayerID;
            overlay.sortingOrder = ghostRenderer.sortingOrder + 1 + i;
            overlay.enabled = overlaySprite != null;
        }
    }

    private void SetGhostTurnOverlayCount(int count)
    {
        if (ghostTurnOverlays == null)
        {
            ghostTurnOverlays = new SpriteRenderer[ConveyerBelt.MaxTurnReceives];
        }

        for (int i = 0; i < ConveyerBelt.MaxTurnReceives; i++)
        {
            bool active = i < count;
            SpriteRenderer overlay = ghostTurnOverlays[i];
            if (!active)
            {
                if (overlay != null)
                {
                    overlay.enabled = false;
                }

                continue;
            }

            if (overlay == null)
            {
                var overlayObject = new GameObject($"TurnOverlay_{i}");
                overlayObject.transform.SetParent(ghostObject.transform, false);
                overlay = overlayObject.AddComponent<SpriteRenderer>();
                overlay.drawMode = SpriteDrawMode.Simple;
                ghostTurnOverlays[i] = overlay;
            }

            overlay.enabled = true;
        }
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
