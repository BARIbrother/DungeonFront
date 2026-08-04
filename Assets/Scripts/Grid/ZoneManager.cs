using System;
using System.Collections.Generic;
using UnityEngine;

// 16×16 구역 · 가로 3 × 세로 4. 시작 구역(0,0)=zone_start만 Floor, 해금 시 Floor로 전환.
public class ZoneManager : MonoBehaviour
{
    public const int ZoneSize = 16;
    public const int ZonesX = 3;
    public const int ZonesY = 4;
    public const int CenterZoneX = 0;
    public const int CenterZoneY = 0;
    public const string CenterZoneId = "zone_start";

    private static ZoneManager instance;

    [SerializeField] private GridManager gridManager;

    // 해금된 zoneId 목록
    private readonly HashSet<string> unlockedZoneIds = new HashSet<string>();

    public static ZoneManager Instance => instance;

    // 구역 해금에 성공했을 때 zoneId를 넘긴다.
    public event Action<string> OnZoneUnlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<ZoneManager>() != null)
        {
            return;
        }

        var systemObject = new GameObject("ZoneManager");
        systemObject.AddComponent<ZoneManager>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        unlockedZoneIds.Clear();
        unlockedZoneIds.Add(CenterZoneId);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // 그리드 좌표가 속한 구역 인덱스 (0..ZonesX-1, 0..ZonesY-1)를 반환한다.
    public static Vector2Int GetZoneIndex(int gridX, int gridY)
    {
        return new Vector2Int(gridX / ZoneSize, gridY / ZoneSize);
    }

    // 구역 인덱스로 zoneId를 만든다. 시작 구역은 zone_start.
    public static string GetZoneId(int zoneX, int zoneY)
    {
        if (zoneX == CenterZoneX && zoneY == CenterZoneY)
        {
            return CenterZoneId;
        }

        return $"zone_{zoneX}_{zoneY}";
    }

    // 구역이 이미 해금됐는지 확인한다.
    public bool IsZoneUnlocked(int zoneX, int zoneY)
    {
        return IsZoneUnlocked(GetZoneId(zoneX, zoneY));
    }

    // zoneId가 이미 해금됐는지 확인한다.
    public bool IsZoneUnlocked(string zoneId)
    {
        return !string.IsNullOrEmpty(zoneId) && unlockedZoneIds.Contains(zoneId);
    }

    // 해금된 구역과 상하좌우로 맞닿은 잠긴 구역만 해금 가능하다. 대각선은 불가.
    public bool CanUnlockZone(int zoneX, int zoneY)
    {
        if (zoneX < 0 || zoneX >= ZonesX || zoneY < 0 || zoneY >= ZonesY)
        {
            return false;
        }

        if (zoneX == CenterZoneX && zoneY == CenterZoneY)
        {
            return false;
        }

        if (IsZoneUnlocked(zoneX, zoneY))
        {
            return false;
        }

        return IsOrthogonallyAdjacentToUnlocked(zoneX, zoneY);
    }

    // 상하좌우 네 칸 중 이미 해금된 구역이 있는지 확인한다.
    private bool IsOrthogonallyAdjacentToUnlocked(int zoneX, int zoneY)
    {
        return IsZoneUnlocked(zoneX - 1, zoneY)
            || IsZoneUnlocked(zoneX + 1, zoneY)
            || IsZoneUnlocked(zoneX, zoneY - 1)
            || IsZoneUnlocked(zoneX, zoneY + 1);
    }

    // 잠긴 구역을 Floor로 전환한다. 시작·이미 해금·비인접·범위 밖이면 false.
    public bool TryUnlockZone(int zoneX, int zoneY)
    {
        if (!CanUnlockZone(zoneX, zoneY))
        {
            return false;
        }

        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogWarning("[ZoneManager] GridManager가 없어 구역을 해금할 수 없습니다.");
            return false;
        }

        string zoneId = GetZoneId(zoneX, zoneY);

        int minX = zoneX * ZoneSize;
        int minY = zoneY * ZoneSize;
        int maxX = minX + ZoneSize;
        int maxY = minY + ZoneSize;

        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                // 사전 배치된 자원 노드·그 위 채굴기는 Locked를 유지한다.
                if (cell.OccupantKind == OccupantKind.ResourceNode
                    || cell.OccupantKind == OccupantKind.MachineOnResourceNode)
                {
                    continue;
                }

                cell.Type = GridCellType.Floor;
                gridManager.SetCell(x, y, cell);
            }
        }

        unlockedZoneIds.Add(zoneId);
        Debug.Log($"[ZoneManager] 구역 해금: {zoneId} ({zoneX},{zoneY})");
        OnZoneUnlocked?.Invoke(zoneId);
        return true;
    }
}
