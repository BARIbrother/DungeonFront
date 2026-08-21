using UnityEngine;

public class AssemblerMachine : Machine, IFactoryProduction
{
    // 티어 1은 2×2. 티어 2·3(workSpeed 15·20)은 수동 제작대와 같은 가로 2 × 세로 1.
    public override Vector2Int GetFootprintSize()
    {
        return workSpeed > 10 ? new Vector2Int(2, 1) : new Vector2Int(2, 2);
    }

    private void Awake()
    {
        size = GetFootprintSize();
        transform.localScale = Vector3.one;

        var boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        boxCollider.size = new Vector2(size.x, size.y);
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
#if UNITY_EDITOR
        Debug.Log($"[AssemblerMachine] 생산 성공 @ {GridAnchor} : {recipeId} → {DescribePortEntries(currentRecipe?.outputEntryList)} / 출력 {DescribePortEntries(outputPort)}");
#endif
    }

    public void TickStartProduction()
    {
        StartProductionTick();
    }
}
