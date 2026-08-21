using UnityEngine;

public class Extractor : Machine
{
    [System.Serializable]
    public struct DirectionalSprite
    {
        public string id;
        public Sprite sprite;
    }

    [SerializeField] private Vector2Int flowDirection = Vector2Int.right;
    [SerializeField] private DirectionalSprite[] directionalSprites;
    [SerializeField] private Item pickedItem;

    private GridManager cachedGridManager;
    private SpriteRenderer extractorRenderer;

    public Vector2Int FlowDirection => flowDirection;

    public Item PickedItem => pickedItem;

    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 1);

    public override bool SupportsInventoryTransferUi() => false;

    public override bool SupportsRecipeSelectionUi() => false;

    public override bool SupportsItemPickerUi() => true;

    private void Awake()
    {
        size = GetFootprintSize();
        ConveyerBeltItemView leftoverView = GetComponent<ConveyerBeltItemView>();
        if (leftoverView != null)
        {
            Destroy(leftoverView);
        }

        extractorRenderer = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.identity;
        ApplyExtractorVisual();
    }

    public override void ChangeRecipe(Recipe newRecipe)
    {
        currentRecipe = null;
    }

    public override void InitializeMachine()
    {
        ApplyExtractorVisual();
        RefreshAdjacentBeltVisuals();
    }

    public override Item GetPickedItem() => pickedItem;

    public override void SetPickedItem(Item item)
    {
        pickedItem = item != null ? item.Clone() : null;
    }

    public void SetFlowDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        flowDirection = direction;
        transform.rotation = Quaternion.identity;
        ApplyExtractorVisual();
        RefreshAdjacentBeltVisuals();
    }

    public void SetDirectionalSprites(DirectionalSprite[] sprites)
    {
        directionalSprites = sprites;
        ApplyExtractorVisual();
    }

    public Sprite GetSpriteForDirection(Vector2Int direction)
    {
        string id = SpriteIdForDirection(direction);
        return FindDirectionalSprite(id) ?? FindDirectionalSprite("side");
    }

    public override void TickLogistics()
    {
        if (IsBroken || pickedItem == null)
        {
            return;
        }

        if (!IsConnectedToCompatibleStorage())
        {
            return;
        }

        ConveyerBelt facingBelt = GetFacingBelt();
        if (facingBelt == null || facingBelt.HasHeldItem)
        {
            return;
        }

        if (ManaEssence.IsEssence(pickedItem))
        {
            if (!TryExtractEssenceFromStorage())
            {
                return;
            }
        }
        else
        {
            PlayerInventory inventory = PlayerInventory.GetOrFind();
            if (inventory == null || inventory.GetCount(pickedItem) <= 0)
            {
                return;
            }

            if (inventory.Remove(pickedItem, 1) <= 0)
            {
                return;
            }
        }

        facingBelt.ReceiveItem(new ItemEntry
        {
            item = pickedItem.Clone(),
            count = 1,
        });
    }

    public bool IsConnectedToWarehouse()
    {
        return GetStorageBehind() is WarehouseMachine;
    }

    private bool IsConnectedToCompatibleStorage()
    {
        Machine behind = GetStorageBehind();
        if (ManaEssence.IsEssence(pickedItem))
        {
            return behind is ManaStorageMachine;
        }

        return behind is WarehouseMachine;
    }

    private bool TryExtractEssenceFromStorage()
    {
        if (GetStorageBehind() is not ManaStorageMachine storage)
        {
            return false;
        }

        return storage.TryExtractItem(pickedItem, 1);
    }

    private Machine GetStorageBehind()
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            return null;
        }

        return gridManager.GetMachineAt(GridAnchor - flowDirection);
    }

    private ConveyerBelt GetFacingBelt()
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            return null;
        }

        return gridManager.GetMachineAt(GridAnchor + flowDirection) as ConveyerBelt;
    }

    private void ApplyExtractorVisual()
    {
        if (extractorRenderer == null)
        {
            extractorRenderer = GetComponent<SpriteRenderer>();
        }

        if (extractorRenderer == null)
        {
            return;
        }

        Sprite sprite = GetSpriteForDirection(flowDirection);
        if (sprite != null)
        {
            extractorRenderer.sprite = sprite;
            extractorRenderer.color = Color.white;
        }

        extractorRenderer.flipX = ExtractorArt.FlipXForDirection(flowDirection);
        extractorRenderer.flipY = false;
        transform.rotation = Quaternion.identity;
    }

    private void RefreshAdjacentBeltVisuals()
    {
        GridManager gridManager = GetGridManager();
        if (gridManager == null)
        {
            return;
        }

        for (int i = 0; i < ConveyorBeltArt.Cardinals.Length; i++)
        {
            Vector2Int neighbor = GridAnchor + ConveyorBeltArt.Cardinals[i];
            if (gridManager.GetMachineAt(neighbor) is ConveyerBelt belt)
            {
                belt.RefreshNeighbors(gridManager);
            }
        }
    }

    private GridManager GetGridManager()
    {
        if (cachedGridManager == null)
        {
            cachedGridManager = FindAnyObjectByType<GridManager>();
        }

        return cachedGridManager;
    }

    private Sprite FindDirectionalSprite(string id)
    {
        if (directionalSprites == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < directionalSprites.Length; i++)
        {
            if (directionalSprites[i].id == id && directionalSprites[i].sprite != null)
            {
                return directionalSprites[i].sprite;
            }
        }

        return null;
    }

    private static string SpriteIdForDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
        {
            return "up";
        }

        if (direction == Vector2Int.down)
        {
            return "down";
        }

        return "side";
    }
}
