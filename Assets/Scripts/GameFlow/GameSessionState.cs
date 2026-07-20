using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem; 
using TMPro; 
using UnityEngine.UI; 
using UnityEngine.SceneManagement; // [추가] 씬 전환을 위해 추가

public class InventoryState { }
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
    public int gold { get; private set; } = 0;               
    public int reputation { get; private set; } = 0;         
    
    public InventoryState inventory { get; private set; }   
    public FactoryState factory { get; private set; }       
    public List<AcceptedQuestState> quests { get; set; } = new List<AcceptedQuestState>(); 

    private float productionEndTime = 0f;                   
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

            if (Time.time >= productionEndTime)
            {
                SetPhase(GamePhase.Settlement); 
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

        Debug.Log($"[NewGame] 데이터 리셋 완료");
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

        // [체크리스트 명세 실현] 페이즈에 따른 실제 씬 전환 흐름 강제 연동
        if (phase == GamePhase.Settlement)
        {
            // Production 카운트다운 완료 후 자동으로 Settlement 씬으로 이동
            SceneManager.LoadScene("SettlementScene"); 
        }
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
    }

    public bool TryAcceptQuest(int id, string name)
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
        productionEndTime = Time.time + TargetProductionTime; 
        SetPhase(GamePhase.Production);
    }

    public void AdvanceDay()
    {
        if (phase != GamePhase.Settlement) return;
        day++;
        UpdateDayText(); 
        SetPhase(GamePhase.Prepare);

        // [체크리스트 명세 실현] AdvancedDay() 호출 후 Factory 씬으로 안전하게 복귀
        SceneManager.LoadScene("FactoryScene");
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

    public void ForceEndProduction()
    {
        if (phase == GamePhase.Production) productionEndTime = Time.time;
    }
}