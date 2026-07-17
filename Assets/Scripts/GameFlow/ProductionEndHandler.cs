using System.Collections.Generic;
using UnityEngine;

// 생산 종료 시 맵 완성품 수집·인벤 이동·WIP 환원 후 요약 UI를 띄운다.
public static class ProductionEndHandler
{
    // GameSessionState 없이 ProductionScene만 테스트할 때 중복 종료를 막는다.
    private static bool isEnding;

    // 틱 정지 → 완성품 스냅샷·이동 → WIP/입력 환원 → 요약 모달.
    public static void EndProduction()
    {
        if (isEnding || ProductionSummaryUI.IsOpen)
        {
            return;
        }

        isEnding = true;

        if (TickManager.Instance != null)
        {
            TickManager.Instance.StopTick();
        }

        List<ProductionSummaryLine> lines = CollectFinishedGoodsFromMap();
        TransferFinishedGoodsAndRefundNonFinished();
        ProductionSummaryUI.Show(lines);
    }

    // 요약 확인 후 중복 종료 가드를 해제한다.
    public static void ClearEnding()
    {
        isEnding = false;
    }

    // outputPort·벨트 heldItem을 itemId별 합산한 요약 목록을 만든다.
    private static List<ProductionSummaryLine> CollectFinishedGoodsFromMap()
    {
        var totals = new Dictionary<string, ProductionSummaryLine>();
        IReadOnlyList<Machine> machines = GetMachinesOnGrid();

        for (int i = 0; i < machines.Count; i++)
        {
            Machine machine = machines[i];
            if (machine == null)
            {
                continue;
            }

            List<ItemEntry> entries = machine.CollectFinishedGoodsSnapshot();
            if (entries == null)
            {
                continue;
            }

            for (int j = 0; j < entries.Count; j++)
            {
                ItemEntry entry = entries[j];
                if (entry == null || entry.item == null || entry.count <= 0)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry.item.id))
                {
                    continue;
                }

                string itemId = entry.item.id;
                string displayName = string.IsNullOrEmpty(entry.item.displayName)
                    ? itemId
                    : entry.item.displayName;

                if (totals.TryGetValue(itemId, out ProductionSummaryLine existing))
                {
                    existing.count += entry.count;
                    totals[itemId] = existing;
                }
                else
                {
                    totals[itemId] = new ProductionSummaryLine
                    {
                        itemId = itemId,
                        displayName = displayName,
                        count = entry.count,
                    };
                }
            }
        }

        return new List<ProductionSummaryLine>(totals.Values);
    }

    // 완성품 인벤 이동 후 WIP·입력 포트 잔여를 환원한다.
    private static void TransferFinishedGoodsAndRefundNonFinished()
    {
        IReadOnlyList<Machine> machines = GetMachinesOnGrid();

        for (int i = 0; i < machines.Count; i++)
        {
            Machine machine = machines[i];
            if (machine == null)
            {
                continue;
            }

            machine.TransferFinishedGoodsToPlayerInventory();
            machine.RefundNonFinishedContentsToPlayerInventory();
        }
    }

    private static IReadOnlyList<Machine> GetMachinesOnGrid()
    {
        if (TickManager.Instance != null)
        {
            return TickManager.Instance.MachinesOnGrid;
        }

        return Object.FindObjectsByType<Machine>(FindObjectsInactive.Exclude);
    }
}
