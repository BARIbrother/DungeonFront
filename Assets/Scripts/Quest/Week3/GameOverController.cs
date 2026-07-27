using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string titleSceneName = "Title";

    public bool IsGameOver { get; private set; }
    private GameSessionState session;

    private void OnEnable()
    {
        session = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();
        if (session != null)
        {
            session.OnNewGame += ResetGameOver;
        }
    }

    private void OnDisable()
    {
        if (session != null)
        {
            session.OnNewGame -= ResetGameOver;
        }
    }

    public void TriggerGameOver()
    {
        IsGameOver = true;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Debug.Log("필수 의뢰를 완료하지 못했습니다.", this);
    }

    public void ReturnToTitle()
    {
        if (!Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogWarning($"Title scene is not in Build Settings: {titleSceneName}", this);
            return;
        }

        SceneManager.LoadScene(titleSceneName);
    }

    public void ResetGameOver()
    {
        IsGameOver = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
}
