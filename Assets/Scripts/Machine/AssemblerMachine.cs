using UnityEngine;

public class AssemblerMachine : Machine, IFactoryProduction
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 2);

    private void Awake()
    {
        size = GetFootprintSize();
    }

    public override void InitializeMachine()
    {
    }

    public void TickCompleteProduction()
    {
        if (!CompleteProductionTick())
        {
            return;
        }

        string recipeId = currentRecipe != null ? currentRecipe.id : "(없음)";
        Debug.Log($"[AssemblerMachine] 생산 성공 @ {GridAnchor} : {recipeId} → {DescribePortEntries(currentRecipe?.outputEntryList)} / 출력 {DescribePortEntries(outputPort)}");
    }

    public void TickStartProduction()
    {
        StartProductionTick();
    }
}
