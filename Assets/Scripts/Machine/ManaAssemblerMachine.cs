using UnityEngine;

// 자동 마나 제작기. 가로 2 × 세로 1. 수작업 1티어와 같은 footprint, 틱으로 진행한다.
public class ManaAssemblerMachine : Machine, IFactoryProduction
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 1);

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
        ApplySelectedRecipe();
    }

    public override bool UsesMana() => true;

    public void TickCompleteProduction()
    {
        if (!CompleteProductionTick())
        {
            return;
        }

        string recipeId = currentRecipe != null ? currentRecipe.id : "(없음)";
#if UNITY_EDITOR
        Debug.Log($"[ManaAssemblerMachine] 생산 성공 @ {GridAnchor} : {recipeId} → {DescribePortEntries(currentRecipe?.outputEntryList)} / 출력 {DescribePortEntries(outputPort)}");
#endif
    }

    public void TickStartProduction()
    {
        StartProductionTick();
    }
}
