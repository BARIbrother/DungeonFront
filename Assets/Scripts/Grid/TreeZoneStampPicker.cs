using UnityEngine;
using UnityEngine.Tilemaps;

// 구역 스탬프 mask 계산 · Locked 셀 → 스탬프 타일 조회.
public static class TreeZoneStampPicker
{
    // 스탬프를 칸 단위로 잘라 붙이는 처리는 잠시 끈다.
    public static bool Enabled => false;

    public static bool TryPick(
        int x,
        int y,
        GridManager grid,
        ZoneManager zones,
        TreeZoneStampSet stamps,
        out TileBase tile)
    {
        tile = null;
        if (!Enabled)
        {
            return false;
        }
        if (grid == null || stamps == null || !grid.IsInBounds(x, y))
        {
            return false;
        }

        if (grid.GetCell(x, y).Type != GridCellType.Locked)
        {
            return false;
        }

        Vector2Int zone = ZoneManager.GetZoneIndex(x, y);
        int mask = BuildMaskForZone(zone.x, zone.y, zones);
        int localX = x - zone.x * TreeZoneStampSet.ZoneSize;
        int localY = y - zone.y * TreeZoneStampSet.ZoneSize;
        tile = stamps.GetTile(mask, localX, localY);
        return tile != null;
    }

    public static int BuildMaskForZone(int zoneX, int zoneY, ZoneManager zones)
    {
        bool n = IsFloorOrOutsideZone(zoneX, zoneY + 1, zones);
        bool s = IsFloorOrOutsideZone(zoneX, zoneY - 1, zones);
        bool e = IsFloorOrOutsideZone(zoneX + 1, zoneY, zones);
        bool w = IsFloorOrOutsideZone(zoneX - 1, zoneY, zones);
        return TreeZoneStampSet.BuildMask(n, s, e, w);
    }

    private static bool IsFloorOrOutsideZone(int zoneX, int zoneY, ZoneManager zones)
    {
        if (zoneX < 0 || zoneY < 0 || zoneX >= ZoneManager.ZonesX || zoneY >= ZoneManager.ZonesY)
        {
            return true;
        }

        if (zones == null)
        {
            // 시작 구역만 Floor로 가정
            return zoneX == ZoneManager.CenterZoneX && zoneY == ZoneManager.CenterZoneY;
        }

        string zoneId = ZoneManager.GetZoneId(zoneX, zoneY);
        return zones.IsZoneUnlocked(zoneId);
    }
}
