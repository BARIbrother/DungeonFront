using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro; 
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class FactoryState { }

[System.Serializable]
public class AcceptedQuestState 
{
    public int questId;
    public string questName;
    public bool isMandatory;       // 필수(스토리) 의뢰 여부
    public int rewardReputation;   // 보상 명성 수치
    public int deadlineDay;        // 만료 날짜 (ex: 3일차 Prepare 시점 만료)

    // 매개변수가 2개만 들어와도 기본값이 적용되도록 설정 (하방 호환성 유지)
    public AcceptedQuestState(int id, string name, bool isMandatory = false, int rewardReputation = 0, int deadlineDay = 999) 
    { 
        this.questId = id; 
        this.questName = name; 
        this.isMandatory = isMandatory;
        this.rewardReputation = rewardReputation;
        this.deadlineDay = deadlineDay;
    }
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
    // [보완] 명세서 요구사항에 따라 orderWindow, shopWindow는 Dev2가 OnPhaseChanged를 구독해 자체 제어하므로
    // 레거시 UI 오브젝트 연결 및 제어를 제거해도 되지만, 하방 호환성을 위해 변수만 남겨두고 수동 조작 로직을 정리합니다.
    public GameObject minimapUI;        
    public GameObject inventoryUI;      
    public GameObject settlementUI;    

    [Header("[시각화 UI 텍스트 연결]")]
    public TextMeshProUGUI dayText;     
    public TextMeshProUGUI timerText;   
    public TextMeshProUGUI goldText;    
    public TextMeshProUGUI reputationText; 

    [Header("[결산 화면 전용 텍스트 추가]")]
    public TextMeshProUGUI settlementTitleText; 
    public TextMeshProUGUI settlementDayText;   

    [Header("[시각화 UI 버튼 연결]")]
    public Button startProductionButton; 
    public Button advanceDayButton;      

    [Header("[기획서 명세 데이터 필드]")]
    public int day { get; private set; } = 1;               
    public GamePhase phase { get; private set; } = GamePhase.Prepare; 
    public int gold { get; set; } = 0;               
    public int reputation { get; set; } = 0;         
    
    public InventoryState inventory { get; private set; }   
    public FactoryState factory { get; private set; }       
    public List<AcceptedQuestState> quests { get; set; } = new List<AcceptedQuestState>(); 

    private float productionEndTime = 0f;
    // 생산 종료 요약 모달이 열린 동안 중복 EndProduction 호출을 막는다.
    private bool isEndingProduction;
    // 1일차는 테스트 모드·연료 해금 여부와 무관하게 기획대로 3분(180초) 고정.
    private float TargetProductionTime => day == 1
        ? 180f
        : isTestMode
            ? 10f
            : UnlockManager.Instance != null
                ? UnlockManager.Instance.GetProductionSeconds()
                : 180f;
    private string lastTimerText;

    public GamePhase Phase => phase;

