using UnityEngine;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("[부트스트랩 설정]")]
    [SerializeField] private bool playAutomatedNewGame = true; // Play 시 자동 NewGame() 수행 여부

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 단일 씬 안에서 관리되므로, 만약 씬 전환이 있더라도 파괴되지 않게 전역 HUD 및 흐름 보존
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // GameSessionState 이벤트 구독 등록
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnPhaseChanged += HandlePhaseChanged;
            GameSessionState.Instance.OnNewGame += HandleNewGameInvoked;

            // [부트스트랩] 명세: Play 시 자동 NewGame() 혹은 디버그 스타트 처리
            if (playAutomatedNewGame)
            {
                GameSessionState.Instance.NewGame();
            }
        }
    }

    private void OnDestroy()
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnPhaseChanged -= HandlePhaseChanged;
            GameSessionState.Instance.OnNewGame -= HandleNewGameInvoked;
        }
    }

    /// <summary>
    /// [체크리스트 명세: 화면 흐름 제어]
    /// 단일 씬 내부에서 UI 패널 활성화를 통해 화면 전환을 시뮬레이션하여 씬 중복 로드 크래시를 원천 방지합니다.
    /// </summary>
    private void HandlePhaseChanged(GamePhase currentPhase)
    {
        if (GameSessionState.Instance == null) return;

        switch (currentPhase)
        {
            case GamePhase.Prepare:
                Debug.Log("[GameFlowController] 정비 단계 활성화 -> Factory 요소 표시");
                // 단일 씬이므로 씬을 로드하지 않고, 기존에 작성된 UI 바인딩 및 데이터를 즉시 갱신합니다.
                GameSessionState.Instance.FindUIObjectsAutomatically();
                GameSessionState.Instance.UpdateGoodsUI();
                break;

            case GamePhase.Production:
                Debug.Log("[GameFlowController] 생산 단계 활성화 -> 타이머 UI 집중 표시");
                break;

            case GamePhase.Settlement:
                Debug.Log("[GameFlowController] 결산 단계 활성화 -> Settlement 스텁 UI 표시");
                // 단일 씬이므로 씬 로드 없이, GameSessionState 내부의 ApplyUIState에 의해 SettlementUI 패널이 켜집니다.
                break;
        }
    }

    /// <summary>
    /// [체크리스트 명세: OnNewGame -> Lead PlayerSpawner 호출 연동]
    /// </summary>
    private void HandleNewGameInvoked()
    {
        Debug.Log("[GameFlowController] NewGame 이벤트 수신 -> 게임 초기화 및 UI 리바인딩 완료");
        
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.FindUIObjectsAutomatically();
            GameSessionState.Instance.UpdateGoodsUI();
        }

        // [체크리스트] OnNewGame시 동등 API나 PlayerSpawner를 호출하는 규칙 연동 처리
        TriggerPlayerSpawner();
    }

    private void TriggerPlayerSpawner()
    {
        // 의존성 해결을 위한 디버깅 및 PlayerSpawner 연동부
        GameObject spawner = GameObject.Find("PlayerSpawner");
        if (spawner != null)
        {
            spawner.SendMessage("SpawnPlayer", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[GameFlowController] PlayerSpawner 연동 성공 및 트리거 완료");
        }
        else
        {
            Debug.Log("[GameFlowController] PlayerSpawner 오브젝트가 현재 씬에 없으므로 스폰 트리거를 대기합니다.");
        }
    }
}