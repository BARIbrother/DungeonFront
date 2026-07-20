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
            GameSessionState.Instance.OnNewGame += HandleNewGameInvoked;

            // [부트스트랩] Play 시 자동 NewGame() 실행
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
            GameSessionState.Instance.OnNewGame -= HandleNewGameInvoked;
        }
    }

    /// <summary>
    /// [Lead 계약] NewGame 이벤트 수신 시 플레이어 스폰
    /// </summary>
    private void HandleNewGameInvoked()
    {
        Debug.Log("[GameFlowController] NewGame 이벤트 수신 -> PlayerSpawner 실행");
        TriggerPlayerSpawner();
    }

    private void TriggerPlayerSpawner()
    {
        GameObject spawner = GameObject.Find("PlayerSpawner");
        if (spawner != null)
        {
            spawner.SendMessage("SpawnPlayer", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[GameFlowController] PlayerSpawner 연동 성공 및 트리거 완료");
        }
        else
        {
            Debug.Log("[GameFlowController] PlayerSpawner 오브젝트가 현재 씬에 없으므로 대기합니다.");
        }
    }
}