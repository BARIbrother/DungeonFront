using UnityEngine;

// footprint만 있는 기계 placeholder. 동작 로직 없음.
public class PlaceholderMachine : Machine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 2);

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
