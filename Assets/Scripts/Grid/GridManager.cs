using System;
using UnityEngine;

// 그리드 데이터(지형·occupation)와 좌표 변환을 담당한다. 렌더링·배치 로직은 포함하지 않는다.
public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 64;
    [SerializeField] private GridPlane plane = GridPlane.XY;
    [SerializeField] private int tilePixelSize = 32;
    [SerializeField] private float pixelsPerUnit = 32f;
    [SerializeField] private Vector2 tilePivot = new Vector2(0.5f, 0.5f);

    private GridCell[,] cells;

    public int Width => width;
    public int Height => height;
    public GridPlane Plane => plane;
    public int TilePixelSize => tilePixelSize;
    public float PixelsPerUnit => pixelsPerUnit;
    public float CellSize => tilePixelSize / pixelsPerUnit;

    // 셀 데이터가 변경될 때 발생한다. TilemapRenderer 등이 구독한다.
    public event Action<Vector2Int, GridCell> CellChanged;

    // 그리드 배열을 초기화한다.
    private void Awake()
    {
        InitializeGrid();
    }

    // cells가 null이면 InitializeGrid를 호출한다. Awake 이전 접근 대비용.
    private void EnsureInitialized()
    {
        if (cells == null)
        {
            InitializeGrid();
        }
    }

    // width×height 크기의 셀 배열을 만들고 전부 Floor로 채운다.
    public void InitializeGrid()
    {
        cells = new GridCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new GridCell(GridCellType.Floor);
            }
        }
    }

    // (x, y)가 그리드 범위 안인지 확인한다.
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // (x, y) 셀 데이터를 반환한다. 범위 밖이면 default를 반환한다.
    public GridCell GetCell(int x, int y)
    {
        EnsureInitialized();

        if (!IsInBounds(x, y))
        {
            return default;
        }

        return cells[x, y];
    }

    // coord 셀 데이터를 반환한다.
    public GridCell GetCell(Vector2Int coord)
    {
        return GetCell(coord.x, coord.y);
    }

    // (x, y) 셀 데이터를 갱신하고 CellChanged 이벤트를 발생시킨다.
    public void SetCell(int x, int y, GridCell cell)
    {
        EnsureInitialized();

        if (!IsInBounds(x, y))
        {
            return;
        }

        cells[x, y] = cell;
        CellChanged?.Invoke(new Vector2Int(x, y), cell);
    }

    // coord 셀 데이터를 갱신한다.
    public void SetCell(Vector2Int coord, GridCell cell)
    {
        SetCell(coord.x, coord.y, cell);
    }

    // (x, y)에 점유 중인 occupant의 instanceId를 반환한다. 없으면 null.
    public string GetOccupantInstanceIdAt(int x, int y)
    {
        return GetCell(x, y).OccupantInstanceId;
    }

    // coord에 점유 중인 occupant의 instanceId를 반환한다.
    public string GetOccupantInstanceIdAt(Vector2Int coord)
    {
        return GetOccupantInstanceIdAt(coord.x, coord.y);
    }

    // 그리드 좌표 (x, y)를 월드 좌표로 변환한다. tilePivot·CellSize·plane을 반영한다.
    public Vector3 GridToWorld(int x, int y)
    {
        float localX = (x + tilePivot.x) * CellSize;
        float localY = (y + tilePivot.y) * CellSize;

        Vector3 local = plane == GridPlane.XY
            ? new Vector3(localX, localY, 0f)
            : new Vector3(localX, 0f, localY);

        return transform.TransformPoint(local);
    }

    // coord를 월드 좌표로 변환한다.
    public Vector3 GridToWorld(Vector2Int coord)
    {
        return GridToWorld(coord.x, coord.y);
    }

    // 월드 좌표를 그리드 좌표로 변환한다. FloorToInt로 셀 인덱스를 구한다.
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float cellSize = CellSize;

        float axis1 = plane == GridPlane.XY ? local.y : local.z;
        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(axis1 / cellSize);
        return new Vector2Int(x, y);
    }

    // 월드 좌표를 그리드 범위 안으로 제한한다.
    public Vector3 ClampWorldPosition(Vector3 worldPosition)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        float cellSize = CellSize;
        float maxX = width * cellSize;
        float maxAxis1 = height * cellSize;

        local.x = Mathf.Clamp(local.x, 0f, maxX);
        if (plane == GridPlane.XY)
        {
            local.y = Mathf.Clamp(local.y, 0f, maxAxis1);
        }
        else
        {
            local.z = Mathf.Clamp(local.z, 0f, maxAxis1);
        }

        return transform.TransformPoint(local);
    }

    // width×height 맵의 기하학적 중앙 월드 좌표를 반환한다.
    public Vector3 GetMapCenterWorld()
    {
        float cellSize = CellSize;
        float localX = width * cellSize * 0.5f;
        float localAxis1 = height * cellSize * 0.5f;

        Vector3 local = plane == GridPlane.XY
            ? new Vector3(localX, localAxis1, 0f)
            : new Vector3(localX, 0f, localAxis1);

        return transform.TransformPoint(local);
    }
}
