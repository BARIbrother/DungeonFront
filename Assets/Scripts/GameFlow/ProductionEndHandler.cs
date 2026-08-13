using System.Collections.Generic;
using UnityEngine;

// 생산 종료 시 맵에 남은 완성품 스냅샷만 보여 준다. 인벤 이관은 창고만 한다.
public static class ProductionEndHandler
{
    // GameSessionState 없이 ProductionScene만 테스트할 때 중복 종료를 막는다.
    private static bool isEnding;

    // 틱 정지 → 맵 잔여 스냅샷 → 요약 모달. 포트·벨트 내용은 그대로 둔다.
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

                if (string.IsNullOrEmpty(entry.item.Id))
                {
                    continue;
                }

                string itemId = entry.item.Id;
                string displayName = string.IsNullOrEmpty(entry.item.DisplayName)
                    ? itemId
                    : entry.item.DisplayName;

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

    private static IReadOnlyList<Machine> GetMachinesOnGrid()
    {
        if (TickManager.Instance != null)
        {
            return TickManager.Instance.MachinesOnGrid;
        }

        return Object.FindObjectsByType<Machine>(FindObjectsInactive.Exclude);
    }
}
