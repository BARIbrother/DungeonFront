using UnityEngine;

public class MinerMachine : Machine, IFactoryProduction
{
    // 출력 버퍼가 담을 수 있는 산출 아이템 총 개수. 이만큼 차면 비워질 때까지 채굴을 멈춘다.
    [SerializeField] private int outputBufferCapacity = 3;

    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 1);

    public override bool IsAvailableCellForMachine(GridManager gridManager, Vector2Int coord)
    {
        GridCell cell = gridManager.GetCell(coord);
        return cell.OccupantKind == OccupantKind.ResourceNode;
    }

    public override OccupantKind GetOccupantKind() => OccupantKind.MachineOnResourceNode;

    public override bool SupportsRecipeSelectionUi() => false;

    private void Awake()
    {
        size = GetFootprintSize();

        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }

        inputPort.length = 1;
        outputPort.length = 1;
        inputPort.Resize();
        outputPort.Resize();
    }

    public override void InitializeMachine()
    {
        Recipe recipe = ResolveRecipeForInstalledResource();
        if (recipe != null)
        {
            SelectRecipe(recipe);
            return;
        }

        ApplySelectedRecipe();
    }

    // 설치 좌표의 자원 노드 itemId와 출력이 일치하는 레시피를 AvailableRecipes에서 고른다.
    private Recipe ResolveRecipeForInstalledResource()
    {
        RecipePool pool = GetAvailableRecipes();
        if (pool?.recipes == null || pool.recipes.Length == 0)
        {
            return null;
        }

        string resourceItemId = GetInstalledResourceItemId();
        if (string.IsNullOrEmpty(resourceItemId))
        {
            return null;
        }

        foreach (Recipe recipe in pool.recipes)
        {
            if (RecipeOutputsItem(recipe, resourceItemId))
            {
                return recipe;
            }
        }

        Debug.LogWarning($"[MinerMachine] 자원 '{resourceItemId}'에 맞는 드릴 레시피가 AvailableRecipes에 없습니다.");
        return null;
    }

    // 배치 직후에는 아직 그리드 occupant가 ResourceNode이므로 GridAnchor에서 자원 종류를 읽는다.
    private string GetInstalledResourceItemId()
    {
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {
            return null;
        }

        GridCell cell = gridManager.GetCell(GridAnchor);
        if (cell.Occupant == null)
        {
            return null;
        }

        ResourceNode resourceNode = cell.Occupant.GetComponent<ResourceNode>();
        return resourceNode?.ItemId;
    }

    private static bool RecipeOutputsItem(Recipe recipe, string itemId)
    {
        if (recipe?.outputEntryList?.entries == null || string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        foreach (ItemEntry output in recipe.outputEntryList.entries)
        {
            if (output?.item != null && output.item.id == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public void TickStartProduction()
    {
        if (IsBroken)
        {
            return;
        }

        if (hasActiveWip || currentRecipe == null)
        {
            return;
        }

        // 출력 버퍼에 다음 배치를 담을 여유가 있을 때만 새 채굴을 시작한다.
        if (!HasOutputBufferRoom())
        {
            return;
        }

        hasActiveWip = true;
        progressTicks = 0;
    }

    public void TickCompleteProduction()
    {
        if (IsBroken)
        {
            return;
        }

        if (!hasActiveWip || currentRecipe == null)
        {
            return;
        }

        if (progressTicks < currentRecipe.durationByTick)
        {
            progressTicks++;
            return;
        }

        // 버퍼가 가득 차 있으면 산출을 보류하고 대기한다.
        if (!TryStoreOutputToBuffer())
        {
            return;
        }

        progressTicks = 0;
        hasActiveWip = false;

        Debug.Log($"[MinerMachine] 채굴 생산 성공 @ {GridAnchor} : +{DescribeEntries(currentRecipe.outputEntryList)} / 버퍼 {DescribeEntries(outputPort)}");
    }

    // 레시피 출력을 버퍼(outputPort)에 누적한다. 여유가 없으면 false.
    private bool TryStoreOutputToBuffer()
    {
        if (!HasOutputBufferRoom())
        {
            return false;
        }

        foreach (ItemEntry output in currentRecipe.outputEntryList.entries)
        {
            if (output == null || output.item == null || output.count <= 0)
            {
                continue;
            }

            outputPort.TryAdd(new ItemEntry { item = output.item, count = output.count });
        }

        return true;
    }

    // 다음 배치를 담아도 버퍼 용량을 넘지 않는지 확인한다.
    private bool HasOutputBufferRoom()
    {
        if (outputPort == null || currentRecipe?.outputEntryList?.entries == null)
        {
            return false;
        }

        foreach (ItemEntry output in currentRecipe.outputEntryList.entries)
        {
            if (output == null || output.item == null || output.count <= 0)
            {
                continue;
            }

            if (GetOutputBufferCount(output.item) + output.count > outputBufferCapacity)
            {
                return false;
            }
        }

        return true;
    }

    // 버퍼에 쌓인 특정 아이템의 총 개수를 센다.
    private int GetOutputBufferCount(ItemDefinition item)
    {
        if (outputPort?.entries == null || item == null)
        {
            return 0;
        }

        int total = 0;
        foreach (ItemEntry entry in outputPort.entries)
        {
            if (entry != null && entry.item != null && entry.item.id == item.id)
            {
                total += entry.count;
            }
        }

        return total;
    }

    // 아이템 목록을 로그용 문자열로 만든다.
    private static string DescribeEntries(ItemEntryList list)
    {
        if (list?.entries == null)
        {
            return "(없음)";
        }

        var builder = new System.Text.StringBuilder();
        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            string itemName = string.IsNullOrEmpty(entry.item.displayName)
                ? entry.item.id
                : entry.item.displayName;
            builder.Append($"{itemName} x{entry.count}");
        }

        return builder.Length > 0 ? builder.ToString() : "(없음)";
    }

    protected override void RefundActiveWipToPlayerInventory()
    {
        // 채굴기는 WIP 시작 시 입력 재료를 소비하지 않는다.
    }
}
