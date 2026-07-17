using UnityEngine;

// 틱 대신 플레이어 클릭으로 workSpeed만큼 진행하는 수작업 기계.
public class HandmadeMachine : Machine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 2);

    private void Awake()
    {
        size = GetFootprintSize();

        // 프리팹이 2x2 플레이스홀더를 scale (0.5,1)로 1x2에 맞출 때,
        // 클릭 콜라이더는 로컬 크기를 scale로 나눠 월드 footprint와 같게 만든다.
        if (GetComponent<Collider2D>() == null)
        {
            var boxCollider = gameObject.AddComponent<BoxCollider2D>();
            Vector3 scale = transform.localScale;
            float scaleX = Mathf.Abs(scale.x) > 0.0001f ? Mathf.Abs(scale.x) : 1f;
            float scaleY = Mathf.Abs(scale.y) > 0.0001f ? Mathf.Abs(scale.y) : 1f;
            boxCollider.size = new Vector2(size.x / scaleX, size.y / scaleY);
        }
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
