using UnityEngine;

// 틱 대신 플레이어 클릭으로 workSpeed만큼 진행하는 수작업 기계.
public class HandmadeMachine : Machine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 1);

    private void Awake()
    {
        size = GetFootprintSize();
        transform.localScale = Vector3.one;

        // 64x32 스프라이트(PPU 32) = 월드 2x1. footprint와 동일하게 클릭 콜라이더를 맞춘다.
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

    public override bool SupportsManualWorkClick() => true;

    // 키 입력 1회: WIP가 없으면 시작하고, workSpeed만큼 진행한다. 진행에 성공하면 true.
    public override bool TryAdvanceManualClick()
    {
        if (IsBroken || currentRecipe == null || currentRecipe.recipeTime <= 0)
        {
            return false;
        }

        if (workSpeed <= 0)
        {
            return false;
        }

        if (!hasActiveWip)
        {
            StartProductionTick();
        }

        if (!hasActiveWip)
        {
            return false;
        }

        int progressBefore = progressTicks;
        AdvanceProductionWork(workSpeed);
        return progressTicks != progressBefore || !hasActiveWip;
    }
}
