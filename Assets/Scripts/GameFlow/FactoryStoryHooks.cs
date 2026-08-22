using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Factory·세션 흐름에서 StoryEventBus로 스토리 이벤트 id를 발행한다.
// Dev1은 OnStoryEvent만 구독한다 — 이 클래스가 Lead측 Raise의 단일 진입점이다.
//
// 기획 eventId 매핑 (Docs/04-story.md):
// | eventId    | 일차 | 페이즈      | 트리거                                                          | 구현 |
// |------------|------|-------------|-------------------------------------------------------------------------|------|
// | 001E00001  | 1    | Prepare     | 1일차 Prepare 첫 진입 — 이브 오프닝                                     | ✓    |
// | 001E00002  | 1    | Prepare     | 001E00001 대화 종료 직후 — 이브 첫 의뢰 안내                            | ✓ NotifyDialogueClosed |
// | 001E00008  | 1    | Prepare     | 001E00002 종료 직후 — 일차 진행 안내 독백                               | ✓ |
// | 001E00003  | 1    | Prepare     | 001E00008 종료 직후 — 조작키 독백. [게이트] 이동 입력 감지 + 5초 대기 후 진행 | ✓ |
// | 001E00007  | 1    | Prepare     | 001E00003 게이트 통과 직후 — 기계배치 독백. [게이트] 채굴기 배치 감지 후 진행 | ✓ |
// | 001E00013  | 1    | Prepare     | 001E00007 게이트 통과 직후 — 배치창 닫기 독백. [게이트] 배치 모드(B) 닫힘 감지 후 진행 | ✓ |
// | 001E00009  | 1    | Prepare     | 001E00013 게이트 통과 직후 — 퀘스트 버튼 안내 독백. [게이트] 퀘스트창 열림 감지 후 진행 | ✓ |
// | 001E00010  | 1    | Prepare     | 001E00009 게이트 통과 직후 — 퀘스트 수락 안내 독백. [게이트] 필수 퀘스트 수락 감지 후 진행 | ✓ |
// | 001E00014  | 1    | Prepare     | 001E00010 게이트 통과 직후 — 퀘스트 보상/페널티 안내 독백               | ✓ |
// | 001E00012  | 1    | Prepare     | 001E00014 종료 직후 — 퀘스트창 닫기 안내 독백. [게이트] 퀘스트창 닫힘 감지 후 진행 | ✓ |
// | 001E00022  | 1    | Prepare     | 001E00012 게이트 통과 직후 — 레시피북 열기 안내 독백. [게이트] 레시피북(K) 열림 감지 후 진행 | ✓ |
// | 001E00023  | 1    | Prepare     | 001E00022 게이트 통과 직후 — 레시피북 닫기 안내 독백. [게이트] 레시피북(K) 닫힘 감지 후 진행 | ✓ |
// | 001E00015  | 1    | Prepare     | 001E00023 게이트 통과 직후 — 테크트리 열기 안내 독백. [게이트] 테크트리창 열림 감지 후 진행 | ✓ |
// | 001E00011  | 1    | Prepare     | 001E00015 게이트 통과 직후 — 테크트리 설명 독백                         | ✓ |
// | 001E00016  | 1    | Prepare     | 001E00011 종료 직후 — 테크트리 닫기 안내 독백. [게이트] 테크트리창 닫힘 감지 후 진행 | ✓ |
// | 001E00017  | 1    | Prepare     | 001E00016 게이트 통과 직후 — 생산 단계로 넘어가자는 독백. [게이트] 생산 단계 진입 감지 후 진행 | ✓ |
// | 001E00004  | 1    | Production  | 생산 시작 후 — 채굴기 산출물 회수 안내. [게이트] 인벤토리 iron_ore 증가 감지 후 진행       | ✓ |
// | 001E00018  | 1    | Production  | 001E00004 게이트 통과 직후 — 용광로 배치 안내. [게이트] 용광로(Smelter) 배치 감지 후 진행    | ✓ |
// | 001E00021  | 1    | Production  | 001E00018 게이트 통과 직후 — 배치창 닫기 안내. [게이트] 배치 모드(B) 닫힘 감지 후 진행       | ✓ |
// | 001E00019  | 1    | Production  | 001E00021 게이트 통과 직후 — 제련·회수 안내. [게이트] 인벤토리 iron_bar 증가 감지 후 진행    | ✓ |
// | 001E00020  | 1    | Production  | 001E00019 게이트 통과 직후 — 마무리 독백. 대화가 닫히면 TutorialPanelUI 시작              | ✓ |
// | 001E00024  | 1    | Production  | 001E00020 종료 직후, [게이트] 고장난 기계 발생 감지 통과 후 — 기계 고장 안내 독백. [게이트] 수리(스페이스) 완료 감지 후 진행 | ✓ |
// | 001E00025  | 1    | Production  | 001E00024 게이트 통과 직후 — 수리 완료 마무리 독백 (게이트 없음)          | ✓ |
// | 001E00005  | 1    | Settlement  | 결산 진입 — 첫 납품 반응 독백                                            | ✓    |
// | 001E00006  | 3    | Prepare     | 3일차 Prepare 진입 (레이)                                               | ✓    |
//
// [게이트]로 표시된 전환은 대화가 닫혀도 바로 다음 이벤트를 Raise하지 않는다.
// 플레이어가 실제로 해당 조작(이동·배치·퀘스트창·테크트리창·생산시작 조작)을 해낼 때까지 대기했다가 넘어간다.
// (TutorialGate, BeginGate/CompleteGate, Update()의 게이트 폴링 참고)
//
// Bus 내부 이벤트 (페이로드는 문자열에 포함):
//   OnPrepareEntered:{day}
//   OnProductionStarted
//   OnProductionEnded
//   OnMachinePlaced:{machineTypeId}:{x},{y}
public class FactoryStoryHooks : MonoBehaviour
{
    // 대화 종료 후 "플레이어가 실제로 해내야 하는" 확인 단계. None이면 대기 없음.
    private enum TutorialGate
    {
        None,
        WaitMovementInput,          // 001E00003 종료 후: 이동키 입력 대기
        WaitMinerPlacement,         // 001E00007 종료 후: 채굴기 배치 대기
        WaitPlacementModeClose,     // 001E00013 종료 후: 배치 모드(B) 닫힘 대기
        WaitQuestWindowOpen,        // 001E00009 종료 후: 퀘스트창 열기 대기
        WaitMandatoryQuestAccept,   // 001E00010 종료 후: 필수 퀘스트 수락 대기
        WaitQuestWindowClose,       // 001E00012 종료 후: 퀘스트창 닫기 대기
        WaitRecipeBookOpen,         // 001E00022 종료 후: 레시피북(K) 열기 대기
        WaitRecipeBookClose,        // 001E00023 종료 후: 레시피북(K) 닫기 대기
        WaitTechTreeOpen,           // 001E00015 종료 후: 테크트리창 열기 대기
        WaitTechTreeClose,          // 001E00016 종료 후: 테크트리창 닫기 대기
        WaitProductionStarted,      // 001E00017 종료 후: 생산 단계 진입 대기
        WaitOreCollected,           // 001E00004 종료 후: 인벤토리 iron_ore 증가 대기
        WaitSmelterPlacement,       // 001E00018 종료 후: 용광로(Smelter) 배치 대기
        WaitIronIngotCollected,     // 001E00019 종료 후: 인벤토리 iron_bar 증가 대기
        WaitMachineBroken,          // 001E00020 종료 후: 기계 고장 발생 대기 (이미 고장 상태면 즉시 통과)
        WaitMachineRepaired,        // 001E00024 종료 후: 기계 수리(스페이스) 완료 대기
    }

