using UnityEngine;

// Locked 셀: 밑(16x4: 바닥그라드+나무7)·옆(SIDE)·가운데(MID).
public static class TreeBorderTilePicker
{
    // row0=floor south, row1-3=tree. cols 0/15=floor side, 1-14=7 trees.
    public const int BottomBandDepth = 4;
    public const int BottomPeriodX = 16;
    public const int BottomPeriodY = 4;
    public const int SidePeriodY = 4;
    public const int MidPeriod = 2;

    public const int EdgeDepth = BottomBandDepth;
    public const int FringeDepth = 1;

    public struct Selection
    {
        public TreeBorderTileKind Kind;
        public int LocalX;
        public int LocalY;
    }

    public static bool TryPick(
        int x,
        int y,
        int mapWidth,
        int mapHeight,
        GridManager grid,
        out Selection selection)
    {
        selection = default;
        if (grid == null || !grid.IsInBounds(x, y))
        {
            return false;
        }

        if (grid.GetCell(x, y).Type != GridCellType.Locked)
        {
            return false;
        }

        bool floorNorth = IsFloorOrOut(x, y + 1, mapWidth, mapHeight, grid);
        bool floorWest = IsFloorOrOut(x - 1, y, mapWidth, mapHeight, grid);
        bool floorEast = IsFloorOrOut(x + 1, y, mapWidth, mapHeight, grid);

        int distSouth = DistanceToFloor(x, y, 0, -1, mapWidth, mapHeight, grid);
        bool onLeft = x == 0 || DistanceToFloor(x, y, -1, 0, mapWidth, mapHeight, grid) == 1;
        bool onRight = x == mapWidth - 1 || DistanceToFloor(x, y, 1, 0, mapWidth, mapHeight, grid) == 1;

        // 1) 위쪽 코너
        if (floorNorth && floorWest)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.CornerTopLeft,
                LocalX = 0,
                LocalY = SidePeriodY - 1,
            };
            return true;
        }

        if (floorNorth && floorEast)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.CornerTopRight,
                LocalX = 0,
                LocalY = SidePeriodY - 1,
            };
            return true;
        }

        // 2) 밑 — Floor 남쪽 4층: 0=바닥그라드, 1-3=나무(한 칸 위)
        if (distSouth >= 1 && distSouth <= BottomBandDepth)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.Bottom,
                LocalX = Mod(x, BottomPeriodX),
                LocalY = distSouth - 1,
            };
            return true;
        }

        // 3) 옆 — 좌·우 경계 (밑 밴드 위)
        if (onLeft)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.SideLeft,
                LocalX = 0,
                LocalY = Mod(y, SidePeriodY),
            };
            return true;
        }

        if (onRight)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.SideRight,
                LocalX = 0,
                LocalY = Mod(y, SidePeriodY),
            };
            return true;
        }

        // 4) 위쪽 직선
        if (floorNorth)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.EdgeTop,
                LocalX = Mod(x, MidPeriod),
                LocalY = 1,
            };
            return true;
        }

        // 5) 가운데
        selection = new Selection
        {
            Kind = TreeBorderTileKind.Mid,
            LocalX = Mod(x, MidPeriod),
            LocalY = Mod(y, MidPeriod),
        };
        return true;
    }

    private static int DistanceToFloor(
        int x,
        int y,
        int dirX,
        int dirY,
        int mapWidth,
        int mapHeight,
        GridManager grid)
    {
        for (int step = 1; step <= mapWidth + mapHeight; step++)
        {
            int nx = x + dirX * step;
            int ny = y + dirY * step;
            if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight)
            {
                return step;
            }

            if (grid.GetCell(nx, ny).Type == GridCellType.Floor)
            {
                return step;
            }

            if (grid.GetCell(nx, ny).Type != GridCellType.Locked)
            {
                return int.MaxValue;
            }
        }

        return int.MaxValue;
    }

    private static bool IsFloorOrOut(int x, int y, int mapWidth, int mapHeight, GridManager grid)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            return true;
        }

        return grid.GetCell(x, y).Type == GridCellType.Floor;
    }

    private static int Mod(int value, int period)
    {
        if (period <= 0)
        {
            return 0;
        }

        int result = value % period;
        return result < 0 ? result + period : result;
    }
}
