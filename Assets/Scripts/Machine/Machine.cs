using System.Text;
using UnityEngine;

public abstract class Machine : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    // 그리드 footprint 크기 (가로×세로 셀 수)
    public Vector2Int size = new Vector2Int(1, 1);

    // GridManager가 배치한 footprint 좌하단 anchor
    private Vector2Int gridAnchor;

    public Vector2Int GridAnchor => gridAnchor;

    // 배치·점유 계산에 쓰는 footprint 크기. 서브클래스에서 고정값을 반환한다.
    public virtual Vector2Int GetFootprintSize() => size;

    public ItemEntryList inputPort;
    public ItemEntryList outputPort;
    public Recipe currentRecipe;

    public abstract void InitializeMachine();
    public abstract void Tick();
    public abstract void PutintoInputPort(ItemEntry IE);
    public abstract void TakeoutOutputPort(ItemEntry IE);

    // GridManager가 배치 직후 그리드 anchor를 주입한다.
    public void Initialize(Vector2Int anchor)
    {
        gridAnchor = anchor;
    }

    // footprint의 coord 칸에 이 기계를 배치할 수 있는지 확인한다.
    public virtual bool IsAvailableCellForMachine(GridManager gridManager, Vector2Int coord)
    {
        GridCell cell = gridManager.GetCell(coord);
        return cell.Type == GridCellType.Floor && !cell.IsOccupied;
    }

    // 그리드에 기록될 occupant 종류.
    public virtual OccupantKind GetOccupantKind() => OccupantKind.Machine;

    private void Start()
    {
        EnsureClickCollider();
    }

    // 클릭 시 InputPort·OutputPort 보유 아이템을 콘솔에 출력한다.
    private void OnMouseDown()
    {
        LogPortContents();
    }

    private void LogPortContents()
    {
        var log = new StringBuilder();
        log.AppendLine("InputPort:");
        AppendPortLines(log, inputPort);
        log.AppendLine("OutputPort:");
        AppendPortLines(log, outputPort);
        Debug.Log(log.ToString());
    }

    private static void AppendPortLines(StringBuilder log, ItemEntryList port)
    {
        if (port?.entries == null || port.entries.Length == 0)
        {
            return;
        }

        foreach (ItemEntry entry in port.entries)
        {
            if (entry == null || entry.item == null)
            {
                continue;
            }

            string itemName = string.IsNullOrEmpty(entry.item.displayName)
                ? entry.item.id
                : entry.item.displayName;
            log.AppendLine($"{itemName} : {entry.count}개");
        }
    }

    // OnMouseDown용 BoxCollider2D가 없으면 footprint 크기에 맞춰 추가한다.
    private void EnsureClickCollider()
    {
        if (GetComponent<Collider2D>() != null)
        {
            return;
        }

        var boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(size.x, size.y);
    }

    public virtual void ChangeRecipe(Recipe newRecipe)
    {
        ReturnPortContentsToPlayerInventory(inputPort);
        ReturnPortContentsToPlayerInventory(outputPort);

        currentRecipe = newRecipe;

        EnsurePortLists();

        int inputLength = newRecipe != null && newRecipe.inputEntryList != null
            ? newRecipe.inputEntryList.length
            : 0;
        int outputLength = newRecipe != null && newRecipe.outputEntryList != null
            ? newRecipe.outputEntryList.length
            : 0;

        inputPort.length = inputLength;
        outputPort.length = outputLength;
        inputPort.Resize();
        outputPort.Resize();
    }

    private void EnsurePortLists()
    {
        if (inputPort == null)
        {
            inputPort = new ItemEntryList();
        }

        if (outputPort == null)
        {
            outputPort = new ItemEntryList();
        }
    }

    private void ReturnPortContentsToPlayerInventory(ItemEntryList port)
    {
        if (port?.entries == null)
        {
            return;
        }

        foreach (var entry in port.entries)
        {
            if (entry == null || entry.item == null || entry.count <= 0)
            {
                continue;
            }

            AddToPlayerInventory(entry);
        }
    }

    private void AddToPlayerInventory(ItemEntry entry)
    {
        var inventory = playerInventory != null
            ? playerInventory
            : FindAnyObjectByType<PlayerInventory>();

        inventory?.Add(entry);
    }
}
