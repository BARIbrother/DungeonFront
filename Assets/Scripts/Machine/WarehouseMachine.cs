using System.Collections.Generic;
using UnityEngine;

// 창고. outputPort는 막고, input으로 들어온 아이템은 즉시 플레이어 인벤으로 옮긴다.
// 마나 저장소와 입고 경로는 같다. 일반 아이템만 받고 정수는 거절한다.
// 나무·돌은 무한 재고라 입고해도 쌓지 않고, 출력기는 재고 없이 꺼낸다.
public class WarehouseMachine : Machine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 1);

    private void Awake()
    {
        size = GetFootprintSize();
        EnsureBlockedOutputPort();
    }

    public override void InitializeMachine()
    {
        EnsureBlockedOutputPort();
    }

    public override bool SupportsRecipeSelectionUi() => false;

    public override bool SupportsInventoryTransferUi() => false;

    public override bool SupportsManualWorkClick() => false;

    // 들어온 아이템을 포트에 쌓지 않고 바로 공유 인벤으로 넣는다.
    public override bool PutintoInputPort(ItemEntry IE)
    {
        if (IsBroken || IE == null || IE.item == null || IE.count <= 0)
        {
            return false;
        }

        if (ManaEssence.IsEssence(IE.item))
        {
            return false;
        }

        // 나무·돌은 창고 무한 재고라 인벤에 쌓지 않는다. 벨트 입고는 성공으로 받는다.
        if (WarehouseStock.IsInfinite(IE.item))
        {
            return true;
        }

        AddToPlayerInventory(new ItemEntry { item = IE.item.Clone(), count = IE.count });
        return true;
    }

    // outputPort 사용을 막는다.
    public override bool TakeoutOutputPort(ItemEntry IE) => false;

    public override List<ItemEntry> CollectFinishedGoodsSnapshot()
    {
        return new List<ItemEntry>();
    }

    public override void TransferFinishedGoodsToPlayerInventory()
    {
    }

    private void EnsureBlockedOutputPort()
    {
        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }

        // 입고는 PutintoInputPort에서 즉시 처리하므로 input 슬롯은 비워 둔다.
        if (inputPort.entries == null || inputPort.length != 0)
        {
            inputPort.length = 0;
            inputPort.Resize();
        }

        if (outputPort.entries == null || outputPort.length != 0)
        {
            outputPort.length = 0;
            outputPort.Resize();
        }
    }
}
