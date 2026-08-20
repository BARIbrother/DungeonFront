using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }

    private const string PauseRequester = "GameOver";

    [Header("[게임오버 UI 연결]")]
    [SerializeField] private GameObject gameOverUI;         // 게임오버 Panel
    [SerializeField] private TMP_Text gameOverMessageText;  // "필수 의뢰를 완료하지 못했습니다" Text
    [SerializeField] private Button titleButton;            // Title 로드 Button

    [Header("[타이틀 씬 이름]")]
    [SerializeField] private string titleSceneName = "TitleScene"; // 실제 프로젝트의 타이틀 씬 이름

    public bool IsGameOver { get; private set; }

    // 씬에 패널이 연결되어 있으면 Overlay는 그리지 않는다.
    public bool HasAssignedUi => gameOverUI != null;

    private GameSessionState boundSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<GameOverController>() != null)
        {
            return;
        }

        var controllerObject = new GameObject("GameOverController");
        controllerObject.AddComponent<GameOverController>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 게임오버 UI 숨김
        if (gameOverUI != null) gameOverUI.SetActive(false);

        // 버튼 클릭 이벤트 연결
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(OnTitleButtonClicked);
        }

        BindSession();
    }

    private void OnDestroy()
    {
        UnbindSession();
        GamePauseService.ReleasePause(PauseRequester);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BindSession()
    {
        GameSessionState candidate = GameSessionState.Instance;
        if (candidate == boundSession)
        {
            return;
        }

        UnbindSession();
        boundSession = candidate;
        if (boundSession == null)
        {
            return;
        }

        boundSession.OnPhaseChanged += HandlePhaseChanged;
        boundSession.OnNewGame += ResetGameOver;
    }

    private void UnbindSession()
    {
        if (boundSession == null)
        {
            return;
        }

        boundSession.OnPhaseChanged -= HandlePhaseChanged;
        boundSession.OnNewGame -= ResetGameOver;
        boundSession = null;
    }

    private void Update()
    {
        BindSession();

        // ⭐ [디버그 기능] 키보드 'G' 키 입력 시 강제 게임오버 발동 (테스트용)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                TriggerGameOver("필수 의뢰를 완료하지 못했습니다");
            }
        }
    }

    // 날짜 변경 시(Prepare 페이즈 진입) 미납 검사 실행
    private void HandlePhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Prepare)
        {
            CheckOverdueQuests(GameSessionState.Instance.day);
        }
    }

    /// <summary>
    /// 기한이 만료된 의뢰들을 검사하여 미납 페널티 또는 게임오버를 처리합니다.
    /// </summary>
    public void CheckOverdueQuests(int currentDay)
    {
        if (GameSessionState.Instance == null || GameSessionState.Instance.quests == null) return;

        // 리스트 원소 삭제를 위해 역순 순회
        for (int i = GameSessionState.Instance.quests.Count - 1; i >= 0; i--)
        {
            var quest = GameSessionState.Instance.quests[i];

            if (IsQuestExpired(quest, currentDay))
            {
                // 1. 필수(스토리) 의뢰 미납 -> 게임오버
                if (quest.isMandatory)
                {
                    TriggerGameOver("필수 의뢰를 완료하지 못했습니다");
                    return;
                }
                // 2. 일반 의뢰 미납 -> 보상 명성의 0.5배 차감
                else
                {
                    int penalty = Mathf.RoundToInt(quest.rewardReputation * 0.5f);
                    GameSessionState.Instance.AddReputation(-penalty);

                    Debug.Log($"<color=red>[미납 페널티] 일반 의뢰 '{quest.questName}' 미납! 명성 -{penalty} 차감</color>");

                    GameSessionState.Instance.quests.RemoveAt(i);
                }
            }
        }
    }

    // 만료 날짜 판정
    private bool IsQuestExpired(AcceptedQuestState quest, int currentDay)
    {
        return currentDay >= quest.deadlineDay;
    }

    // 게임오버 팝업 출력
    public void TriggerGameOver(string message)
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        Debug.LogError($"[GameOver] {message}");

        // 게임 진행은 즉시 멈추고, 패널만 효과음이 끝난 뒤 띄운다.
        GamePauseService.RequestPause(PauseRequester);
        StartCoroutine(ShowGameOverAfterSound(message));
    }

    private IEnumerator ShowGameOverAfterSound(string message)
    {
        AudioManager audio = AudioManager.Instance;
        AudioCatalog.AudioEntry entry = audio != null && audio.Catalog != null
            ? audio.Catalog.gameOver
            : null;

        if (audio != null)
        {
            audio.StopBgm();
            audio.PlaySfx(entry);
        }

        float duration = audio != null ? audio.GetPlaybackDuration(entry) : 0f;
        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        if (gameOverMessageText != null)
        {
            gameOverMessageText.text = message;
        }

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

    }

    public void ResetGameOver()
    {
        if (!IsGameOver && (gameOverUI == null || !gameOverUI.activeSelf))
        {
            GamePauseService.ReleasePause(PauseRequester);
            return;
        }

        IsGameOver = false;
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        GamePauseService.ReleasePause(PauseRequester);
    }

    // Title 로드 버튼 클릭 핸들러
    private void OnTitleButtonClicked()
    {
        ResetGameOver();
        SceneManager.LoadScene(titleSceneName);
    }
}
