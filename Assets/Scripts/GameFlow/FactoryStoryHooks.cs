using System.Collections.Generic;
using UnityEngine;

// Factory·세션 흐름에서 StoryEventBus로 스토리 이벤트 id를 발행한다.
// Dev1은 OnStoryEvent만 구독한다 — 이 클래스가 Lead측 Raise의 단일 진입점이다.
//
// 기획 eventId 매핑 (Docs/04-story.md):
// | eventId    | 일차 | 페이즈      | 트리거                         | 구현 |
// |------------|------|-------------|-------------------------------|------|
// | 001E00001  | 1    | Prepare     | 1일차 Prepare 첫 진입         | ✓    |
// | 001E00002  | 1    | Prepare     | 001E00001 대화 종료 직후      | ✓ NotifyDialogueClosed |
// | 001E00003  | 1    | Prepare     | 튜토 단계 (미정)              | ✗ Dev1 튜토 패널 담당 |
// | 001E00004  | 1    | Production  | 생산 시작 후                  | ✓    |
// | 001E00005  | 1    | Settlement  | 결산 진입                     | ✓    |
// | 001E00006  | 3    | Prepare     | 3일차 Prepare 진입 (레이)     | ✓    |
//
// Bus 내부 이벤트 (페이로드는 문자열에 포함):
//   OnPrepareEntered:{day}
//   OnProductionStarted
//   OnProductionEnded
//   OnMachinePlaced:{machineTypeId}:{x},{y}
public class FactoryStoryHooks : MonoBehaviour
{
    private static FactoryStoryHooks instance;

    private PlacementController placementController;
    private bool sessionBound;
    private readonly HashSet<string> firedStoryIds = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<FactoryStoryHooks>() != null)
        {
            return;
        }

        var systemObject = new GameObject("FactoryStoryHooks");
        systemObject.AddComponent<FactoryStoryHooks>();
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

    private void OnEnable()
    {
        TryBindSession();
        TryBindPlacement();
    }

    private void Start()
    {
        TryBindSession();
        TryBindPlacement();

        // NewGame 없이 Prepare로 시작된 경우 Bus·오프닝을 보완한다.
        if (GameSessionState.Instance != null
            && GameSessionState.Instance.day == 1
            && GameSessionState.Instance.Phase == GamePhase.Prepare
            && !firedStoryIds.Contains("001E00001"))
        {
            StoryEventBus.Raise("OnPrepareEntered:1");
            TryRaiseDay1Opening();
        }
    }

    private void Update()
    {
        // 씬 부트스트랩 순서가 달라질 수 있어 미연결 참조를 재시도한다.
        if (!sessionBound)
        {
            TryBindSession();
        }

        if (placementController == null)
        {
            TryBindPlacement();
        }
    }

    private void OnDisable()
    {
        UnbindSession();
        UnbindPlacement();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Dev1 대화 UI가 닫힐 때 호출한다. 연쇄 이벤트(001E00001 → 001E00002)를 Lead가 발행한다.
    public static void NotifyDialogueClosed(string eventId)
    {
        if (instance == null || string.IsNullOrEmpty(eventId))
        {
            return;
        }

        instance.HandleDialogueClosed(eventId);
    }

    private void TryBindSession()
    {
        if (GameSessionState.Instance == null)
        {
            return;
        }

        GameSessionState.Instance.OnPhaseChanged -= HandlePhaseChanged;
        GameSessionState.Instance.OnPhaseChanged += HandlePhaseChanged;
        GameSessionState.Instance.OnNewGame -= HandleNewGame;
        GameSessionState.Instance.OnNewGame += HandleNewGame;
        sessionBound = true;
    }

    private void UnbindSession()
    {
        sessionBound = false;

        if (GameSessionState.Instance == null)
        {
            return;
        }

        GameSessionState.Instance.OnPhaseChanged -= HandlePhaseChanged;
        GameSessionState.Instance.OnNewGame -= HandleNewGame;
    }

    private void TryBindPlacement()
    {
        if (placementController == null)
        {
            placementController = FindAnyObjectByType<PlacementController>();
        }

        if (placementController == null)
        {
            return;
        }

        placementController.OnMachinePlaced -= HandleMachinePlaced;
        placementController.OnMachinePlaced += HandleMachinePlaced;
    }

    private void UnbindPlacement()
    {
        if (placementController == null)
        {
            return;
        }

        placementController.OnMachinePlaced -= HandleMachinePlaced;
    }

    private void HandleNewGame()
    {
        firedStoryIds.Clear();
        // NewGame은 SetPhase를 거치지 않으므로 Prepare 진입 Bus 이벤트를 직접 발행한다.
        StoryEventBus.Raise("OnPrepareEntered:1");
        TryRaiseDay1Opening();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (GameSessionState.Instance == null)
        {
            return;
        }

        int day = GameSessionState.Instance.day;

        switch (phase)
        {
            case GamePhase.Prepare:
                StoryEventBus.Raise($"OnPrepareEntered:{day}");
                TryRaiseDay1Opening();
                TryRaiseOnce("001E00006", day == 3);
                break;

            case GamePhase.Production:
                StoryEventBus.Raise("OnProductionStarted");
                TryRaiseOnce("001E00004", day == 1);
                break;

            case GamePhase.Settlement:
                StoryEventBus.Raise("OnProductionEnded");
                TryRaiseOnce("001E00005", day == 1);
                break;
        }
    }

    private void HandleMachinePlaced(string machineTypeId, Vector2Int grid)
    {
        StoryEventBus.Raise($"OnMachinePlaced:{machineTypeId}:{grid.x},{grid.y}");
    }

    private void HandleDialogueClosed(string eventId)
    {
        // 001E00001 종료 → 이브 첫 의뢰 안내
        if (eventId == "001E00001")
        {
            TryRaiseOnce("001E00002", true);
        }
    }

    private void TryRaiseDay1Opening()
    {
        if (GameSessionState.Instance == null)
        {
            return;
        }

        if (GameSessionState.Instance.day != 1
            || GameSessionState.Instance.Phase != GamePhase.Prepare)
        {
            return;
        }

        TryRaiseOnce("001E00001", true);
    }

    private void TryRaiseOnce(string eventId, bool condition)
    {
        if (!condition || string.IsNullOrEmpty(eventId))
        {
            return;
        }

        if (!firedStoryIds.Add(eventId))
        {
            return;
        }

        StoryEventBus.Raise(eventId);
        Debug.Log($"[FactoryStoryHooks] Raise {eventId}");
    }
}
