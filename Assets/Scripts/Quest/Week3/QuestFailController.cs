using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuestFailController : MonoBehaviour
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
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        StartCoroutine(ShowPanelAfterGameOverSound());
        return;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Debug.Log("필수 의뢰를 완료하지 못했습니다.", this);
    }

    private IEnumerator ShowPanelAfterGameOverSound()
    {
        AudioManager audio = AudioManager.Instance;
        AudioCatalog.AudioEntry entry = audio != null && audio.Catalog != null
            ? audio.Catalog.gameOver
            : null;
        float duration = audio != null ? audio.GetPlaybackDuration(entry) : 0f;

        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
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
