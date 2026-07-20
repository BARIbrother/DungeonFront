using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using TMPro; 
using UnityEngine.UI; 

public class FactoryState { }

[System.Serializable]
public class AcceptedQuestState 
{
    public int questId;
    public string questName;
    public AcceptedQuestState(int id, string name) { this.questId = id; this.questName = name; }
}

public class GameSessionState : MonoBehaviour
{
    public static GameSessionState Instance { get; private set; }

    // [체크리스트 명세 연동] 페이즈 변경 및 뉴게임 이벤트
    public event Action<GamePhase> OnPhaseChanged;
    public event Action OnNewGame; 

    [Header("[테스트 설정]")]
    [SerializeField] private bool isTestMode = true; 

    [Header("[UI 오브젝트 연결]")]
    public GameObject orderWindow;      
    public GameObject shopWindow;       
    public GameObject minimapUI;        
    public GameObject inventoryUI;      
    public GameObject settlementUI;    

    [Header("[시각화 UI 텍스트 연결]")]
    public TextMeshProUGUI dayText;     
    public TextMeshProUGUI timerText;   
    public TextMeshProUGUI goldText;    
    public TextMeshProUGUI reputationText; 

    [Header("[시각화 UI 버튼 연결]")]
    public Button startProductionButton; 
    public Button advanceDayButton;      

    [Header("[기획서 명세 데이터 필드]")]
    public int day { get; private set; } = 1;               
    public GamePhase phase { get; private set; } = GamePhase.Prepare; 
    public int gold { get; private set; } = 0;               
    public int reputation { get; private set; } = 0;         
    
    public InventoryState inventory { get; private set; }   
    public FactoryState factory { get; private set; }       
    public List<AcceptedQuestState> quests { get; set; } = new List<AcceptedQuestState>(); 

    private float productionEndTime = 0f;
    // 생산 종료 요약 모달이 열린 동안 중복 EndProduction 호출을 막는다.
    private bool isEndingProduction;
    private float TargetProductionTime => isTestMode ? 10f : 300f;

    public GamePhase Phase => phase;
    
