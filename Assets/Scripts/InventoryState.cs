using System.Collections.Generic;

public class InventoryState
{
    public Dictionary<string, int> items = new();
    public List<MachineInstanceState> machines = new();
}