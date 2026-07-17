using System;
using System.Collections.Generic;
using UnityEngine;

// 생산 단계에서 발생하는 이벤트를 수집·발행한다. 관련 매니저 참조를 보유한다.
public class ProductionEventManager : MonoBehaviour
{
    public static ProductionEventManager Instance { get; private set; }

    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private RecipeManager recipeManager;
    [SerializeField] private ItemManager itemManager;

    public GridManager Grid => gridManager;
    public PlayerInventory PlayerInventory => playerInventory;
    public QuestManager Quest => questManager;
    public RecipeManager Recipes => recipeManager;
    public ItemManager Items => itemManager;
    public GameSessionState Session => GameSessionState.Instance;
    public TickManager Tick => TickManager.Instance;

    public bool IsProductionActive =>
        Session != null && Session.Phase == GamePhase.Production;

    public event Action OnProductionStarted;
    public event Action OnProductionEnded;
    public event Action<int> OnProductionTickAdvanced;
    public event Action<Machine> OnMachineProductionCompleted;
    public event Action<Machine> OnMachineBroken;
    public event Action<Machine> OnMachineRepaired;

    private const int BreakdownAfterTicks = 100;

    private bool isBreakdownPending;
    private Machine brokenMachine;
    private bool isSubscribedToSession;

    public Machine BrokenMachine => brokenMachine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<ProductionEventManager>() != null)
        {
            return;
        }

        var eventObject = new GameObject("ProductionEventManager");
        eventObject.AddComponent<ProductionEventManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ResolveReferences();
        SubscribeSessionEvents();
    }

    private void OnEnable()
    {
        SubscribeSessionEvents();
    }

    private void OnDisable()
    {
        UnsubscribeSessionEvents();
    }

    private void Update()
    {
        TrySubscribeSessionEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        ClearBreakdownState();
        UnsubscribeSessionEvents();
    }

    // 플레이어가 근접 상호작용으로 고장난 기계를 수리한다. 성공 시 true.
    public bool TryRepairMachine(Machine machine)
    {
        if (machine == null || !machine.IsBroken)
        {
            return false;
        }

        machine.SetBroken(false);
        if (brokenMachine == machine)
        {
            brokenMachine = null;
        }

        OnMachineRepaired?.Invoke(machine);
        Debug.Log($"[ProductionEventManager] 기계 수리 완료: {machine.name}");
        return true;
    }

    // 인스펙터 미할당 시 씬에서 매니저를 찾아 연결한다.
    private void ResolveReferences()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
        }

        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (questManager == null)
        {
            questManager = FindAnyObjectByType<QuestManager>();
        }

        if (recipeManager == null)
        {
            recipeManager = FindAnyObjectByType<RecipeManager>();
        }

        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }
    }

    private void SubscribeSessionEvents()
    {
        if (GameSessionState.Instance == null)
        {
            return;
        }

        if (isSubscribedToSession)
        {
            return;
        }

        GameSessionState.Instance.OnPhaseChanged += HandlePhaseChanged;
        isSubscribedToSession = true;
    }

    private void TrySubscribeSessionEvents()
    {
        if (!isSubscribedToSession)
        {
            SubscribeSessionEvents();
        }
    }

    private void UnsubscribeSessionEvents()
    {
        if (GameSessionState.Instance == null || !isSubscribedToSession)
        {
            return;
        }

        GameSessionState.Instance.OnPhaseChanged -= HandlePhaseChanged;
        isSubscribedToSession = false;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Production:
                OnProductionStarted?.Invoke();
                break;
            case GamePhase.Settlement:
                ClearBreakdownState();
                OnProductionEnded?.Invoke();
                break;
        }
    }

    private void ScheduleBreakdown()
    {
        ClearBreakdownState(keepBrokenMachine: false);
        isBreakdownPending = true;
        Debug.Log($"[ProductionEventManager] {BreakdownAfterTicks}틱 후 랜덤 기계 고장 예약");
    }

    private bool TryTriggerRandomBreakdown()
    {
        if (brokenMachine != null || Tick == null)
        {
            return brokenMachine != null;
        }

        IReadOnlyList<Machine> machines = Tick.MachinesOnGrid;
        if (machines.Count == 0)
        {
            return false;
        }

        int index = UnityEngine.Random.Range(0, machines.Count);
        Machine target = machines[index];
        if (target == null)
        {
            return false;
        }

        brokenMachine = target;
        brokenMachine.SetBroken(true);
        isBreakdownPending = false;
        OnMachineBroken?.Invoke(brokenMachine);
        Debug.Log($"[ProductionEventManager] 기계 고장: {brokenMachine.name} (틱 {Tick.ProductionTick})");
        return true;
    }

    private void ClearBreakdownState(bool keepBrokenMachine = false)
    {
        isBreakdownPending = false;

        if (!keepBrokenMachine && brokenMachine != null)
        {
            brokenMachine.SetBroken(false);
            brokenMachine = null;
        }
    }

    // TickManager가 생산 틱 세션을 시작할 때 호출한다.
    public void NotifyProductionSessionStarted()
    {
        ScheduleBreakdown();
    }

    // TickManager가 생산 틱을 진행할 때마다 호출한다.
    public void NotifyProductionTick(int tick)
    {
        if (Tick == null || !Tick.IsRunning)
        {
            return;
        }

        OnProductionTickAdvanced?.Invoke(tick);

        if (!isBreakdownPending || brokenMachine != null || tick < BreakdownAfterTicks)
        {
            return;
        }

        if (!TryTriggerRandomBreakdown())
        {
            Debug.LogWarning(
                $"[ProductionEventManager] {tick}틱 시점에 고장을 낼 기계가 없습니다. 그리드에 기계를 배치해 주세요.");
        }
    }

    // 기계 생산 완료 시 호출한다.
    public void NotifyMachineProductionCompleted(Machine machine)
    {
        if (!IsProductionActive || machine == null)
        {
            return;
        }

        OnMachineProductionCompleted?.Invoke(machine);
    }
}