    public float ProductionRemainingSeconds
    {
        get
        {
            if (phase != GamePhase.Production) return 0f;
            float remaining = productionEndTime - Time.time;
            return Mathf.Max(0f, remaining);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindUIObjectsAutomatically();
        UpdateDayText(); 
        UpdateTimerUI(); 
        UpdateGoodsUI(); 
        ApplyUIState(phase);
    }

    private void Update()
    {
        if (phase == GamePhase.Production)
        {
            UpdateTimerUI();

            if (!isEndingProduction && Time.time >= productionEndTime)
            {
                EndProduction();
            }
        }

        if (!isEndingProduction && (phase == GamePhase.Prepare || phase == GamePhase.Production))
        {
            HandleGlobalInput();
        }
    }

    public void NewGame()
    {
        day = 1;
        phase = GamePhase.Prepare;
        gold = 100; 
        reputation = 10; 
        inventory = new InventoryState(); 
        factory = new FactoryState();     
        quests.Clear();                   
        productionEndTime = 0f;
        isEndingProduction = false;

        inventory.machines.Add(new MachineInstanceState
        {
            instanceId = Guid.NewGuid().ToString(),
            machineDefId = "Miner_1",
            placement = MachinePlacement.InInventory
        });

        inventory.machines.Add(new MachineInstanceState
        {
            instanceId = Guid.NewGuid().ToString(),
            machineDefId = "Smelter_1",
            placement = MachinePlacement.InInventory
        });

        inventory.machines.Add(new MachineInstanceState
        {
            instanceId = Guid.NewGuid().ToString(),
            machineDefId = "Assembler_1",
            placement = MachinePlacement.InInventory
        });

        Debug.Log($"[NewGame] 데이터 리셋 완료 — machines={inventory.machines.Count}");
        UpdateDayText();
        UpdateTimerUI();
        UpdateGoodsUI();
        ApplyUIState(phase);

        // [체크리스트] OnNewGame 이벤트 발생 -> 외부 스폰 로직 트리거용
        OnNewGame?.Invoke();
    }

    public void SetPhase(GamePhase next)
    {
        bool isValidTransition = false;

        switch (phase)
        {
            case GamePhase.Prepare:
                if (next == GamePhase.Production) isValidTransition = true;
                break;
            case GamePhase.Production:
                if (next == GamePhase.Settlement) isValidTransition = true;
                break;
            case GamePhase.Settlement:
                if (next == GamePhase.Prepare) isValidTransition = true;
                break;
        }

        if (!isValidTransition)
        {
            Debug.LogWarning($"[GameSession] 유효하지 않은 페이즈 전환 시도 거부됨: {phase} -> {next}");
            return;
        }

        phase = next;
        
        if (phase != GamePhase.Production)
        {
            productionEndTime = 0f;
        }

        Debug.Log($"[GameSession] 페이즈 전환 완료 -> {phase}");
        
        ApplyUIState(phase);
        UpdateTimerUI(); 
        
        // [체크리스트] 페이즈 변경 이벤트 알림 -> GameFlowController가 수신
        OnPhaseChanged?.Invoke(phase);
    }

    [Header("[결산 화면 전용 텍스트 추가]")]
    public TextMeshProUGUI settlementTitleText; // "결산" 또는 "X일차 결산"
    public TextMeshProUGUI settlementDayText;   // 결산창 내부 일차 표시용 (session.day)

    private void ApplyUIState(GamePhase currentPhase)
    {
        if (timerText != null) timerText.gameObject.SetActive(true);

        switch (currentPhase)
        {
            case GamePhase.Prepare:
                // [기획서 요건] 정비 단계 시 Factory 요소 표시
                if (minimapUI != null) minimapUI.SetActive(true);
                if (inventoryUI != null) inventoryUI.SetActive(true);
                if (shopWindow != null) shopWindow.SetActive(false);
                if (settlementUI != null) settlementUI.SetActive(false); // 결산 가리기
                if (orderWindow != null) orderWindow.SetActive(false); 

                if (startProductionButton != null) startProductionButton.gameObject.SetActive(true);
                if (advanceDayButton != null) advanceDayButton.gameObject.SetActive(false);
                break;

            case GamePhase.Production:
                if (shopWindow != null) shopWindow.SetActive(false); 
                if (minimapUI != null) minimapUI.SetActive(true);
                if (inventoryUI != null) inventoryUI.SetActive(true);
                if (settlementUI != null) settlementUI.SetActive(false);

                if (startProductionButton != null) startProductionButton.gameObject.SetActive(false);
                if (advanceDayButton != null) advanceDayButton.gameObject.SetActive(false);
                break;

            case GamePhase.Settlement:
                // [기획서 요건] Settlement가 되면 이 화면만 보임 (Factory 숨김 시뮬레이션)
                if (orderWindow != null) orderWindow.SetActive(false);
                if (shopWindow != null) shopWindow.SetActive(false);
                if (minimapUI != null) minimapUI.SetActive(false);    // 맵 숨기기
                if (inventoryUI != null) inventoryUI.SetActive(false);  // 인벤토리 숨기기
                
                if (settlementUI != null) settlementUI.SetActive(true); // 결산 창만 활성화

                // 결산 화면 내 요소 실시간 반영 (명세서 요건)
                if (settlementTitleText != null) settlementTitleText.text = $"Day{day} Settlement"; // n일차 결산
                if (settlementDayText != null) settlementDayText.text = $"Day Progress: {day}"; // 진행일차 : n일

                if (startProductionButton != null) startProductionButton.gameObject.SetActive(false);
                if (advanceDayButton != null) advanceDayButton.gameObject.SetActive(true); // 다음날 버튼 표시
                break;
        }
    }

    // 오브젝트 자동 찾기 기능에 결산 텍스트 2개 추가
    public void FindUIObjectsAutomatically()
    {
        if (orderWindow == null) orderWindow = GameObject.Find("OrderWindow");
        if (shopWindow == null) shopWindow = GameObject.Find("ShopWindow");
        if (minimapUI == null) minimapUI = GameObject.Find("MinimapUI");
        if (inventoryUI == null) inventoryUI = GameObject.Find("InventoryUI");
        if (settlementUI == null) settlementUI = GameObject.Find("SettlementUI");
        
        if (dayText == null) dayText = GameObject.Find("DayText")?.GetComponent<TextMeshProUGUI>();
        if (timerText == null) timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        if (goldText == null) goldText = GameObject.Find("GoldText")?.GetComponent<TextMeshProUGUI>();
        if (reputationText == null) reputationText = GameObject.Find("ReputationText")?.GetComponent<TextMeshProUGUI>();

        if (settlementTitleText == null) settlementTitleText = GameObject.Find("SettlementTitleText")?.GetComponent<TextMeshProUGUI>();
        if (settlementDayText == null) settlementDayText = GameObject.Find("SettlementDayText")?.GetComponent<TextMeshProUGUI>();

        if (startProductionButton == null) startProductionButton = GameObject.Find("StartProductionButton")?.GetComponent<Button>();
        if (advanceDayButton == null) advanceDayButton = GameObject.Find("AdvanceDayButton")?.GetComponent<Button>();
    }

    private void HandleGlobalInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.oKey.wasPressedThisFrame && orderWindow != null)
        {
            orderWindow.SetActive(!orderWindow.activeSelf);
        }

