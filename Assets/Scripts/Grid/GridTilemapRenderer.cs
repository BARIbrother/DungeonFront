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

    private Dictionary<GridCellType, TileBase> tileLookup;

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
    }

    // GridManager.CellChanged 이벤트를 구독한다.
    private void OnEnable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged += OnCellChanged;
        }
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

    // GridManager.CellChanged 이벤트 구독을 해제한다.
    private void OnDisable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged -= OnCellChanged;
        }
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

        if (!tileLookup.TryGetValue(GridCellType.Floor, out TileBase floorTile) || floorTile == null)
        {
            Debug.LogWarning("[GridTilemapRenderer] GridCellType.Floor에 Tile Asset이 연결되지 않았습니다.", this);
            return false;
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
                tileLookup.TryGetValue(cell.Type, out TileBase tile);
                block[x + y * width] = tile;

                if (tile != null)
                {
                    placedCount++;
                }
            }
        }

        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, width, height, 1), block);
        tilemap.CompressBounds();
        tilemap.RefreshAllTiles();

        if (placedCount == 0)
        {
            Debug.LogWarning("[GridTilemapRenderer] 배치된 타일이 0개입니다. Tile Asset에 Sprite가 있는지 확인하세요.", this);
        }
    }

    // 단일 셀 변경 시 Tilemap 타일을 갱신한다.
    private void OnCellChanged(Vector2Int coord, GridCell cell)
    {
        if (tilemap == null || tileLookup == null)
        {
            return;
        }

        ApplyTile(coord.x, coord.y, cell);
    }

    // (x, y) 셀의 GridCellType에 맞는 Tile Asset을 Tilemap에 설정한다.
    private void ApplyTile(int x, int y, GridCell cell)
    {
        Vector3Int pos = new Vector3Int(x, y, 0);
        tileLookup.TryGetValue(cell.Type, out TileBase tile);
        tilemap.SetTile(pos, tile);
    }
}
