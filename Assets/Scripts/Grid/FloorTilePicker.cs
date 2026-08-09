// Floor 셀에 민짜/장식 잔디를 고른다. 좌표 해시로 간헐·결정적 배치.
public static class FloorTilePicker
{
    // 약 1/6 칸에 장식 (0~999 기준)
    public const int DecorationThreshold = 170;

    public static int PickIndex(int x, int y, int decorationCount)
    {
        if (decorationCount <= 0)
        {
            return -1;
        }

        int hash = Hash(x, y);
        if ((hash % 1000) >= DecorationThreshold)
        {
            return -1;
        }

        return (hash / 1000) % decorationCount;
    }

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663;
            h ^= h >> 13;
            h *= 1274126177;
            return h & 0x7fffffff;
        }
    }
}