        // F: 생산 즉시 종료 (요약 모달 → Settlement). E는 수리·수작업용.
        if (phase == GamePhase.Production && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ForceEndProduction();
            return;
        }

        if (phase == GamePhase.Prepare)
        {
            if (Keyboard.current.pKey.wasPressedThisFrame && shopWindow != null)
            {
                shopWindow.SetActive(!shopWindow.activeSelf);
            }
        }
    }

    public bool TryAcceptQuest(int id, string name)
    {
    // [수정] 1. 이미 수락한 의뢰인지 검사 -> 있으면 취소 처리
    if (quests.Exists(q => q.questId == id))
    {
        quests.RemoveAll(q => q.questId == id);
        Debug.Log($"<color=orange>[의뢰 취소] {name} 취소됨. (현재: {quests.Count}/3)</color>");
        return true; // 상태 전환 성공으로 리턴
    }

    // 2. 이미 3개를 수락했는지 개수 검사
    if (quests.Count >= 3)
    {
        Debug.LogWarning($"[의뢰 실패] 이미 최대치(3개)의 의뢰를 수락했습니다.");
        return false; // 자리가 없으므로 실패 리턴
    }

    // 3. 의뢰 추가
    quests.Add(new AcceptedQuestState(id, name));
    Debug.Log($"<color=cyan>[의뢰 수락] {name} 추가됨. (현재: {quests.Count}/3)</color>");
    return true;
    }

    public void RemoveQuest(int id)
    {
        quests.RemoveAll(q => q.questId == id);
    }

    public void StartProduction()
    {
        if (phase != GamePhase.Prepare) return;
        isEndingProduction = false;
        productionEndTime = Time.time + TargetProductionTime;
        SetPhase(GamePhase.Production);
    }

    public void AdvanceDay()
    {
        if (phase != GamePhase.Settlement) return;
        day++;
        UpdateDayText(); 
        SetPhase(GamePhase.Prepare);
    }

    private void UpdateDayText()
    {
        if (dayText != null) dayText.text = $"Day : {day}";
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        switch (phase)
        {
            case GamePhase.Prepare:
                timerText.text = $"Time Left: {TargetProductionTime:F1}s";
                break;
            case GamePhase.Production:
                float remaining = ProductionRemainingSeconds;
                timerText.text = $"Producing: {remaining:F1}s";
                break;
            case GamePhase.Settlement:
                timerText.text = $"Production Complete: 0.0s";
                break;
        }
    }

    public void UpdateGoodsUI()
    {
        if (goldText != null) goldText.text = $"Gold: {gold:G}";
        if (reputationText != null) reputationText.text = $"Reputation: {reputation:G}";
    }

    public void AddGold(int amount) { gold += amount; UpdateGoodsUI(); }
    public void AddReputation(int amount) { reputation += amount; UpdateGoodsUI(); }

    // 타이머 만료·조기 종료 공통 진입점. 요약 확인 전까지 Settlement로 가지 않는다.
    public void EndProduction()
    {
        if (phase != GamePhase.Production || isEndingProduction)
        {
            return;
        }

        isEndingProduction = true;
        productionEndTime = 0f;
        ProductionEndHandler.EndProduction();
    }

    // 요약 모달 확인 후 Settlement 전환이 끝났을 때 종료 가드를 해제한다.
    public void ClearEndingProduction()
    {
        isEndingProduction = false;
    }

    public void ForceEndProduction()
    {
        if (phase != GamePhase.Production)
        {
            return;
        }

        EndProduction();
    }
}