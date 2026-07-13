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
}
