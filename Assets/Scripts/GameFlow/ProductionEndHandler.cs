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

        // 생산 종료 순간에는 고장 예약/루프도 끝낸다. 결산 확인을 누를 때까지
        // 고장 상태가 남아 다음 단계에서 다시 소리 나는 일을 막는다.
        ProductionEventManager.Instance?.EndProductionSession();

        List<ProductionSummaryLine> lines = CollectFinishedGoodsFromMap();
        AudioManager audio = AudioManager.Instance;
        AudioCatalog.AudioEntry phaseEnd = audio != null && audio.Catalog != null
            ? audio.Catalog.phaseEnd
            : null;

        if (audio != null)
        {
            audio.StopBgm();
            audio.PlaySfx(phaseEnd);
        }

        // 호루라기가 끝난 뒤에만 결산 내용을 연다.
        ProductionSummaryUI.ShowAfterSound(lines,
            audio != null ? audio.GetPlaybackDuration(phaseEnd) : 0f);
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