    private static FactoryStoryHooks instance;

    private PlacementController placementController;
    private QuestManager questManager;
    private bool sessionBound;
    private readonly HashSet<string> firedStoryIds = new HashSet<string>();

    // 이동키를 처음 누른 뒤 이만큼(초) 더 조작해 볼 시간을 주고 나서 다음 대사로 넘어간다.
    private const float MovementConfirmDelaySeconds = 5f;

    private TutorialGate activeGate = TutorialGate.None;
    private string gateNextEventId;
    private bool movementInputDetected;
    private float movementInputDetectedAtUnscaledTime;
    private int oreCountAtGateStart;
    private int ingotCountAtGateStart;

    // WaitProductionStarted 게이트가 끝나면 바로 패널을 띄우지 않고, 001E00004(생산 시작 안내) 대화가
    // 화면에 뜬 뒤 플레이어가 그걸 닫을 때까지 기다린다. 그래야 대화창과 튜토리얼 패널이 겹치지 않는다.
    private bool tutorialPanelPendingAfter004;

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
        TryBindQuestManager();
    }

    private void Start()
    {
        TryBindSession();
        TryBindPlacement();
        TryBindQuestManager();

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
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f8Key.wasPressedThisFrame)
        {
            TutorialActionLock.ReleaseAll();
        }

        // 씬 부트스트랩 순서가 달라질 수 있어 미연결 참조를 재시도한다.
        if (!sessionBound)
        {
            TryBindSession();
        }

        if (placementController == null)
        {
            TryBindPlacement();
        }

        if (questManager == null)
        {
            TryBindQuestManager();
        }

        PollActiveGate();
    }

    private void OnDisable()
    {
        UnbindSession();
        UnbindPlacement();
        UnbindQuestManager();
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

    private void TryBindQuestManager()
    {
        if (questManager == null)
        {
            questManager = QuestManager.Instance != null
                ? QuestManager.Instance
                : FindAnyObjectByType<QuestManager>();
        }

        if (questManager == null)
        {
            return;
        }

        questManager.OnQuestAccepted -= HandleQuestAccepted;
        questManager.OnQuestAccepted += HandleQuestAccepted;
    }

    private void UnbindQuestManager()
    {
        if (questManager == null)
        {
            return;
        }

        questManager.OnQuestAccepted -= HandleQuestAccepted;
    }

    private void HandleNewGame()
    {
        ResetFiredStoryIds();
        // NewGame은 SetPhase를 거치지 않으므로 Prepare 진입 Bus 이벤트를 직접 발행한다.
        StoryEventBus.Raise("OnPrepareEntered:1");
        TryRaiseDay1Opening();
    }

    public void ResetFiredStoryIds()
    {
        firedStoryIds.Clear();
        activeGate = TutorialGate.None;
        gateNextEventId = null;
        movementInputDetected = false;
        tutorialPanelPendingAfter004 = false;
        oreCountAtGateStart = 0;
        ingotCountAtGateStart = 0;
        ProductionEventManager.ResetBreakdownGate();
        TutorialActionLock.Reset();
        if (GameSessionState.Instance != null && GameSessionState.Instance.day == 1)
        {
            TutorialActionLock.SetTutorialActive(true);
        }
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
                if (day != 1)
                {
                    TutorialActionLock.SetTutorialActive(false);
                }

                StoryEventBus.Raise($"OnPrepareEntered:{day}");
                TryRaiseDay1Opening();
                TryRaiseOnce("001E00006", day == 3);
                break;

            case GamePhase.Production:
                StoryEventBus.Raise("OnProductionStarted");
                TryRaiseOnce("001E00004", day == 1);

                // 001E00017(생산 단계로 넘어가자는 안내) 게이트가 대기 중이었다면 여기서 통과시킨다.
                // 튜토리얼 패널은 방금 Raise한 001E00004 대화가 닫힌 뒤에 띄운다(HandleDialogueClosed 참고).
                if (activeGate == TutorialGate.WaitProductionStarted)
                {
                    activeGate = TutorialGate.None;
                    gateNextEventId = null;
                    TutorialActionLock.SetGate(TutorialActionLock.Gate.None);
                    tutorialPanelPendingAfter004 = true;
                }

                break;

            case GamePhase.Settlement:
                TutorialActionLock.SetTutorialActive(false);
                StoryEventBus.Raise("OnProductionEnded");

                // 기계 고장·수리 튜토리얼이 아직 진행 중이었다면(3분 내내 못 고쳤거나 등) 결산 대사와
                // 겹치지 않도록 조용히 정리한다. ProductionEventManager도 결산 진입 시 고장 상태를 풀어준다.
                if (activeGate == TutorialGate.WaitMachineBroken || activeGate == TutorialGate.WaitMachineRepaired)
                {
                    activeGate = TutorialGate.None;
                    gateNextEventId = null;
                    TutorialActionLock.SetGate(TutorialActionLock.Gate.None);
                }

                TryRaiseOnce("001E00005", day == 1);
                break;
        }
    }

    private void HandleMachinePlaced(string machineTypeId, Vector2Int grid)
    {
        StoryEventBus.Raise($"OnMachinePlaced:{machineTypeId}:{grid.x},{grid.y}");

        if (activeGate == TutorialGate.WaitMinerPlacement && IsMinerDefinition(machineTypeId))
        {
            CompleteGate(TutorialGate.WaitMinerPlacement);
        }

        if (activeGate == TutorialGate.WaitSmelterPlacement && IsSmelterDefinition(machineTypeId))
        {
            CompleteGate(TutorialGate.WaitSmelterPlacement);
        }
    }

    // 채굴기는 티어별로 "Miner_1", "Miner_2" ... 형태의 machineDefId를 쓴다 (MachineDatabase 참고).
    private static bool IsMinerDefinition(string machineTypeId)
    {
        return !string.IsNullOrEmpty(machineTypeId)
            && machineTypeId.StartsWith("Miner", StringComparison.Ordinal);
    }

    // 용광로는 티어별로 "Smelter_1", "Smelter_2" ... 형태의 machineDefId를 쓴다 (MachineDatabase 참고).
    private static bool IsSmelterDefinition(string machineTypeId)
    {
        return !string.IsNullOrEmpty(machineTypeId)
            && machineTypeId.StartsWith("Smelter", StringComparison.Ordinal);
    }

    // 인벤토리 보유 수량 조회 헬퍼. PlayerInventory가 아직 없으면 0.
    private static int GetPlayerItemCount(string itemId)
    {
        return PlayerInventory.Instance != null ? PlayerInventory.Instance.GetCount(itemId) : 0;
    }

    private void HandleQuestAccepted(Quest quest)
    {
        if (activeGate != TutorialGate.WaitMandatoryQuestAccept)
        {
            return;
        }

        bool isMandatory = QuestRuntimeRegistry.Get(quest)?.isMandatory ?? false;
        if (isMandatory)
        {
            CompleteGate(TutorialGate.WaitMandatoryQuestAccept);
        }
    }

    private void HandleDialogueClosed(string eventId)
    {
        switch (eventId)
        {
            case "001E00001":
                TryRaiseOnce("001E00002", true);
                break;
            case "001E00002":
                TryRaiseOnce("001E00008", true);
                break;
            case "001E00008":
                TryRaiseOnce("001E00003", true);
                break;
            case "001E00003":
                // 조작키 안내 종료 — 플레이어가 실제로 움직여 볼 때까지 대기한다.
                BeginGate(TutorialGate.WaitMovementInput, "001E00007");
                break;
            case "001E00007":
                // 기계배치 안내 종료 — 채굴기를 배치할 때까지 대기한다.
                BeginGate(TutorialGate.WaitMinerPlacement, "001E00013");
                break;
            case "001E00013":
                // 배치창 닫기 안내 종료 — 배치 모드(B)를 닫을 때까지 대기한다.
                BeginGate(TutorialGate.WaitPlacementModeClose, "001E00009");
                break;
            case "001E00009":
                // 퀘스트 버튼 안내 종료 — 퀘스트창을 열 때까지 대기한다.
                BeginGate(TutorialGate.WaitQuestWindowOpen, "001E00010");
                break;
            case "001E00010":
                // 퀘스트 수락 안내 종료 — 필수 퀘스트를 수락할 때까지 대기한다.
                BeginGate(TutorialGate.WaitMandatoryQuestAccept, "001E00014");
                break;
            case "001E00014":
                TryRaiseOnce("001E00012", true);
                break;
            case "001E00012":
                // 퀘스트창 닫기 안내 종료 — 퀘스트창을 닫을 때까지 대기한다.
                BeginGate(TutorialGate.WaitQuestWindowClose, "001E00022");
                break;
            case "001E00022":
                // 레시피북 열기 안내 종료 — 레시피북(K)을 열 때까지 대기한다.
                BeginGate(TutorialGate.WaitRecipeBookOpen, "001E00023");
                break;
            case "001E00023":
                // 레시피북 닫기 안내 종료 — 레시피북(K)을 닫을 때까지 대기한다.
                BeginGate(TutorialGate.WaitRecipeBookClose, "001E00015");
                break;
            case "001E00015":
                // 테크트리 열기 안내 종료 — 테크트리창을 열 때까지 대기한다.
                BeginGate(TutorialGate.WaitTechTreeOpen, "001E00011");
                break;
            case "001E00011":
                TryRaiseOnce("001E00016", true);
                break;
            case "001E00016":
                // 테크트리 닫기 안내 종료 — 테크트리창을 닫을 때까지 대기한다.
                BeginGate(TutorialGate.WaitTechTreeClose, "001E00017");
                break;
            case "001E00017":
                // 생산 단계로 넘어가자는 안내 종료 — 실제로 생산 단계가 시작될 때까지 대기한다.
                // (다음 dialogue id는 없음 — HandlePhaseChanged가 게이트를 직접 통과시킨다)
                BeginGate(TutorialGate.WaitProductionStarted, null);
                break;
            case "001E00004":
                // 채굴기 산출물 회수 안내 종료 — 인벤토리에 iron_ore가 늘어날 때까지 대기한다.
                BeginGate(TutorialGate.WaitOreCollected, "001E00018");
                break;
            case "001E00018":
                // 용광로 배치 안내 종료 — 용광로(Smelter)를 배치할 때까지 대기한다.
                BeginGate(TutorialGate.WaitSmelterPlacement, "001E00021");
                break;
            case "001E00021":
                // 배치창 닫기 안내 종료 — 배치 모드(B)를 닫아야 방금 놓은 용광로를 클릭해 상호작용할 수 있다.
                BeginGate(TutorialGate.WaitPlacementModeClose, "001E00019");
                break;
            case "001E00019":
                // 제련·회수 안내 종료 — 인벤토리에 iron_bar가 늘어날 때까지 대기한다.
                BeginGate(TutorialGate.WaitIronIngotCollected, "001E00020");
                break;
            case "001E00020":
                // 생산 튜토 마무리 대화가 닫혔고, 그 직전 001E00017 게이트가 통과된 상태였다면 튜토리얼 패널을 띄운다.
                if (tutorialPanelPendingAfter004)
                {
                    tutorialPanelPendingAfter004 = false;
                    ShowTutorialPanel();
                }

                // 이어서 기계 고장·수리 튜토리얼 — 이미 고장난 기계가 있으면 바로, 없으면 고장날 때까지 대기한다.
                BeginGate(TutorialGate.WaitMachineBroken, "001E00024");
                break;
            case "001E00024":
                // 기계 고장 안내 종료 — 스페이스바로 실제 수리를 마칠 때까지 대기한다.
                BeginGate(TutorialGate.WaitMachineRepaired, "001E00025");
                break;
            case "001E00025":
                TutorialActionLock.SetTutorialActive(false);
                break;
        }
    }

    private static void ShowTutorialPanel()
    {
        TutorialPanelUI panel = FindAnyObjectByType<TutorialPanelUI>();
        panel?.Show();
    }

    // 게이트를 시작한다. 조건이 충족되면(PollActiveGate·이벤트 콜백) nextEventId를 Raise한다.
    private void BeginGate(TutorialGate gate, string nextEventId)
    {
        activeGate = gate;
        gateNextEventId = nextEventId;
        movementInputDetected = false;
        TutorialActionLock.SetGate(ToLockGate(gate));

        // 게이트 시작 시점의 보유량을 기준선으로 잡아, 이미 갖고 있던 수량과 무관하게 "이번에 늘었는지"만 본다.
        if (gate == TutorialGate.WaitOreCollected)
        {
            oreCountAtGateStart = GetPlayerItemCount("iron_ore");
        }
        else if (gate == TutorialGate.WaitIronIngotCollected)
        {
            ingotCountAtGateStart = GetPlayerItemCount("iron_bar");
        }
    }

    // 진행 중인 게이트가 gate와 일치하면 종료하고 대기 중이던 다음 이벤트를 Raise한다.
    private void CompleteGate(TutorialGate gate)
    {
        if (activeGate != gate)
        {
            return;
        }

        string nextEventId = gateNextEventId;
        activeGate = TutorialGate.None;
        gateNextEventId = null;
        TutorialActionLock.SetGate(TutorialActionLock.Gate.None);
        TryRaiseOnce(nextEventId, true);
    }

    private static TutorialActionLock.Gate ToLockGate(TutorialGate gate)
    {
        return gate switch
        {
            TutorialGate.WaitMovementInput => TutorialActionLock.Gate.WaitMovementInput,
            TutorialGate.WaitMinerPlacement => TutorialActionLock.Gate.WaitMinerPlacement,
            TutorialGate.WaitPlacementModeClose => TutorialActionLock.Gate.WaitPlacementModeClose,
            TutorialGate.WaitQuestWindowOpen => TutorialActionLock.Gate.WaitQuestWindowOpen,
            TutorialGate.WaitMandatoryQuestAccept => TutorialActionLock.Gate.WaitMandatoryQuestAccept,
            TutorialGate.WaitQuestWindowClose => TutorialActionLock.Gate.WaitQuestWindowClose,
            TutorialGate.WaitRecipeBookOpen => TutorialActionLock.Gate.WaitRecipeBookOpen,
            TutorialGate.WaitRecipeBookClose => TutorialActionLock.Gate.WaitRecipeBookClose,
            TutorialGate.WaitTechTreeOpen => TutorialActionLock.Gate.WaitTechTreeOpen,
            TutorialGate.WaitTechTreeClose => TutorialActionLock.Gate.WaitTechTreeClose,
            TutorialGate.WaitProductionStarted => TutorialActionLock.Gate.WaitProductionStarted,
            TutorialGate.WaitOreCollected => TutorialActionLock.Gate.WaitOreCollected,
            TutorialGate.WaitSmelterPlacement => TutorialActionLock.Gate.WaitSmelterPlacement,
            TutorialGate.WaitIronIngotCollected => TutorialActionLock.Gate.WaitIronIngotCollected,
            TutorialGate.WaitMachineBroken => TutorialActionLock.Gate.WaitMachineBroken,
            TutorialGate.WaitMachineRepaired => TutorialActionLock.Gate.WaitMachineRepaired,
            _ => TutorialActionLock.Gate.None,
        };
    }

    // 매 프레임 폴링이 필요한 게이트(이동·퀘스트창 열림/닫힘)를 여기서 확인한다.
    // 이벤트 콜백으로 처리되는 게이트(기계배치·퀘스트수락)는 HandleMachinePlaced·HandleQuestAccepted에서 처리한다.
    private void PollActiveGate()
    {
        switch (activeGate)
        {
            case TutorialGate.WaitMovementInput:
                // 이동키를 처음 감지한 시점부터 MovementConfirmDelaySeconds만큼 더 조작해 볼 시간을 준 뒤 진행한다.
                if (!movementInputDetected)
                {
                    if (IsMovementInputPressed())
                    {
                        movementInputDetected = true;
                        movementInputDetectedAtUnscaledTime = Time.unscaledTime;
                    }
                }
                else if (Time.unscaledTime - movementInputDetectedAtUnscaledTime >= MovementConfirmDelaySeconds)
                {
                    CompleteGate(TutorialGate.WaitMovementInput);
                }

                break;

            case TutorialGate.WaitPlacementModeClose:
                if (placementController == null || !placementController.IsPlacementMode)
                {
                    CompleteGate(TutorialGate.WaitPlacementModeClose);
                }

                break;

            case TutorialGate.WaitQuestWindowOpen:
                if (QuestWindowController.Instance != null && QuestWindowController.Instance.IsOpen)
                {
                    CompleteGate(TutorialGate.WaitQuestWindowOpen);
                }

                break;

            case TutorialGate.WaitQuestWindowClose:
                if (QuestWindowController.Instance == null || !QuestWindowController.Instance.IsOpen)
                {
                    CompleteGate(TutorialGate.WaitQuestWindowClose);
                }

                break;

            case TutorialGate.WaitRecipeBookOpen:
                if (RecipeBookUI.IsOpen)
                {
                    CompleteGate(TutorialGate.WaitRecipeBookOpen);
                }

                break;

            case TutorialGate.WaitRecipeBookClose:
                if (!RecipeBookUI.IsOpen)
                {
                    CompleteGate(TutorialGate.WaitRecipeBookClose);
                }

                break;

            case TutorialGate.WaitTechTreeOpen:
                if (TechTreeUI.IsOpen)
                {
                    CompleteGate(TutorialGate.WaitTechTreeOpen);
                }

                break;

            case TutorialGate.WaitTechTreeClose:
                if (!TechTreeUI.IsOpen)
                {
                    CompleteGate(TutorialGate.WaitTechTreeClose);
                }

                break;

            case TutorialGate.WaitOreCollected:
                if (GetPlayerItemCount("iron_ore") > oreCountAtGateStart)
                {
                    CompleteGate(TutorialGate.WaitOreCollected);
                }

                break;

            case TutorialGate.WaitIronIngotCollected:
                if (GetPlayerItemCount("iron_bar") > ingotCountAtGateStart)
                {
                    CompleteGate(TutorialGate.WaitIronIngotCollected);
                }

                break;

            case TutorialGate.WaitMachineBroken:
                // 이미 고장난 기계가 있으면(자연 발생 타이밍이 대사보다 먼저였을 수 있음) 바로 통과한다.
                if (ProductionEventManager.Instance != null && ProductionEventManager.Instance.BrokenMachine != null)
                {
                    CompleteGate(TutorialGate.WaitMachineBroken);
                }

                break;

            case TutorialGate.WaitMachineRepaired:
                if (ProductionEventManager.Instance == null || ProductionEventManager.Instance.BrokenMachine == null)
                {
                    CompleteGate(TutorialGate.WaitMachineRepaired);
                }

                break;
        }
    }

    private static bool IsMovementInputPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.wKey.isPressed || keyboard.aKey.isPressed
            || keyboard.sKey.isPressed || keyboard.dKey.isPressed
            || keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed
            || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;
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

        TutorialActionLock.SetTutorialActive(true);
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
