using UnityEngine;

// 벨트 위 아이템 아이콘 표시. 틱 간 월드 좌표 보간으로 끊김 없이 이동한다.
[RequireComponent(typeof(ConveyerBelt))]
public class ConveyerBeltItemView : MonoBehaviour
{
    [SerializeField] private int sortingOrderOffset = 10;
    [SerializeField] private float iconWorldSize = 0.55f;

    private ConveyerBelt belt;
    private SpriteRenderer beltRenderer;
    private SpriteRenderer itemRenderer;
    private Vector3 displayWorldPosition;
    private Vector3 targetWorldPosition;
    private bool hasVisual;
    private float cachedTickInterval = 0.1f;
    private float cachedCellSize = 1f;
    private bool cachesValid;

    public Vector3 CurrentWorldPosition => displayWorldPosition;

    public bool HasActiveVisual => hasVisual;

    private void Awake()
    {
        belt = GetComponent<ConveyerBelt>();
        beltRenderer = GetComponent<SpriteRenderer>();
        EnsureItemRenderer();
        RefreshCaches();
    }

    private void Update()
    {
        if (!hasVisual || itemRenderer == null)
        {
            return;
        }

        if ((displayWorldPosition - targetWorldPosition).sqrMagnitude
            <= 0.0000001f)
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

        if (!TryApplyHeldItemSprite())
        {
            HideVisual();
            return;
        }

        bool wasHidden = !hasVisual;
        hasVisual = true;
        targetWorldPosition = belt.GetItemWorldPosition(belt.NormalizedProgress);

        if (wasHidden)
        {
            displayWorldPosition = targetWorldPosition;
            if (itemRenderer != null)
            {
                itemRenderer.transform.position = displayWorldPosition;
            }
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
        itemRenderer.transform.position = displayWorldPosition;
    }

    public void ApplyItemSprite(Item item)
    {
        ApplyItemSprite(item?.definition);
    }

    public void ApplyItemSprite(ItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return;
        }

        TryApplyHeldItemSprite(itemDefinition);
    }

    private bool TryApplyHeldItemSprite()
    {
        return TryApplyHeldItemSprite(belt.HeldItemDefinition);
    }

    private bool TryApplyHeldItemSprite(ItemDefinition itemDefinition)
    {
        Sprite icon = ItemIconResolver.Resolve(itemDefinition);
        if (icon == null)
        {
            return false;
        }

        EnsureItemRenderer();
        ApplyBeltSorting();
        ApplyIconScale(icon);
        itemRenderer.sprite = icon;
        itemRenderer.color = Color.white;
        itemRenderer.enabled = true;
        itemRenderer.gameObject.SetActive(true);
        hasVisual = true;
        return true;
    }

    private void ApplyIconScale(Sprite icon)
    {
        if (itemRenderer == null || icon == null)
        {
            return;
        }

        float pixels = Mathf.Max(icon.rect.width, icon.rect.height);
        float ppu = icon.pixelsPerUnit > 0f ? icon.pixelsPerUnit : 32f;
        float nativeWorld = pixels / ppu;
        float scale = nativeWorld > 0.0001f ? iconWorldSize / nativeWorld : 1f;
        itemRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private float GetMoveStepPerFrame()
    {
        if (!cachesValid)
        {
            RefreshCaches();
        }

        float segmentLength = cachedCellSize / ConveyerBelt.TicksPerCell;
        return segmentLength * (Time.deltaTime / cachedTickInterval);
    }

    private void RefreshCaches()
    {
        cachedTickInterval = TickManager.Instance != null
            ? TickManager.Instance.TickInterval
            : 0.1f;
        if (cachedTickInterval <= 0.0001f)
        {
            cachedTickInterval = 0.1f;
        }

        cachedCellSize = belt != null ? belt.GetCellSize() : 1f;
        cachesValid = true;
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
        else
        {
            itemRenderer.sortingOrder = sortingOrderOffset;
        }
    }
}
