using UnityEngine;

public class SmelterMachine : Machine, IFactoryProduction
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
        CompleteProductionTick();
    }

    public void TickStartProduction()
    {
        StartProductionTick();
    }
}
