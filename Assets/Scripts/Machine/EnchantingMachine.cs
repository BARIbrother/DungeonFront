using UnityEngine;

// 마법 부여대. 틱 생산 스텁 (레시피는 RecipePool에서 연결).
public class EnchantingMachine : Machine, IFactoryProduction
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 2);

    private void Awake()
    {
        size = GetFootprintSize();
    }

    public override void InitializeMachine()
    {
        ApplySelectedRecipe();
    }

    public void TickCompleteProduction()
    {
        if (!CompleteProductionTick())
        {
            return;
        }

        string recipeId = currentRecipe != null ? currentRecipe.id : "(없음)";
        Debug.Log($"[EnchantingMachine] 생산 성공 @ {GridAnchor} : {recipeId} → {DescribePortEntries(currentRecipe?.outputEntryList)} / 출력 {DescribePortEntries(outputPort)}");
    }

    public void TickStartProduction()
    {
        StartProductionTick();
    }
}
