using System.Collections.Generic;
using UnityEngine;

// Production 틱 진행. 그리드에 배치된 기계 목록을 유지하고 틱마다 Machine.Tick()을 호출한다.
public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    [SerializeField] private float ticksPerSecond = 10f;

    private readonly List<Machine> machinesOnGrid = new();
    private float tickInterval;
    private float tickAccumulator;
    private bool isRunning;

    public IReadOnlyList<Machine> MachinesOnGrid => machinesOnGrid;
    public bool IsRunning => isRunning;

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
    }

    public void StopTick()
    {
        isRunning = false;
        tickAccumulator = 0f;
    }

    private void AdvanceTick()
    {
        for (int i = machinesOnGrid.Count - 1; i >= 0; i--)
        {
            Machine machine = machinesOnGrid[i];
            if (machine == null)
            {
                machinesOnGrid.RemoveAt(i);
                continue;
            }

            machine.Tick();
        }
    }
}
