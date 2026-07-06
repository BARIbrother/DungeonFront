using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem; // 새 인풋 시스템 필수

// 타 이슈 연동용 스텁(Stub) 클래스 정의
public class InventoryState { }
public class FactoryState { }
public class AcceptedQuestState { }

public class GameSessionState : MonoBehaviour
{
    // 싱글톤 패턴으로 어디서나 접근 가능하게 설정
    public static GameSessionState Instance { get; private set; }

    // 페이즈 변경을 외부 시스템(Lead/UI)이 감지할 수 있도록 액션 이벤트 제공
    public event Action<GamePhase> OnPhaseChanged;

    [Header("[임시 UI 오브젝트 연결]")]
    public GameObject orderWindow;      // 의뢰창
    public GameObject shopWindow;       // 구매창
    public GameObject minimapUI;        // 미니맵 UI
    public GameObject inventoryUI;      // 인벤토리 UI

    [Header("[게임 데이터 필드]")]
    public int day { get; private set; }
    public GamePhase phase { get; private set; }
    public int gold { get; private set; }
    public int reputation { get; private set; }
    
    // 스텁 필드들
    public InventoryState inventory { get; private set; }
    public FactoryState factory { get; private set; }
    public List<AcceptedQuestState> quests { get; private set; } = new List<AcceptedQuestState>();

    // 생산 종료 시간 (타이머 체크용)
    private float productionEndTime = 0f;

    // 외부용 프로퍼티
    public GamePhase Phase => phase;
    
    // Production일 때만 남은 시간 계산, 아닐 때는 0 반환
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
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 데이터가 유지되도록 설정
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        NewGame();
    }

    private void Update()
    {
        // 1. 생산(Production) 중 매 프레임 타이머 체크
        if (phase == GamePhase.Production)
        {
            if (Time.time >= productionEndTime)
            {
                SetPhase(GamePhase.Settlement); // 시간 만료 시 결산 단계로 자동 전환
            }
        }

        // 2. 준비(Prepare) 단계에서만 키보드 입력으로 창 열고 닫기 허용
        if (phase == GamePhase.Prepare)
        {
            HandlePrepareInput();
        }
    }

    // 새 게임 시작 기능 (초기 데이터 세팅)
    public void NewGame()
    {
        day = 1;
        phase = GamePhase.Prepare;
        gold = 0;
        reputation = 0;
        inventory = new InventoryState(); 
        factory = new FactoryState();     
        quests.Clear();                   
        productionEndTime = 0f;

        Debug.Log($"[NewGame] 게임 시작 - 일차: {day}, 페이즈: {phase}");
        
        // 씬 로드 직후 Hierarchy 창에서 UI 오브젝트들을 자동으로 찾아서 연결합니다.
        FindUIObjectsAutomatically();
        
        // 초기 UI 상태 세팅 강제 호출
        ApplyUIState(phase);
        OnPhaseChanged?.Invoke(phase);
    }

    // 페이즈 전환 제어 및 유효성 검사
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

        // 유효하지 않은 전환은 차단하고 경고 로그를 남깁니다.
        if (!isValidTransition)
        {
            Debug.LogWarning($"[GameSession] 유효하지 않은 페이즈 전환 시도 거부됨: {phase} -> {next}");
            return;
        }

        // 상태 전환 수행
        phase = next;
        Debug.Log($"[GameSession] 페이즈 전환 완료 -> {phase}");
        
        // 씬 전환이나 리로드 직후에도 정상 작동하도록 UI를 다시 한 번 자동으로 감지합니다.
        FindUIObjectsAutomatically();
        
        // 전환된 페이즈에 따라 UI 상태 적용 및 이벤트 전파
        ApplyUIState(phase);
        OnPhaseChanged?.Invoke(phase);
    }

    // Hierarchy 계층 구조에서 이름이 일치하는 UI 오브젝트를 자동으로 찾아 연결하는 함수
    private void FindUIObjectsAutomatically()
    {
        // 인스펙터 연결이 Missing(유실)되었거나 비어있을 때만 이름으로 찾습니다.
        if (orderWindow == null) orderWindow = GameObject.Find("orderWindow");
        if (shopWindow == null) shopWindow = GameObject.Find("shopWindow");
        if (minimapUI == null) minimapUI = GameObject.Find("minimapUI");
        if (inventoryUI == null) inventoryUI = GameObject.Find("inventoryUI");
    }

    // 페이즈별 UI 활성화/비활성화 규칙 처리 함수
    private void ApplyUIState(GamePhase currentPhase)
    {
        switch (currentPhase)
        {
            case GamePhase.Prepare:
                if (minimapUI != null) minimapUI.SetActive(true);
                if (inventoryUI != null) inventoryUI.SetActive(true);
                if (orderWindow != null) orderWindow.SetActive(false);
                if (shopWindow != null) shopWindow.SetActive(false);
                break;

            case GamePhase.Production:
                // 생산에 돌입하면 의뢰창과 구매창을 강제로 닫고 잠금
                if (orderWindow != null) orderWindow.SetActive(false);
                if (shopWindow != null) shopWindow.SetActive(false);
                if (minimapUI != null) minimapUI.SetActive(true);
                if (inventoryUI != null) inventoryUI.SetActive(true);
                break;

            case GamePhase.Settlement:
                // 결산 단계 진입 시 팩토리 UI 정리 (이후 다른 씬/결과창 제어용)
                if (orderWindow != null) orderWindow.SetActive(false);
                if (shopWindow != null) shopWindow.SetActive(false);
                break;
        }
    }

    // 준비 단계 키 입력 처리 (O키: 의뢰창 토글, P키: 구매창 토글)
    private void HandlePrepareInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.oKey.wasPressedThisFrame && orderWindow != null)
        {
            orderWindow.SetActive(!orderWindow.activeSelf);
        }

        if (Keyboard.current.pKey.wasPressedThisFrame && shopWindow != null)
        {
            shopWindow.SetActive(!shopWindow.activeSelf);
        }
    }

    // 생산 시작 버튼 클릭 시 호출할 API
    public void StartProduction()
    {
        if (phase != GamePhase.Prepare)
        {
            Debug.LogWarning("[GameSession] Prepare 단계가 아닐 때는 생산을 시작할 수 없습니다.");
            return;
        }

        productionEndTime = Time.time + 300f; // 현재 시간에 300초(5분) 더함
        SetPhase(GamePhase.Production);
    }

    // 결산 화면에서 「다음 날」 버튼을 누르면 호출할 API
    public void AdvanceDay()
    {
        if (phase != GamePhase.Settlement)
        {
            Debug.LogWarning("[GameSession] Settlement 단계가 아닐 때는 다음 날로 진행할 수 없습니다.");
            return;
        }

        day++;
        SetPhase(GamePhase.Prepare);
        productionEndTime = 0f;
    }

    // 테스트용 디버그 기능: 즉시 생산 종료 트리거
    public void ForceEndProduction()
    {
        if (phase == GamePhase.Production)
        {
            productionEndTime = Time.time;
        }
    }
}