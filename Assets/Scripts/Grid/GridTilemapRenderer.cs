using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// GridManager의 지형 데이터를 Unity Tilemap에 동기화한다.
public class GridTilemapRenderer : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Tilemap tilemap;

    [System.Serializable]
    private struct TileEntry
    {
        public GridCellType Type;
        public TileBase Tile;
    }

    [SerializeField] private TileEntry[] tiles;
    [SerializeField] private TreeBorderTileSet treeBorderTileSet;
    [SerializeField] private TreeZoneStampSet treeZoneStampSet;
    [SerializeField] private Sprite floorSprite;
    [SerializeField] private Sprite[] floorDecorationSprites;
    [SerializeField] private Sprite lockedZoneSprite;

    private Dictionary<GridCellType, TileBase> tileLookup;
    private TileBase floorTile;
    private TileBase[] floorDecorationTiles;
    private ZoneManager zoneManager;
    private Transform lockedZoneRoot;
    private readonly Dictionary<Vector2Int, Transform> lockedZoneOverlays = new();
    private const int LockedTilesPerAxis = 2;

    // 타일 lookup을 구성하고 gridManager·tilemap 참조를 찾는다.
    private void Awake()
    {
        tileLookup = new Dictionary<GridCellType, TileBase>();

        if (tiles != null)
        {
            foreach (TileEntry entry in tiles)
            {
                tileLookup[entry.Type] = entry.Tile;
            }
        }

        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (tilemap == null)
        {
            tilemap = FindAnyObjectByType<Tilemap>();
        }

        zoneManager = FindAnyObjectByType<ZoneManager>();
        EnsureTreeBorderTileSet();
        EnsureTreeZoneStampSet();
        EnsureFloorTile();
        EnsureFloorDecorationTiles();
        EnsureLockedZoneSprite();
    }

    private void EnsureFloorTile()
    {
        if (floorTile != null)
        {
            return;
        }

        if (floorSprite == null)
        {
#if UNITY_EDITOR
            floorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Background/floor_grass_32.png");
#endif
        }

        if (floorSprite == null)
        {
            return;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = floorSprite;
        tile.color = Color.white;
        floorTile = tile;
        tileLookup[GridCellType.Floor] = floorTile;
    }

    private void EnsureFloorDecorationTiles()
    {
        if (floorDecorationTiles != null)
        {
            return;
        }

        if (floorDecorationSprites == null || floorDecorationSprites.Length == 0)
        {
#if UNITY_EDITOR
            LoadFloorDecorationSpritesFromFolder();
#endif
        }

        if (floorDecorationSprites == null || floorDecorationSprites.Length == 0)
        {
            floorDecorationTiles = System.Array.Empty<TileBase>();
            return;
        }

        floorDecorationTiles = new TileBase[floorDecorationSprites.Length];
        for (int i = 0; i < floorDecorationSprites.Length; i++)
        {
            Sprite sprite = floorDecorationSprites[i];
            if (sprite == null)
            {
                continue;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            floorDecorationTiles[i] = tile;
        }
    }

#if UNITY_EDITOR
    private void LoadFloorDecorationSpritesFromFolder()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(
            "floor_deco_ t:Texture2D",
            new[] { "Assets/Art/Background/Tiles/Floor" });
        if (guids == null || guids.Length == 0)
        {
            return;
        }

        var sprites = new System.Collections.Generic.List<Sprite>();
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        floorDecorationSprites = sprites.ToArray();
    }
#endif

    private void EnsureTreeBorderTileSet()
    {
        if (treeBorderTileSet != null)
        {
            return;
        }

#if UNITY_EDITOR
        treeBorderTileSet = UnityEditor.AssetDatabase.LoadAssetAtPath<TreeBorderTileSet>(
            "Assets/Data/TreeBorderTileSet.asset");
#endif
    }

    private void EnsureTreeZoneStampSet()
    {
        if (treeZoneStampSet != null)
        {
            return;
        }

#if UNITY_EDITOR
        treeZoneStampSet = UnityEditor.AssetDatabase.LoadAssetAtPath<TreeZoneStampSet>(
            "Assets/Data/TreeZoneStampSet.asset");
#endif
    }

    private void EnsureLockedZoneSprite()
    {
        if (lockedZoneSprite != null)
        {
            return;
        }

#if UNITY_EDITOR
        lockedZoneSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Background/Tiles/Tree/ZoneTemplates/locked_zone.png");
#endif
    }

    // GridManager.CellChanged 이벤트를 구독한다.
    private void OnEnable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged += OnCellChanged;
        }

        if (zoneManager == null)
        {
            zoneManager = FindAnyObjectByType<ZoneManager>();
        }

        if (zoneManager != null)
        {
            zoneManager.OnZoneUnlocked -= OnZoneUnlocked;
            zoneManager.OnZoneUnlocked += OnZoneUnlocked;
        }
    }

    private void OnDisable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged -= OnCellChanged;
        }

        if (zoneManager != null)
        {
            zoneManager.OnZoneUnlocked -= OnZoneUnlocked;
        }
    }

    private void OnZoneUnlocked(string zoneId)
    {
        RefreshAllTiles();
    }

    // 설정 검증 후 전체 타일을 Tilemap에 반영한다.
    private void Start()
    {
        if (!ValidateSetup())
        {
            return;
        }

        SyncAllTiles();
    }

    // 구역 해금·맵 부트스트랩 직후 전체 타일을 다시 맞춘다.
    public void RefreshAllTiles()
    {
        if (!ValidateSetup())
        {
            return;
        }

        SyncAllTiles();
    }

    // Inspector 연결·타일 에셋·Grid Cell Size·위치 정합성을 검사한다.
    private bool ValidateSetup()
    {
        if (gridManager == null)
        {
            Debug.LogWarning("[GridTilemapRenderer] GridManager가 연결되지 않았습니다.", this);
            return false;
        }

        if (tilemap == null)
        {
            Debug.LogWarning("[GridTilemapRenderer] Tilemap이 연결되지 않았습니다.", this);
            return false;
        }

        if (tiles == null || tiles.Length == 0)
        {
            Debug.LogWarning("[GridTilemapRenderer] Tiles 배열이 비어 있습니다. Floor 타일을 등록하세요.", this);
            return false;
        }

        if (!tileLookup.TryGetValue(GridCellType.Floor, out TileBase mappedFloor) || mappedFloor == null)
        {
            if (floorTile == null)
            {
                Debug.LogWarning("[GridTilemapRenderer] GridCellType.Floor에 Tile Asset이 연결되지 않았습니다.", this);
                return false;
            }
        }

        Grid grid = tilemap.layoutGrid;
        if (grid != null)
        {
            float expectedCellSize = gridManager.CellSize;
            if (!Mathf.Approximately(grid.cellSize.x, expectedCellSize)
                || !Mathf.Approximately(grid.cellSize.y, expectedCellSize))
            {
                Debug.LogWarning(
                    $"[GridTilemapRenderer] Grid Cell Size({grid.cellSize.x}, {grid.cellSize.y})가 " +
                    $"GridManager Cell Size({expectedCellSize})와 다릅니다. Grid Inspector에서 Cell Size를 맞추세요.",
                    this);
            }

            if (Vector3.Distance(gridManager.transform.position, grid.transform.position) > 0.01f)
            {
                Debug.LogWarning(
                    "[GridTilemapRenderer] GridManager와 Tilemap Grid의 위치가 다릅니다. " +
                    "같은 GameObject에 두거나 위치를 (0,0,0)으로 맞추세요.",
                    this);
            }
        }

        return true;
    }

    // GridManager 전체 셀을 읽어 Tilemap에 SetTilesBlock으로 한 번에 배치한다.
    private void SyncAllTiles()
    {
        int width = gridManager.Width;
        int height = gridManager.Height;
        TileBase[] block = new TileBase[width * height];
        int placedCount = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GridCell cell = gridManager.GetCell(x, y);
                TileBase tile = ResolveTile(x, y, cell);
                block[x + y * width] = tile;

                if (tile != null)
                {
                    placedCount++;
                }
            }
        }

        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), block);
        SyncExteriorUnknownTiles();
        SyncLockedZoneOverlays();
        tilemap.CompressBounds();
        tilemap.RefreshAllTiles();

        if (placedCount == 0)
        {
            Debug.LogWarning("[GridTilemapRenderer] 배치된 타일이 0개입니다. Tile Asset에 Sprite가 있는지 확인하세요.", this);
        }
    }

    // 필드(그리드) 밖을 미확인 Locked 숲으로 채운다. 두께는 구역 1(=16칸)을 기준으로
    // 카메라 시야 반경보다 얇지 않게 맞춘다.
    private void SyncExteriorUnknownTiles()
    {
        if (tilemap == null || gridManager == null)
        {
            return;
        }

        int width = gridManager.Width;
        int height = gridManager.Height;
        int pad = GetExteriorPadCells();

        // 이전보다 얇아질 수 있어, 여유 있게 옛 외곽을 지운 뒤 다시 깐다.
        int clearPad = pad + ZoneManager.ZoneSize;
        for (int y = -clearPad; y < height + clearPad; y++)
        {
            for (int x = -clearPad; x < width + clearPad; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    continue;
                }

                tilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }

    // 미해금(·맵 밖) 구역마다 16×16 이미지를 한 장씩 올린다. 칸 단위 슬라이스는 쓰지 않는다.
    private void SyncLockedZoneOverlays()
    {
        EnsureLockedZoneSprite();
        if (gridManager == null)
        {
            return;
        }

        if (lockedZoneSprite == null)
        {
            ClearLockedZoneOverlays();
            return;
        }

        if (lockedZoneRoot == null)
        {
            var rootObject = new GameObject("LockedZoneOverlays");
            rootObject.transform.SetParent(transform, false);
            lockedZoneRoot = rootObject.transform;
        }

        int pad = GetExteriorPadCells();
        int zonePad = Mathf.Max(1, Mathf.CeilToInt(pad / (float)ZoneManager.ZoneSize));
        var needed = new HashSet<Vector2Int>();
        for (int zoneY = -zonePad; zoneY < ZoneManager.ZonesY + zonePad; zoneY++)
        {
            for (int zoneX = -zonePad; zoneX < ZoneManager.ZonesX + zonePad; zoneX++)
            {
                if (IsZoneUsingLockedOverlay(zoneX, zoneY))
                {
                    needed.Add(new Vector2Int(zoneX, zoneY));
                }
            }
        }

        List<Vector2Int> toRemove = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, Transform> pair in lockedZoneOverlays)
        {
            if (!needed.Contains(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Vector2Int key = toRemove[i];
            if (lockedZoneOverlays.TryGetValue(key, out Transform overlay) && overlay != null)
            {
                Destroy(overlay.gameObject);
            }

            lockedZoneOverlays.Remove(key);
        }

        foreach (Vector2Int zone in needed)
        {
            PlaceLockedZoneOverlay(zone);
        }
    }

    private bool IsZoneUsingLockedOverlay(int zoneX, int zoneY)
    {
        bool inMap = zoneX >= 0
            && zoneX < ZoneManager.ZonesX
            && zoneY >= 0
            && zoneY < ZoneManager.ZonesY;
        if (!inMap)
        {
            return true;
        }

        if (zoneManager != null)
        {
            return !zoneManager.IsZoneUnlocked(zoneX, zoneY);
        }

        return !(zoneX == ZoneManager.CenterZoneX && zoneY == ZoneManager.CenterZoneY);
    }

    private void PlaceLockedZoneOverlay(Vector2Int zone)
    {
        if (!lockedZoneOverlays.TryGetValue(zone, out Transform overlayRoot) || overlayRoot == null)
        {
            var overlayObject = new GameObject($"LockedZone_{zone.x}_{zone.y}");
            overlayObject.transform.SetParent(lockedZoneRoot, false);
            overlayRoot = overlayObject.transform;
            lockedZoneOverlays[zone] = overlayRoot;
        }

        int tileCells = ZoneManager.ZoneSize / LockedTilesPerAxis;
        Vector2 spriteSize = lockedZoneSprite.bounds.size;
        if (spriteSize.x < 0.0001f || spriteSize.y < 0.0001f)
        {
            return;
        }

        float tileWorld = tileCells * gridManager.CellSize;
        float scaleX = tileWorld / spriteSize.x;
        float scaleY = tileWorld / spriteSize.y;
        int sortingLayer = GetTilemapSortingLayerId();
        int sortingOrder = GetTilemapSortingOrder() - 1;

        int childIndex = 0;
        for (int tileY = 0; tileY < LockedTilesPerAxis; tileY++)
        {
            for (int tileX = 0; tileX < LockedTilesPerAxis; tileX++)
            {
                SpriteRenderer renderer = GetOrCreateOverlayTile(overlayRoot, childIndex);
                childIndex++;

                renderer.sprite = lockedZoneSprite;
                renderer.color = Color.white;
                renderer.drawMode = SpriteDrawMode.Simple;
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                int minX = zone.x * ZoneManager.ZoneSize + tileX * tileCells;
                int minY = zone.y * ZoneManager.ZoneSize + tileY * tileCells;
                Vector3 minCell = gridManager.GridToWorld(minX, minY);
                Vector3 maxCell = gridManager.GridToWorld(
                    minX + tileCells - 1,
                    minY + tileCells - 1);
                renderer.transform.position = (minCell + maxCell) * 0.5f;
            }
        }
    }

    private static SpriteRenderer GetOrCreateOverlayTile(Transform parent, int index)
    {
        if (index < parent.childCount)
        {
            SpriteRenderer existing = parent.GetChild(index).GetComponent<SpriteRenderer>();
            if (existing != null)
            {
                return existing;
            }
        }

        var tileObject = new GameObject($"Tile_{index}");
        tileObject.transform.SetParent(parent, false);
        return tileObject.AddComponent<SpriteRenderer>();
    }

    private void ClearLockedZoneOverlays()
    {
        foreach (KeyValuePair<Vector2Int, Transform> pair in lockedZoneOverlays)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        lockedZoneOverlays.Clear();
    }

    private int GetTilemapSortingLayerId()
    {
        TilemapRenderer tilemapRenderer = tilemap != null ? tilemap.GetComponent<TilemapRenderer>() : null;
        return tilemapRenderer != null ? tilemapRenderer.sortingLayerID : 0;
    }

    private int GetTilemapSortingOrder()
    {
        TilemapRenderer tilemapRenderer = tilemap != null ? tilemap.GetComponent<TilemapRenderer>() : null;
        return tilemapRenderer != null ? tilemapRenderer.sortingOrder : 0;
    }

    private int GetExteriorPadCells()
    {
        int pad = ZoneManager.ZoneSize;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float cellSize = Mathf.Max(0.0001f, gridManager.CellSize);
            int cameraPad = Mathf.CeilToInt(
                Mathf.Max(cam.orthographicSize, cam.orthographicSize * cam.aspect) / cellSize) + 1;
            pad = Mathf.Max(pad, cameraPad);
        }

        return pad;
    }

    // 단일 셀 변경 시 Tilemap 타일을 갱신한다. 숲 경계는 인접 Locked 셀도 함께 갱신한다.
    private void OnCellChanged(Vector2Int coord, GridCell cell)
    {
        if (tilemap == null || tileLookup == null)
        {
            return;
        }

        ApplyTile(coord.x, coord.y, cell);
    }

    // (x, y) 셀의 GridCellType·숲 경계 규칙에 맞는 Tile Asset을 Tilemap에 설정한다.
    private void ApplyTile(int x, int y, GridCell cell)
    {
        Vector3Int pos = new Vector3Int(x, y, 0);
        tilemap.SetTile(pos, ResolveTile(x, y, cell));
    }

    private TileBase ResolveTile(int x, int y, GridCell cell)
    {
        if (cell.Type == GridCellType.Floor)
        {
            return ResolveFloorTile(x, y);
        }

        if (cell.Type == GridCellType.Locked)
        {
            // 미해금 구역은 칸 타일 대신 16×16 오버레이 이미지를 쓴다.
            return null;
        }

        tileLookup.TryGetValue(cell.Type, out TileBase tile);
        return tile;
    }

    private TileBase ResolveFloorTile(int x, int y)
    {
        if (floorDecorationTiles != null && floorDecorationTiles.Length > 0)
        {
            int index = FloorTilePicker.PickIndex(x, y, floorDecorationTiles.Length);
            if (index >= 0 && index < floorDecorationTiles.Length && floorDecorationTiles[index] != null)
            {
                return floorDecorationTiles[index];
            }
        }

        return floorTile;
    }
}
