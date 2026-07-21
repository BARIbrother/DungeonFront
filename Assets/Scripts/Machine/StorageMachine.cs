using System.Collections.Generic;
using UnityEngine;

// 맵 위 아이템 버퍼(마나 저장소 등). 창고(즉시 인벤 이관)는 WarehouseMachine을 쓴다.
public abstract class StorageMachine : Machine
{
    // inputPort 슬롯 수 (벨트 등으로 들어온 아이템 버퍼).
    [SerializeField] private int bufferSlotCount = 8;

    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 2);

    // true면 E키로 버퍼 → 플레이어 인벤 이관.
    protected abstract bool AllowManualWithdraw { get; }

    private void Awake()
    {
        size = GetFootprintSize();
        EnsureBufferPorts();
    }

    public override void InitializeMachine()
    {
        EnsureBufferPorts();
    }

    public override bool SupportsRecipeSelectionUi() => false;

    // 저장소는 생산 기계가 아니므로 정보 패널을 띄우지 않는다.
    public override bool SupportsInfoPanel() => false;

    public override bool SupportsManualWorkClick() => AllowManualWithdraw;

    // 버퍼 전량을 플레이어 인벤으로 옮긴다. 하나라도 옮기면 true.
    public override bool TryAdvanceManualClick()
    {
        if (!AllowManualWithdraw || IsBroken)
        {
            return false;
        }

        return TransferBufferToPlayerInventory();
    }

    // 생산 종료 요약: 창고 버퍼(inputPort)를 완성품으로 취급한다.
    public override List<ItemEntry> CollectFinishedGoodsSnapshot()
    {
        return inputPort != null ? inputPort.CopyAllEntries() : new List<ItemEntry>();
    }

    public override void TransferFinishedGoodsToPlayerInventory()
    {
        TransferBufferToPlayerInventory();
    }

    private void EnsureBufferPorts()
    {
        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }

        int slots = bufferSlotCount > 0 ? bufferSlotCount : 1;
        if (inputPort.entries == null || inputPort.length != slots)
        {
            inputPort.length = slots;
            inputPort.Resize();
        }

        if (outputPort.entries == null || outputPort.length != 0)
        {
            outputPort.length = 0;
            outputPort.Resize();
        }
    }

    // inputPort 슬롯을 비우며 플레이어 인벤에 넣는다.
    private bool TransferBufferToPlayerInventory()
    {
        if (inputPort?.entries == null)
        {
            return false;
        }

        bool transferred = false;
        for (int i = 0; i < inputPort.entries.Length; i++)
        {
            ItemEntry entry = inputPort.entries[i];
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            AddToPlayerInventory(new ItemEntry { item = entry.item, count = entry.count });
            entry.item = null;
            entry.count = 0;
            transferred = true;
        }

        return transferred;
    }
}
