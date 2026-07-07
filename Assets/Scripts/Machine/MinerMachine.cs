using UnityEngine;

public class MinerMachine : Machine, IFactoryProduction
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(1, 1);

    public override bool IsAvailableCellForMachine(GridManager gridManager, Vector2Int coord)
    {
        GridCell cell = gridManager.GetCell(coord);
        return cell.OccupantKind == OccupantKind.ResourceNode;
    }

    public override OccupantKind GetOccupantKind() => OccupantKind.MachineOnResourceNode;

    private void Awake()
    {
        size = GetFootprintSize();

        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }

        inputPort.length = 1;
        outputPort.length = 1;
        inputPort.Resize();
        outputPort.Resize();
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
