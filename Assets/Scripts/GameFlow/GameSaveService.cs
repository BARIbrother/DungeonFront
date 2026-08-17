using System;
using System.IO;
using UnityEngine;

[Serializable]
public class DungeonFrontSaveData
{
    public int version = 1;
    public int day;
    public GamePhase phase;
    public int gold;
    public int reputation;
    public ItemStackSave[] itemStacks;
    public AcceptedQuestSave[] acceptedQuests;
    public string[] completedQuestIds;
}

/// <summary>
/// persistentDataPath 아래 slot_{n}.json에 세션·재고·수락 의뢰·진행 의뢰를 저장한다.
/// </summary>
public sealed class GameSaveService : MonoBehaviour
{
    private static GameSaveService instance;
    private static bool isRestoring;
    private GameSessionState boundSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            new GameObject("GameSaveService").AddComponent<GameSaveService>();
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

    private void Update()
    {
        GameSessionState session = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();
        if (session == boundSession)
        {
            return;
        }
        if (boundSession != null)
        {
            boundSession.OnPhaseChanged -= HandlePhaseChanged;
        }
        boundSession = session;
        if (boundSession != null)
        {
            boundSession.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void OnDestroy()
    {
        if (boundSession != null)
        {
            boundSession.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(GamePhase _)
    {
        // Load 중 RestoreSession이 발생시키는 phase 이벤트로 빈 퀘스트 상태가 덮어써지는 것을 막는다.
        if (!isRestoring)
        {
            Save(0);
        }
    }

    public static bool HasSave(int slot = 0) => File.Exists(GetPath(slot));

    public static void Save(int slot = 0)
    {
        GameSessionState session = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();
        if (session == null)
        {
            Debug.LogWarning("[Save] GameSessionState를 찾지 못해 저장하지 않았습니다.");
            return;
        }

        PlayerInventory inventory = PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>();
        QuestSaveProvider questSave = FindAnyObjectByType<QuestSaveProvider>();
        QuestProgressionService progression = QuestProgressionService.Instance ?? FindAnyObjectByType<QuestProgressionService>();
        DungeonFrontSaveData data = new()
        {
            day = session.day,
            phase = session.Phase,
            gold = session.gold,
            reputation = session.reputation,
            itemStacks = inventory != null ? inventory.ExportItemStacks().ToArray() : Array.Empty<ItemStackSave>(),
            acceptedQuests = questSave != null ? questSave.Export() : Array.Empty<AcceptedQuestSave>(),
            completedQuestIds = progression != null ? new System.Collections.Generic.List<string>(progression.CompletedQuestIds).ToArray() : Array.Empty<string>(),
        };

        string path = GetPath(slot);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"[Save] 슬롯 {slot} 저장 완료: {path}");
    }

    public static bool Load(int slot = 0)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Save] 슬롯 {slot} 저장 파일이 없습니다.");
            return false;
        }

        DungeonFrontSaveData data;
        try
        {
            data = JsonUtility.FromJson<DungeonFrontSaveData>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] 저장 파일을 읽지 못했습니다: {exception.Message}");
            return false;
        }

        if (data == null)
        {
            return false;
        }

        GameSessionState session = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();
        if (session == null)
        {
            Debug.LogWarning("[Save] GameSessionState를 찾지 못해 불러오지 않았습니다.");
            return false;
        }

        isRestoring = true;
        try
        {
            session.RestoreSession(data.day, data.phase, data.gold, data.reputation);
            (PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>())?.RestoreItemStacks(data.itemStacks);
            FindAnyObjectByType<QuestSaveProvider>()?.Import(data.acceptedQuests);
            (QuestProgressionService.Instance ?? FindAnyObjectByType<QuestProgressionService>())?.Restore(data.completedQuestIds);
        }
        finally
        {
            isRestoring = false;
        }

        // 복원이 완전히 끝난 상태를 다시 저장한다.
        Save(slot);
        Debug.Log($"[Save] 슬롯 {slot} 불러오기 완료: {path}");
        return true;
    }

    private static string GetPath(int slot) => Path.Combine(Application.persistentDataPath, $"slot_{Mathf.Max(0, slot)}.json");
}
