using UnityEngine;

public class Extractor : Machine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 1);

    private void Awake()
    {
        size = GetFootprintSize();
    }

    public override void ChangeRecipe(Recipe newRecipe)
    {
        currentRecipe = null;
    }

    public override void InitializeMachine()
    {
    }

    // Extractor는 생산 기계가 아니므로 정보 패널을 띄우지 않는다.
    public override bool SupportsInfoPanel() => false;
}
