// 창고가 무한으로 공급하는 기초 자원. 입고해도 인벤에 쌓지 않고, 출력기는 재고 없이 꺼낸다.
public static class WarehouseStock
{
    public const string WoodLogId = "wood_log";
    public const string StoneId = "stone";

    public static readonly string[] InfiniteIds =
    {
        WoodLogId,
        StoneId,
    };

    public static bool IsInfinite(Item item)
    {
        return IsInfinite(item != null ? item.Id : null);
    }

    public static bool IsInfinite(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return false;
        }

        return itemId == WoodLogId || itemId == StoneId;
    }
}
