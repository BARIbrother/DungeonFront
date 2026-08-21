using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기존 GameSessionState/PlayerInventory 파일을 수정하지 않고 첫 NewGame 데이터를 실제 인벤토리에 맞춘다.
/// 이미 기계가 있는 저장/테스트 상태는 덮어쓰지 않는다.
/// </summary>
public sealed class GameStartRuntimeBridge : MonoBehaviour
{
    private static GameStartRuntimeBridge instance;
    private GameSessionState session;

    private static readonly string[] StartingMachineIds =
    {
        "Miner_1", "Smelter_1", "HandmadeAssembler_1", "Warehouse_1"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            new GameObject("GameStartRuntimeBridge").AddComponent<GameStartRuntimeBridge>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => BindSession();

    private void Update() => BindSession();

    private void BindSession()
    {
        GameSessionState candidate = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();
        if (candidate == session) return;
        if (session != null) session.OnNewGame -= HandleNewGame;
        session = candidate;
        if (session != null) session.OnNewGame += HandleNewGame;
    }

    private void OnDestroy()
    {
        if (session != null) session.OnNewGame -= HandleNewGame;
    }

    private void HandleNewGame()
    {
        PlayerInventory inventory = PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>();
        if (inventory == null || inventory.Machines.Count > 0)
        {
            return;
        }

        MachineDatabase database = FindMachineDatabase();
        if (database == null)
        {
            Debug.LogWarning("[GameStart] MachineDatabase를 찾지 못해 시작 기계를 지급하지 못했습니다.");
            return;
        }

        var missing = new List<string>();
        foreach (string id in StartingMachineIds)
        {
            ItemDef_Machine definition = database.Get(id);
            if (definition == null) missing.Add(id);
            else inventory.AddMachine(definition);
        }

        if (missing.Count > 0)
        {
            Debug.LogWarning($"[GameStart] 시작 기계 정의 누락: {string.Join(", ", missing)}");
        }
    }

    private static MachineDatabase FindMachineDatabase()
    {
        MachineDatabase[] loaded = Resources.FindObjectsOfTypeAll<MachineDatabase>();
        return loaded.Length > 0 ? loaded[0] : null;
    }
}
