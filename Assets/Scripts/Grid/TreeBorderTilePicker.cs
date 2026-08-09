using UnityEngine;

// Locked 셀 좌표·주변 Floor 여부로 숲 경계 타일 종류·로컬 인덱스를 고른다.
// 가로 경계(잔디↔숲)는 맞닿은 1칸만 edge/fringe.
// 세로로 줄기 끝(팁·밑동)은 Floor/맵 끝줄에만 쓰고, 중간은 잎+줄기 몸통만 쓴다.
public static class TreeBorderTilePicker
{
    public const int EdgeDepth = 1;
    public const int FringeDepth = 1;

    // edge/fringe 세로 슬라이스: 0=밑동, 1·2=중간(잎+줄기), 3=꼭대기 팁
    private const int LocalYBottom = 0;
    private const int LocalYTop = 3;

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

        GridCellType here = grid.GetCell(x, y).Type;
        if (here != GridCellType.Locked)
        {
            return false;
        }

        int localY = PickLocalY(x, y, mapWidth, mapHeight, grid);

        // 맵 바깥쪽 한 줄: fringe
        if (x == 0)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.FringeLeft,
                LocalX = 0,
                LocalY = localY,
            };
            return true;
        }

        if (x == mapWidth - 1)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.FringeRight,
                LocalX = 1,
                LocalY = localY,
            };
            return true;
        }

        int distFloorLeft = DistanceToFloor(x, y, -1, mapWidth, mapHeight, grid);
        int distFloorRight = DistanceToFloor(x, y, 1, mapWidth, mapHeight, grid);

        // Floor와 가로로 맞닿은 1칸만 edge (끝나는 줄)
        if (distFloorLeft == 1)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.EdgeLeft,
                LocalX = 0,
                LocalY = localY,
            };
            return true;
        }

        if (distFloorRight == 1)
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.EdgeRight,
                LocalX = 2,
                LocalY = localY,
            };
            return true;
        }

        // 세로로만 Floor와 맞닿은 줄: 잎 아래/위를 줄기 끝 타일로 막는다.
        if (IsFloorOrOut(x, y - 1, mapWidth, mapHeight, grid))
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.EdgeLeft,
                LocalX = 1,
                LocalY = LocalYBottom,
            };
            return true;
        }

        if (IsFloorOrOut(x, y + 1, mapWidth, mapHeight, grid))
        {
            selection = new Selection
            {
                Kind = TreeBorderTileKind.EdgeLeft,
                LocalX = 1,
                LocalY = LocalYTop,
            };
            return true;
        }

        selection = new Selection
        {
            Kind = TreeBorderTileKind.Fill,
            LocalX = Mod(x, 2),
            LocalY = Mod(y, 2),
        };
        return true;
    }

    // 밑동·팁은 세로 끝줄에만. 중간은 1·2만 번갈아 써서 끝이 반복되지 않게 한다.
    private static int PickLocalY(int x, int y, int mapWidth, int mapHeight, GridManager grid)
    {
        if (IsFloorOrOut(x, y - 1, mapWidth, mapHeight, grid))
        {
            return LocalYBottom;
        }

        if (IsFloorOrOut(x, y + 1, mapWidth, mapHeight, grid))
        {
            return LocalYTop;
        }

        return 1 + Mod(y, 2);
    }

    private static bool IsFloorOrOut(int x, int y, int mapWidth, int mapHeight, GridManager grid)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
        {
            return true;
        }

        return grid.GetCell(x, y).Type == GridCellType.Floor;
    }

    private static int DistanceToFloor(
        int x,
        int y,
        int direction,
        int mapWidth,
        int mapHeight,
        GridManager grid)
    {
        for (int step = 1; step <= mapWidth; step++)
        {
            int nx = x + direction * step;
            if (nx < 0 || nx >= mapWidth || y < 0 || y >= mapHeight)
            {
                return int.MaxValue;
            }

            GridCellType type = grid.GetCell(nx, y).Type;
            if (type == GridCellType.Floor)
            {
                return step;
            }

            if (type != GridCellType.Locked)
            {
                return int.MaxValue;
            }
        }

        return int.MaxValue;
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
