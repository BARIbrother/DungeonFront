using System.Collections.Generic;
using UnityEngine;

// MapNodeLayout 설계도의 모든 노드를 시작 시 사전 배치한다.
// 미해금 구역 노드는 숨기고, 구역 해금 후에 보이며 채굴 가능하다.
public class MapNodeLayoutApplier : MonoBehaviour
{
    [SerializeField] private MapNodeLayout layout;
    [SerializeField] private GridManager gridManager;

    // 이 Applier가 배치한 노드 좌표 (NewGame 시 제거 대상)
    private readonly List<Vector2Int> appliedCoords = new List<Vector2Int>();

    private MapNodeLayoutEntry[] cachedDefaultEntries;
    private bool sessionBound;
    private bool zoneBound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<MapNodeLayoutApplier>() != null)
        {
            return;
        }

        if (FindAnyObjectByType<GridManager>() == null)
        {
            return;
        }

        var systemObject = new GameObject("MapNodeLayoutApplier");
        systemObject.AddComponent<MapNodeLayoutApplier>();
    }

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }
    }

    private void OnEnable()
    {
        TryBindZone();
        TryBindSession();
    }

    private void Start()
    {
        TryBindZone();
        TryBindSession();
        ApplyAllLayout();
    }

    private void OnDisable()
    {
        UnbindZone();
        UnbindSession();
    }

    private void TryBindZone()
    {
        if (zoneBound || ZoneManager.Instance == null)
        {
            return;
        }

        ZoneManager.Instance.OnZoneUnlocked -= HandleZoneUnlocked;
        ZoneManager.Instance.OnZoneUnlocked += HandleZoneUnlocked;
        zoneBound = true;
    }

    private void UnbindZone()
    {
        zoneBound = false;

        if (ZoneManager.Instance == null)
        {
            return;
        }

        ZoneManager.Instance.OnZoneUnlocked -= HandleZoneUnlocked;
    }

    private void TryBindSession()
    {
        if (sessionBound || GameSessionState.Instance == null)
        {
            return;
        }

        GameSessionState.Instance.OnNewGame -= HandleNewGame;
        GameSessionState.Instance.OnNewGame += HandleNewGame;
        sessionBound = true;
    }

    private void UnbindSession()
    {
        sessionBound = false;

        if (GameSessionState.Instance == null)
        {
            return;
        }

        GameSessionState.Instance.OnNewGame -= HandleNewGame;
    }

    private void HandleNewGame()
    {
        ClearAppliedNodes();
        ApplyAllLayout();
    }

    private void HandleZoneUnlocked(string zoneId)
    {
        RevealNodesInZone(zoneId);
    }

    // 해금 여부와 관계없이 레이아웃의 모든 노드를 배치한다. 미해금 구역은 숨긴다.
    private void ApplyAllLayout()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogWarning("[MapNodeLayoutApplier] GridManager가 없어 노드를 배치할 수 없습니다.");
            return;
        }

        MapNodeLayoutEntry[] entries = GetEntries();
        for (int i = 0; i < entries.Length; i++)
        {
            TryApplyEntry(entries[i]);
        }
    }

    private void TryApplyEntry(MapNodeLayoutEntry entry)
    {
        if (appliedCoords.Contains(entry.gridCoord))
        {
            return;
        }

        string itemId = string.IsNullOrEmpty(entry.itemId) ? "iron_ore" : entry.itemId;
        if (!gridManager.TryPlaceResourceNode(entry.gridCoord, itemId))
        {
            Debug.LogWarning(
                $"[MapNodeLayoutApplier] 노드 배치 실패: zone={entry.zoneId} coord={entry.gridCoord} item={itemId}");
            return;
        }

        appliedCoords.Add(entry.gridCoord);
        SetNodeVisible(entry.gridCoord, IsZoneUnlocked(entry.zoneId));
    }

    // 해금된 구역의 숨겨 둔 노드를 다시 보이게 한다.
    private void RevealNodesInZone(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId))
        {
            return;
        }

        MapNodeLayoutEntry[] entries = GetEntries();
        for (int i = 0; i < entries.Length; i++)
        {
            MapNodeLayoutEntry entry = entries[i];
            if (entry.zoneId != zoneId)
            {
                continue;
            }

            SetNodeVisible(entry.gridCoord, true);
        }
    }

    private void SetNodeVisible(Vector2Int coord, bool visible)
    {
        if (gridManager == null)
        {
            return;
        }

        GridCell cell = gridManager.GetCell(coord);
        if (cell.OccupantKind != OccupantKind.ResourceNode || cell.Occupant == null)
        {
            return;
        }

        cell.Occupant.SetActive(visible);
        gridManager.SetResourceNodeVisible(coord, visible);
    }

    private static bool IsZoneUnlocked(string zoneId)
    {
        if (string.IsNullOrEmpty(zoneId))
        {
            return false;
        }

        if (ZoneManager.Instance != null)
        {
            return ZoneManager.Instance.IsZoneUnlocked(zoneId);
        }

        return zoneId == ZoneManager.CenterZoneId;
    }

    // 이 Applier가 깐 노드만 제거한다 (세이브 wipe가 아님).
    private void ClearAppliedNodes()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            appliedCoords.Clear();
            return;
        }

        for (int i = 0; i < appliedCoords.Count; i++)
        {
            // 제거 전에 활성화해야 Destroy·그리드 정리가 안정적이다.
            SetNodeVisible(appliedCoords[i], true);
            gridManager.TryRemoveResourceNode(appliedCoords[i]);
        }

        appliedCoords.Clear();
    }

    private MapNodeLayoutEntry[] GetEntries()
    {
        if (layout != null && layout.Entries != null && layout.Entries.Length > 0)
        {
            return layout.Entries;
        }

        if (cachedDefaultEntries == null)
        {
            cachedDefaultEntries = MapNodeLayout.CreateDefaultEntries();
        }

        return cachedDefaultEntries;
    }
}
