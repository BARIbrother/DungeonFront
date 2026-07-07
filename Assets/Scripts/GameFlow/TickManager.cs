using System.Collections.Generic;
using UnityEngine;

// Production 틱 진행. 그리드에 배치된 기계 목록을 유지하고 페이즈별로 틱을 호출한다.
public class TickManager : MonoBehaviour
{
    public const int ProductionPhaseTicks = 3000;

    public static TickManager Instance { get; private set; }

    [SerializeField] private float ticksPerSecond = 10f;

    private readonly List<Machine> machinesOnGrid = new();
    private float tickInterval;
    private float tickAccumulator;
    private bool isRunning;
    private int productionTick;

    public IReadOnlyList<Machine> MachinesOnGrid => machinesOnGrid;
    public bool IsRunning => isRunning;
    public int ProductionTick => productionTick;
    public float TickInterval => tickInterval;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<TickManager>() != null)
        {
            return;
        }

        var tickObject = new GameObject("TickManager");
        tickObject.AddComponent<TickManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        tickInterval = 1f / ticksPerSecond;
    }

    private void Start()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnPhaseChanged -= HandlePhaseChanged;
            GameSessionState.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnEnable()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDisable()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        tickAccumulator += Time.deltaTime;
        while (tickAccumulator >= tickInterval)
        {
            tickAccumulator -= tickInterval;
            AdvanceTick();
        }
    }

    // 그리드에 기계가 배치될 때 목록에 등록한다.
    public void RegisterMachine(Machine machine)
    {
        if (machine == null || machinesOnGrid.Contains(machine))
        {
            return;
        }

        machinesOnGrid.Add(machine);
    }

    // 그리드에서 기계가 회수·제거될 때 목록에서 해제한다.
    public void UnregisterMachine(Machine machine)
    {
        if (machine == null)
        {
            return;
        }

        machinesOnGrid.Remove(machine);
    }

    public void StartTick()
    {
        isRunning = true;
        tickAccumulator = 0f;
        productionTick = 0;
    }

    public void StopTick()
    {
        isRunning = false;
        tickAccumulator = 0f;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Production:
                StartTick();
                break;
            case GamePhase.Settlement:
                StopTick();
                break;
        }
    }

    private void AdvanceTick()
    {
        TickCompleteProductionPhase();
        TickLogisticsPhase();
        TickStartProductionPhase();
        TickMiscPhase();
    }

    // 1. 생산 완료: 제조 시간이 끝나면 outputPort에 산출.
    private void TickCompleteProductionPhase()
    {
        for (int i = machinesOnGrid.Count - 1; i >= 0; i--)
        {
            Machine machine = machinesOnGrid[i];
            if (machine == null)
            {
                machinesOnGrid.RemoveAt(i);
                continue;
            }

            if (machine is IFactoryProduction production)
            {
                production.TickCompleteProduction();
            }
        }
    }

    // 2. 물류: 컨베이어 이동. 이동 방향 역순으로 처리해 이중 이동을 막는다.
    private void TickLogisticsPhase()
    {
        var belts = new List<ConveyerBelt>();
        for (int i = 0; i < machinesOnGrid.Count; i++)
        {
            if (machinesOnGrid[i] is ConveyerBelt belt)
            {
                belts.Add(belt);
            }
        }

        belts.Sort(CompareBeltsForProcessingOrder);
        for (int i = 0; i < belts.Count; i++)
        {
            belts[i].TickLogistics();
        }

        for (int i = 0; i < belts.Count; i++)
        {
            belts[i].SyncItemVisual();
        }
    }

    // 3. 생산 시작: inputPort 재료가 충족되면 새 배치 시작.
    private void TickStartProductionPhase()
    {
        for (int i = machinesOnGrid.Count - 1; i >= 0; i--)
        {
            Machine machine = machinesOnGrid[i];
            if (machine == null)
            {
                machinesOnGrid.RemoveAt(i);
                continue;
            }

            if (machine is IFactoryProduction production)
            {
                production.TickStartProduction();
            }
        }
    }

    // 4. 기타: 생산 단계 경과 틱 등.
    private void TickMiscPhase()
    {
        if (productionTick < ProductionPhaseTicks)
        {
            productionTick++;
        }
    }

    private static int CompareBeltsForProcessingOrder(ConveyerBelt a, ConveyerBelt b)
    {
        int scoreA = GetBeltFlowScore(a);
        int scoreB = GetBeltFlowScore(b);
        return scoreB.CompareTo(scoreA);
    }

    private static int GetBeltFlowScore(ConveyerBelt belt)
    {
        Vector2Int anchor = belt.GridAnchor;
        Vector2Int direction = belt.FlowDirection;
        return anchor.x * direction.x + anchor.y * direction.y;
    }
}
