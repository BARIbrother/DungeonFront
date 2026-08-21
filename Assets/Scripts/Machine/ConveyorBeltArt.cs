using UnityEngine;

// 컨베이어 벨트 1개의 방향별 텍스처. conv_xty에서 x=받는 쪽, y=보내는 쪽 (l/r/u/d).
public static class ConveyorBeltArt
{
    public const string SheetPath = "Assets/Art/Machines/conv_belt.png";
    public const string PrefabPath = "Assets/Prefabs/Machines/ConveyerBelt_machine.prefab";
    public const string DefaultSpriteId = "conv_ltr";
    public const int TileSize = 32;
    public const int SheetColumns = 4;
    public const int SheetRows = 3;

    public static readonly string[] SpriteIds =
    {
        "conv_ltr",
        "conv_rtl",
        "conv_utd",
        "conv_dtu",
        "conv_ltu",
        "conv_ltd",
        "conv_rtu",
        "conv_rtd",
        "conv_utl",
        "conv_utr",
        "conv_dtl",
        "conv_dtr",
    };

    public static char ToLetter(Vector2Int direction)
    {
        if (direction == Vector2Int.left)
        {
            return 'l';
        }

        if (direction == Vector2Int.right)
        {
            return 'r';
        }

        if (direction == Vector2Int.up)
        {
            return 'u';
        }

        if (direction == Vector2Int.down)
        {
            return 'd';
        }

        return 'r';
    }

    public static string SpriteId(Vector2Int receive, Vector2Int send)
    {
        return $"conv_{ToLetter(receive)}t{ToLetter(send)}";
    }

    public static Vector2Int StraightReceive(Vector2Int send)
    {
        return send == Vector2Int.zero ? Vector2Int.left : -send;
    }

    public static readonly Vector2Int[] Cardinals =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };

    // 직각으로만 꺾인다. 같은 방향 직진·정면(반대 방향) 만남은 코너가 아니다.
    public static bool IsTurnFeed(Vector2Int feederSend, Vector2Int targetSend)
    {
        if (feederSend == Vector2Int.zero || targetSend == Vector2Int.zero)
        {
            return false;
        }

        return feederSend != targetSend && feederSend != -targetSend;
    }
}
