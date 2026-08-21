using UnityEngine;

// 추출기 방향별 텍스처. 옆모습은 좌우 flipX로 재사용한다.
public static class ExtractorArt
{
    public const string SidePath = "Assets/Art/Machines/extractor_side.png";
    public const string DownPath = "Assets/Art/Machines/extractor_down.png";
    public const string UpPath = "Assets/Art/Machines/extractor_up.png";
    public const string PrefabPath = "Assets/Prefabs/Machines/Extractor_machine.prefab";
    public const string MachineDefPath = "Assets/ItemDefinition/MachineDef/Extractor_1.asset";
    public const string MachineDefId = "Extractor_1";

    public static readonly string[] ArtPaths =
    {
        SidePath,
        DownPath,
        UpPath,
    };

    public static string PathForDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.up)
        {
            return UpPath;
        }

        if (direction == Vector2Int.down)
        {
            return DownPath;
        }

        return SidePath;
    }

    public static bool FlipXForDirection(Vector2Int direction)
    {
        return direction == Vector2Int.left;
    }
}
