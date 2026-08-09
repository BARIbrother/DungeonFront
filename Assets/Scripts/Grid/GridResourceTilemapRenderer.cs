using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// GridCell.ResourceItemId를 별도 Tilemap(광석 레이어)에 동기화한다.
public class GridResourceTilemapRenderer : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Tilemap resourceTilemap;

    [System.Serializable]
    private struct ResourceTileEntry
    {
        public string ItemId;
        public TileBase Tile;
    }

    [SerializeField] private ResourceTileEntry[] resourceTiles;

    [SerializeField] private Sprite ironOreFallbackSprite;

    private Dictionary<string, TileBase> tileLookup;

    private void Awake()
    {
        BuildLookup();
        EnsureRuntimeTiles();
        ResolveReferences();
        EnsureResourceTilemapExists();
    }

    private void OnEnable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged += OnCellChanged;
        }
    }

    private void Start()
    {
        if (!ValidateSetup())
        {
            return;
        }

        SyncAllResourceTiles();
    }

    private void OnDisable()
    {
        if (gridManager != null)
        {
            gridManager.CellChanged -= OnCellChanged;
        }
    }

    public void RefreshAllResourceTiles()
    {
        if (!ValidateSetup())
        {
            return;
        }

        SyncAllResourceTiles();
    }

    private void BuildLookup()
    {
        tileLookup = new Dictionary<string, TileBase>();
        if (resourceTiles == null)
        {
            return;
        }

        foreach (ResourceTileEntry entry in resourceTiles)
        {
            if (string.IsNullOrEmpty(entry.ItemId) || entry.Tile == null)
            {
                continue;
            }

            tileLookup[entry.ItemId] = entry.Tile;
        }
    }

    private void ResolveReferences()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (resourceTilemap != null)
        {
            return;
        }

        Grid grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            return;
        }

        Transform resourcesTransform = grid.transform.Find("Tilemap_resources");
        if (resourcesTransform != null)
        {
            resourceTilemap = resourcesTransform.GetComponent<Tilemap>();
        }
    }

    private void EnsureResourceTilemapExists()
    {
        if (resourceTilemap != null)
        {
            return;
        }

        Grid grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            return;
        }

        var tilemapObject = new GameObject("Tilemap_resources");
        tilemapObject.transform.SetParent(grid.transform, false);
        resourceTilemap = tilemapObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = 1;

        Tilemap background = null;
        Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>();
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i].name == "Tilemap_bg")
            {
                background = tilemaps[i];
                break;
            }
        }

        if (background != null)
        {
            TilemapRenderer backgroundRenderer = background.GetComponent<TilemapRenderer>();
            if (backgroundRenderer != null)
            {
                renderer.sortingLayerID = backgroundRenderer.sortingLayerID;
            }
        }
    }

    private void EnsureRuntimeTiles()
    {
        if (tileLookup == null)
        {
            tileLookup = new Dictionary<string, TileBase>();
        }

        if (tileLookup.ContainsKey("iron_ore"))
        {
            return;
        }

#if UNITY_EDITOR
        if (ironOreFallbackSprite == null)
        {
            ironOreFallbackSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/ResourceNodes/iron_ore_placeholder.png");
        }
#endif

        if (ironOreFallbackSprite == null)
        {
            return;
        }

        var runtimeTile = ScriptableObject.CreateInstance<Tile>();
        runtimeTile.sprite = ironOreFallbackSprite;
        tileLookup["iron_ore"] = runtimeTile;
    }

    private bool ValidateSetup()
    {
        ResolveReferences();

        if (gridManager == null)
        {
            Debug.LogWarning("[GridResourceTilemapRenderer] GridManager가 연결되지 않았습니다.", this);
            return false;
        }

        if (resourceTilemap == null)
        {
            Debug.LogWarning("[GridResourceTilemapRenderer] Tilemap_resources가 없습니다. 에디터 메뉴로 씬을 설정하세요.", this);
            return false;
        }

        if (tileLookup == null || tileLookup.Count == 0)
        {
            Debug.LogWarning("[GridResourceTilemapRenderer] resourceTiles가 비어 있습니다.", this);
            return false;
        }

        return true;
    }

    private void SyncAllResourceTiles()
    {
        int width = gridManager.Width;
        int height = gridManager.Height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                ApplyResourceTile(x, y, gridManager.GetCell(x, y));
            }
        }

        resourceTilemap.CompressBounds();
        resourceTilemap.RefreshAllTiles();
    }

    private void OnCellChanged(Vector2Int coord, GridCell cell)
    {
        if (resourceTilemap == null)
        {
            return;
        }

        ApplyResourceTile(coord.x, coord.y, cell);
    }

    private void ApplyResourceTile(int x, int y, GridCell cell)
    {
        Vector3Int pos = new Vector3Int(x, y, 0);
        if (!cell.HasResourceNode || !cell.ResourceNodeVisible)
        {
            resourceTilemap.SetTile(pos, null);
            return;
        }

        if (tileLookup == null || !tileLookup.TryGetValue(cell.ResourceItemId, out TileBase tile))
        {
            resourceTilemap.SetTile(pos, null);
            return;
        }

        resourceTilemap.SetTile(pos, tile);
    }
}
