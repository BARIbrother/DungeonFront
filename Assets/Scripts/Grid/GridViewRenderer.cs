using System.Collections.Generic;
using UnityEngine;

// [레거시] 카메라 시야 내 타일만 GameObject로 렌더링한다. Tilemap 전환 후 미사용.
public class GridViewRenderer : MonoBehaviour
{
    [System.Serializable]
    private struct TilePrefabEntry
    {
        public GridCellType Type;
        public GameObject Prefab;
    }

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private int viewPadding = 2;
    [SerializeField] private TilePrefabEntry[] tilePrefabs;

    private readonly Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    private readonly Dictionary<GameObject, GameObject> tilePrefabByInstance = new Dictionary<GameObject, GameObject>();
    private readonly Dictionary<GridCellType, GameObject> prefabLookup = new Dictionary<GridCellType, GameObject>();
    private readonly Dictionary<GameObject, Stack<GameObject>> tilePools = new Dictionary<GameObject, Stack<GameObject>>();

    private RectInt lastVisibleBounds;
    private Transform tileRoot;

    // 참조를 찾고 prefab lookup·tileRoot를 초기화한다.
    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        foreach (TilePrefabEntry entry in tilePrefabs)
        {
            prefabLookup[entry.Type] = entry.Prefab;
        }

        tileRoot = new GameObject("GridTiles").transform;
        tileRoot.SetParent(transform);
    }

    // 첫 LateUpdate에서 bounds 비교가 동작하도록 lastVisibleBounds를 초기화한다.
    private void Start()
    {
        lastVisibleBounds = new RectInt(int.MinValue, int.MinValue, 0, 0);
    }

    // GridManager.CellChanged 이벤트를 구독한다.
    private void OnEnable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged += OnCellChanged;
        }
    }

    // GridManager.CellChanged 이벤트 구독을 해제한다.
    private void OnDisable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged -= OnCellChanged;
        }
    }

    // 카메라 시야 bounds가 바뀌면 보이는 타일만 spawn/release한다.
    private void LateUpdate()
    {
        if (gridManager == null || targetCamera == null)
        {
            return;
        }

        RectInt visibleBounds = GetVisibleBounds();

        if (visibleBounds == lastVisibleBounds)
        {
            return;
        }

        RefreshVisibleTiles(visibleBounds);
        lastVisibleBounds = visibleBounds;
    }

    // 화면에 표시 중인 셀의 지형이 바뀌면 해당 타일 GameObject를 교체한다.
    private void OnCellChanged(Vector2Int coord, GridCell cell)
    {
        if (!activeTiles.TryGetValue(coord, out GameObject tileObject))
        {
            return;
        }

        ReleaseTile(coord, tileObject);
        TrySpawnTile(coord, cell);
    }

    // 카메라 viewport 기준으로 현재 보이는 그리드 범위(RectInt)를 계산한다.
    private RectInt GetVisibleBounds()
    {
        float viewDistance = GetViewDistanceToGridPlane();

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, viewDistance));
        Vector3 topRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, viewDistance));

        Vector2Int minGrid = gridManager.WorldToGrid(bottomLeft);
        Vector2Int maxGrid = gridManager.WorldToGrid(topRight);

        int minX = Mathf.Min(minGrid.x, maxGrid.x);
        int minY = Mathf.Min(minGrid.y, maxGrid.y);
        int maxX = Mathf.Max(minGrid.x, maxGrid.x);
        int maxY = Mathf.Max(minGrid.y, maxGrid.y);

        minX = Mathf.Clamp(minX - viewPadding, 0, gridManager.Width - 1);
        minY = Mathf.Clamp(minY - viewPadding, 0, gridManager.Height - 1);
        maxX = Mathf.Clamp(maxX + viewPadding, 0, gridManager.Width - 1);
        maxY = Mathf.Clamp(maxY + viewPadding, 0, gridManager.Height - 1);

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    // 카메라에서 그리드 평면까지의 거리를 구한다. ViewportToWorldPoint의 z 인자로 사용.
    private float GetViewDistanceToGridPlane()
    {
        Vector3 gridPoint = gridManager.GridToWorld(0, 0);

        if (gridManager.Plane == GridPlane.XY)
        {
            return Mathf.Abs(targetCamera.transform.position.z - gridPoint.z);
        }

        return Mathf.Abs(targetCamera.transform.position.y - gridPoint.y);
    }

    // 시야 밖 타일은 풀에 반환하고, 새로 들어온 셀은 spawn한다.
    private void RefreshVisibleTiles(RectInt visibleBounds)
    {
        List<Vector2Int> toRemove = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, GameObject> pair in activeTiles)
        {
            if (!visibleBounds.Contains(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            ReleaseTile(toRemove[i], activeTiles[toRemove[i]]);
        }

        for (int x = visibleBounds.xMin; x < visibleBounds.xMax; x++)
        {
            for (int y = visibleBounds.yMin; y < visibleBounds.yMax; y++)
            {
                Vector2Int coord = new Vector2Int(x, y);

                if (activeTiles.ContainsKey(coord))
                {
                    continue;
                }

                TrySpawnTile(coord, gridManager.GetCell(coord));
            }
        }
    }

    // 셀 타입에 맞는 prefab을 풀에서 꺼내거나 생성해 배치한다.
    private void TrySpawnTile(Vector2Int coord, GridCell cell)
    {
        if (!prefabLookup.TryGetValue(cell.Type, out GameObject prefab) || prefab == null)
        {
            return;
        }

        GameObject tileObject = GetTileFromPool(prefab);
        tileObject.transform.SetParent(tileRoot, false);
        tileObject.transform.position = gridManager.GridToWorld(coord);
        tileObject.SetActive(true);
        tilePrefabByInstance[tileObject] = prefab;
        activeTiles[coord] = tileObject;
    }

    // prefab별 풀에서 재사용 가능한 GameObject를 반환한다. 없으면 Instantiate.
    private GameObject GetTileFromPool(GameObject prefab)
    {
        if (!tilePools.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            tilePools[prefab] = pool;
        }

        while (pool.Count > 0)
        {
            GameObject pooled = pool.Pop();
            if (pooled != null)
            {
                return pooled;
            }
        }

        return Instantiate(prefab);
    }

    // 타일 GameObject를 비활성화하고 prefab별 풀에 반환한다.
    private void ReleaseTile(Vector2Int coord, GameObject tileObject)
    {
        activeTiles.Remove(coord);

        if (!tilePrefabByInstance.TryGetValue(tileObject, out GameObject prefab))
        {
            Destroy(tileObject);
            return;
        }

        tilePrefabByInstance.Remove(tileObject);
        tileObject.SetActive(false);

        if (!tilePools.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            tilePools[prefab] = pool;
        }

        pool.Push(tileObject);
    }
}