    public bool IsTestMode => isTestMode;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        FindUIObjectsAutomatically();
        RefreshHudTexts();
        NewGame(); // ⭐ 실행 시 골드 100, 명성 10 설정 및 OnNewGame 이벤트로 해금 리셋!
    }

    // Production 등 다른 씬으로 옮겨도 DayText/TimerText를 다시 붙인다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIObjectsAutomatically();
        RefreshHudTexts();
    }

    private void RefreshHudTexts()
    {
        UpdateDayText();
        UpdateTimerUI();
    }

    // UIManager가 Production Canvas의 Day/Time TMP를 연결할 때 사용한다.
    public void BindPrimaryHud(TextMeshProUGUI day, TextMeshProUGUI timer)
    {
        if (day != null)
        {
            dayText = day;
        }

        if (timer != null)
        {
            timerText = timer;
        }

        RefreshHudTexts();
    }

    // Day/Time 아래의 "생산 시작" 버튼을 연결한다.
    public void BindStartProductionButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        startProductionButton = button;
        startProductionButton.onClick.RemoveAllListeners();
        startProductionButton.onClick.AddListener(StartProduction);
        UiButtonStyle.Apply(startProductionButton);
        ApplyUIState(phase);
    }

    // Day/Time 아래의 "다음 일차 시작" 버튼을 연결한다.
    public void BindAdvanceDayButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        advanceDayButton = button;
        advanceDayButton.onClick.RemoveAllListeners();
        advanceDayButton.onClick.AddListener(AdvanceDay);
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
        
        // [체크리스트 명세 실행] 페이즈 변경 시 이벤트를 전파하여 외부 스크립트(Lead/Dev2 UI 등)가 감지하도록 함
        OnPhaseChanged?.Invoke(phase);
    }

    private void ApplyUIState(GamePhase currentPhase)
    {
        if (timerText != null) timerText.gameObject.SetActive(true);

        switch (currentPhase)
        {
            case GamePhase.Prepare:
                if (minimapUI != null) minimapUI.SetActive(true);
                if (inventoryUI != null) inventoryUI.SetActive(true);
                if (settlementUI != null) settlementUI.SetActive(false); 

                if (startProductionButton != null) startProductionButton.gameObject.SetActive(true);
                if (advanceDayButton != null) advanceDayButton.gameObject.SetActive(false);
                break;

            case GamePhase.Production:
                if (minimapUI != null) minimapUI.SetActive(true);
                if (inventoryUI != null) inventoryUI.SetActive(true);
                if (settlementUI != null) settlementUI.SetActive(false);

                if (startProductionButton != null) startProductionButton.gameObject.SetActive(false);
                if (advanceDayButton != null) advanceDayButton.gameObject.SetActive(false);
                break;

            case GamePhase.Settlement:
                if (minimapUI != null) minimapUI.SetActive(false);    
                if (inventoryUI != null) inventoryUI.SetActive(false);  
                
                if (settlementUI != null) settlementUI.SetActive(true); 

                if (settlementTitleText != null) settlementTitleText.text = $"Day{day} Settlement"; 
                if (settlementDayText != null) settlementDayText.text = $"Day Progress: {day}"; 

                if (startProductionButton != null) startProductionButton.gameObject.SetActive(false);
                if (advanceDayButton != null) advanceDayButton.gameObject.SetActive(true); 
                break;
        }
    }

    public void FindUIObjectsAutomatically()
    {
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

        if (startProductionButton != null)
        {
            startProductionButton.onClick.RemoveAllListeners();
            startProductionButton.onClick.AddListener(StartProduction);
            UiButtonStyle.Apply(startProductionButton);
        }

        if (advanceDayButton != null)
        {
            advanceDayButton.onClick.RemoveAllListeners();
            advanceDayButton.onClick.AddListener(AdvanceDay);
            UiButtonStyle.Apply(advanceDayButton);
        }
    }

    public bool TryAcceptQuest(int id, string name, bool isMandatory = false, int rewardReputation = 0, int durationDays = 1)
    {
        if (quests.Exists(q => q.questId == id))
        {
            quests.RemoveAll(q => q.questId == id);
            Debug.Log($"<color=orange>[의뢰 취소] {name} 취소됨. (현재: {quests.Count}/3)</color>");
            return true; 
        }

        if (quests.Count >= 3)
        {
            Debug.LogWarning($"[의뢰 실패] 이미 최대치(3개)의 의뢰를 수락했습니다.");
            return false; 
        }

        // 만료 날짜 계산: 현재 날짜(day) + 기한(durationDays)
        // 예: 1일차 Prepare에 2일 기한 의뢰 수락 -> deadlineDay = 3 -> 3일차 Prepare 시작 시 미납 판정!
        int calculatedDeadline = this.day + durationDays;

        quests.Add(new AcceptedQuestState(id, name, isMandatory, rewardReputation, calculatedDeadline));
        Debug.Log($"<color=cyan>[의뢰 수락] {name} (만료일: Day {calculatedDeadline}) 추가됨. (현재: {quests.Count}/3)</color>");
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

        QuestDataStore questData = FindAnyObjectByType<QuestDataStore>();
        questData?.AdvanceDay();

        SetPhase(GamePhase.Prepare);
    }

    private void UpdateDayText()
    {
        if (dayText != null) dayText.text = $"Day : {day}";
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        string next;
        switch (phase)
        {
            case GamePhase.Prepare:
                next = $"Time Left: {TargetProductionTime:F1}s";
                break;
            case GamePhase.Production:
                // 0.1초 단위가 바뀔 때만 문자열을 갱신한다.
                float remaining = ProductionRemainingSeconds;
                float rounded = Mathf.Ceil(remaining * 10f) / 10f;
                next = $"Producing: {rounded:F1}s";
                break;
            case GamePhase.Settlement:
                next = "Production Complete: 0.0s";
                break;
            default:
                return;
        }

        if (next == lastTimerText)
        {
            return;
        }

        lastTimerText = next;
        timerText.text = next;
    }

    public void UpdateGoodsUI()
    {
        if (goldText != null) goldText.text = $"Gold: {gold:G}";
        if (reputationText != null) reputationText.text = $"Reputation: {reputation:G}";
    }

    public void AddGold(int amount) { gold += amount; UpdateGoodsUI(); }
    public void AddReputation(int amount) { reputation += amount; UpdateGoodsUI(); }

    public void RestoreSession(int savedDay, GamePhase savedPhase, int savedGold, int savedReputation)
    {
        day = Mathf.Max(1, savedDay);
        gold = Mathf.Max(0, savedGold);
        reputation = Mathf.Max(0, savedReputation);
        phase = savedPhase;
        productionEndTime = 0f;
        isEndingProduction = false;
        FindUIObjectsAutomatically();
        UpdateDayText();
        UpdateTimerUI();
        UpdateGoodsUI();
        ApplyUIState(phase);
        OnPhaseChanged?.Invoke(phase);
    }

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

    // Dev Mode: 유효 전이 규칙을 무시하고 페이즈를 맞춘다.
    public void ForcePhase(GamePhase next)
    {
        if (phase == next)
        {
            ApplyUIState(phase);
            return;
        }

        phase = next;
        if (phase == GamePhase.Production)
        {
            isEndingProduction = false;
            productionEndTime = Time.time + TargetProductionTime;
        }
        else
        {
            productionEndTime = 0f;
            isEndingProduction = false;
        }

        UpdateDayText();
        ApplyUIState(phase);
        UpdateTimerUI();
        OnPhaseChanged?.Invoke(phase);
        Debug.Log($"[GameSession] ForcePhase -> {phase}");
    }

    // Dev Mode: 일차를 절대값으로 맞춘다.
    public void SetDay(int nextDay)
    {
        day = Mathf.Max(1, nextDay);
        UpdateDayText();
        if (phase == GamePhase.Settlement && settlementDayText != null)
        {
            settlementDayText.text = $"Day Progress: {day}";
        }

        Debug.Log($"[GameSession] SetDay -> {day}");
    }
}
