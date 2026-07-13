using UnityEngine;

// 벨트 위 아이템 아이콘 표시. 틱 간 월드 좌표 보간으로 끊김 없이 이동한다.
[RequireComponent(typeof(ConveyerBelt))]
public class ConveyerBeltItemView : MonoBehaviour
{
    [SerializeField] private int sortingOrderOffset = 1;

    private ConveyerBelt belt;
    private SpriteRenderer beltRenderer;
    private SpriteRenderer itemRenderer;
    private Vector3 displayWorldPosition;
    private Vector3 targetWorldPosition;
    private bool hasVisual;

    public Vector3 CurrentWorldPosition => displayWorldPosition;

    public bool HasActiveVisual => hasVisual;

    private void Awake()
    {
        belt = GetComponent<ConveyerBelt>();
        beltRenderer = GetComponent<SpriteRenderer>();
        EnsureItemRenderer();
    }

    private void Update()
    {
        if (!hasVisual || itemRenderer == null)
        {
            return;
        }

        displayWorldPosition = Vector3.MoveTowards(
            displayWorldPosition,
            targetWorldPosition,
            GetMoveStepPerFrame());
        itemRenderer.transform.position = displayWorldPosition;
    }

    // 물류 틱 직후 논리 상태에 맞춰 목표 위치·스프라이트를 갱신한다.
    public void SyncAfterLogisticsTick()
    {
        if (!belt.HasHeldItem)
        {
            HideVisual();
            return;
        }

        ItemDefinition itemDefinition = belt.HeldItemDefinition;
        if (itemDefinition == null || itemDefinition.icon == null)
        {
            HideVisual();
            return;
        }

        bool wasHidden = !hasVisual;

        EnsureItemRenderer();
        ApplyBeltSorting();
        itemRenderer.sprite = itemDefinition.icon;
        itemRenderer.enabled = true;
        hasVisual = true;

        targetWorldPosition = belt.GetItemWorldPosition(belt.NormalizedProgress);
        if (!itemRenderer.gameObject.activeSelf)
        {
            itemRenderer.gameObject.SetActive(true);
        }

        if (wasHidden)
        {
            displayWorldPosition = targetWorldPosition;
        }
    }

    // 아이템이 벨트에 올라올 때 월드 좌표를 지정한다. 벨트 간 전달 시 이어받는다.
    public void InheritWorldPosition(Vector3 worldPosition)
    {
        EnsureItemRenderer();
        displayWorldPosition = worldPosition;
        targetWorldPosition = worldPosition;
        hasVisual = true;
        itemRenderer.enabled = true;
        itemRenderer.gameObject.SetActive(true);
    }

    public void ApplyItemSprite(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null || itemDefinition.icon == null)
        {
            return;
        }

        EnsureItemRenderer();
        ApplyBeltSorting();
        itemRenderer.sprite = itemDefinition.icon;
        itemRenderer.enabled = true;
        hasVisual = true;
    }

    private float GetMoveStepPerFrame()
    {
        float tickInterval = TickManager.Instance != null
            ? TickManager.Instance.TickInterval
            : 0.1f;
        float cellSize = belt.GetCellSize();
        float segmentLength = cellSize / ConveyerBelt.TicksPerCell;
        return segmentLength * (Time.deltaTime / tickInterval);
    }

    private void HideVisual()
    {
        hasVisual = false;
        if (itemRenderer != null)
        {
            itemRenderer.enabled = false;
        }
    }

    private void EnsureItemRenderer()
    {
        if (itemRenderer != null)
        {
            return;
        }

        var itemObject = new GameObject("BeltItemIcon");
        itemObject.transform.SetParent(transform, false);
        itemRenderer = itemObject.AddComponent<SpriteRenderer>();
        itemRenderer.enabled = false;
    }

    private void ApplyBeltSorting()
    {
        if (itemRenderer == null)
        {
            return;
        }

        if (beltRenderer != null)
        {
            itemRenderer.sortingLayerID = beltRenderer.sortingLayerID;
            itemRenderer.sortingOrder = beltRenderer.sortingOrder + sortingOrderOffset;
        }
    }
}
