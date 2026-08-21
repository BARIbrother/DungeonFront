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

    private const int BreakdownsPerDay = 1;
    private static int MinTicksBeforeBreakdown => 3 * (int)TickManager.TicksPerSecond;
    public const string BreakdownUnlockStoryEventId = "001E00020";

    private static bool isBreakdownEnabled;
    private int breakdownQuotaDay = -1;
    private int breakdownsUsedThisDay;
    private int scheduledBreakdownAtTick = -1;
    private bool isBreakdownPending;
    private Machine brokenMachine;
    private bool isSubscribedToSession;

    public Machine BrokenMachine => brokenMachine;
    public static bool IsBreakdownEnabled => isBreakdownEnabled;

    // 001E00020 이브 대사("왜 저한테 설명하시는 겁니까?") 이후 고장을 허용한다.
    public static void EnableBreakdown()
    {
        if (isBreakdownEnabled)
        {
            return;
        }

        isBreakdownEnabled = true;
        Debug.Log("[ProductionEventManager] 기계 고장 기능 활성화");
        Instance?.TryScheduleBreakdownAfterGate();
    }

    public static void ResetBreakdownGate()
    {
        isBreakdownEnabled = false;
        Instance?.ResetDailyBreakdownQuota();
    }

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
        PlayCatalogSfx(audio => audio.Catalog.repair);
        AudioManager.Instance?.StopMachineBreaking();
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

    private void ResetDailyBreakdownQuota()
    {
        breakdownQuotaDay = -1;
        breakdownsUsedThisDay = 0;
        scheduledBreakdownAtTick = -1;
        isBreakdownPending = false;
    }

    private void EnsureDailyBreakdownQuota()
    {
        int day = Session != null ? Session.day : 1;
        if (breakdownQuotaDay == day)
        {
            return;
        }

        breakdownQuotaDay = day;
        breakdownsUsedThisDay = 0;
        scheduledBreakdownAtTick = -1;
        isBreakdownPending = false;
    }

    private void TryScheduleBreakdownIfNeeded(int currentTick)
    {
        if (IsBreakdownSuppressed())
        {
            return;
        }

        EnsureDailyBreakdownQuota();

        if (breakdownsUsedThisDay >= BreakdownsPerDay)
        {
            return;
        }

        if (isBreakdownPending || brokenMachine != null)
        {
            return;
        }

        ScheduleNextBreakdown(currentTick);
    }

    // currentTick 이후 ~ 생산 종료 틱 사이의 랜덤 시점에 고장을 예약한다.
    private void ScheduleNextBreakdown(int currentTick)
    {
        int totalTicks = TickManager.ProductionPhaseTicks;
        int earliestTick = currentTick + MinTicksBeforeBreakdown;
        if (earliestTick > totalTicks)
        {
            Debug.Log(
                $"[ProductionEventManager] Day {breakdownQuotaDay}: 남은 틱이 부족해 고장을 예약하지 않습니다. (현재 {currentTick}/{totalTicks})");
            return;
        }

        scheduledBreakdownAtTick = UnityEngine.Random.Range(earliestTick, totalTicks + 1);
        isBreakdownPending = true;
        Debug.Log(
            $"[ProductionEventManager] Day {breakdownQuotaDay}: {scheduledBreakdownAtTick}틱에 고장 예약 "
            + $"(현재 {currentTick}, 일일 {breakdownsUsedThisDay + 1}/{BreakdownsPerDay})");
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
        scheduledBreakdownAtTick = -1;
        breakdownsUsedThisDay++;
        OnMachineBroken?.Invoke(brokenMachine);
        PlayCatalogSfx(audio => audio.Catalog.machineBreak);
        AudioManager.Instance?.StartMachineBreaking();
        Debug.Log($"[ProductionEventManager] 기계 고장: {brokenMachine.name} (틱 {Tick.ProductionTick})");
        return true;
    }

    private void ClearBreakdownState(bool keepBrokenMachine = false)
    {
        isBreakdownPending = false;
        scheduledBreakdownAtTick = -1;

        if (!keepBrokenMachine && brokenMachine != null)
        {
            brokenMachine.SetBroken(false);
            brokenMachine = null;
        }

        if (!keepBrokenMachine)
        {
            AudioManager.Instance?.StopMachineBreaking();
        }
    }

    // 생산 종료가 결산 UI보다 먼저 일어날 때 호출한다.
    // 고장 예약, 고장 상태, 반복음을 한 번에 정리한다.
    public void EndProductionSession()
    {
        ClearBreakdownState();
    }

    private static void PlayCatalogSfx(Func<AudioManager, AudioCatalog.AudioEntry> selectClip)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null || audio.Catalog == null || selectClip == null)
        {
            return;
        }

        audio.PlaySfx(selectClip(audio));
    }

    // TickManager가 생산 틱 세션을 시작할 때 호출한다.
    public void NotifyProductionSessionStarted()
    {
        EnsureDailyBreakdownQuota();

        if (IsBreakdownSuppressed())
        {
            scheduledBreakdownAtTick = -1;
            isBreakdownPending = false;
            return;
        }

        TryScheduleBreakdownIfNeeded(0);
    }

    // TickManager가 생산 틱을 진행할 때마다 호출한다.
    public void NotifyProductionTick(int tick)
    {
        if (!IsProductionActive || Session == null || Session.IsEndingProduction
            || Tick == null || !Tick.IsRunning)
        {
            return;
        }

        OnProductionTickAdvanced?.Invoke(tick);

        if (IsBreakdownSuppressed())
        {
            return;
        }

        if (!isBreakdownPending || brokenMachine != null || tick < scheduledBreakdownAtTick)
        {
            return;
        }

        if (!TryTriggerRandomBreakdown())
        {
            Debug.LogWarning(
                $"[ProductionEventManager] {tick}틱 시점에 고장을 낼 기계가 없습니다. 그리드에 기계를 배치해 주세요.");
            isBreakdownPending = false;
            scheduledBreakdownAtTick = -1;
            return;
        }

        if (breakdownsUsedThisDay < BreakdownsPerDay)
        {
            TryScheduleBreakdownIfNeeded(tick);
        }
    }

    private void TryScheduleBreakdownAfterGate()
    {
        if (!IsProductionActive || Tick == null || !Tick.IsRunning)
        {
            return;
        }

        TryScheduleBreakdownIfNeeded(Tick.ProductionTick);
    }

    // 1일차는 001E00020 이브 대사 전까지 고장을 막는다. 2일차 이후는 항상 허용.
    private bool IsBreakdownSuppressed()
    {
        GameSessionState session = GameSessionState.Instance;
        if (session != null && session.day > 1)
        {
            return false;
        }

        return !isBreakdownEnabled;
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
