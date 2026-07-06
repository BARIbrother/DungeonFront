using UnityEngine;

public class SmelterMachine : Machine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 2);

    private void Awake()
    {
        size = GetFootprintSize();
    }

    public override void InitializeMachine()
    {
    }

    public override void Tick()
    {
    }

    public override void PutintoInputPort(ItemEntry IE)
    {
    }

    public override void TakeoutOutputPort(ItemEntry IE)
    {
    }
}
