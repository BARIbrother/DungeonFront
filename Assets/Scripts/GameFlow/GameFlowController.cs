// GameFlowController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 새 인풋 시스템 필수 필수!

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Header("[게임 상태 및 시간]")]
    public GamePhase currentPhase = GamePhase.Prepare;
    public int currentDay = 1;
    public float productionDuration = 300f; 
    private float productionRemainingSeconds;
    public float ProductionRemainingSeconds => productionRemainingSeconds;

    [Header("[UI 오브젝트 연결]")]
    public GameObject orderWindow;      
    public GameObject shopWindow;       
    public GameObject minimapUI;        
    public GameObject inventoryUI;      
    public GameObject resultWindow;     

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
        NewGame();
    }

    private void Update()
    {
        if (currentPhase == GamePhase.Production)
        {
            if (productionRemainingSeconds > 0)
            {
                productionRemainingSeconds -= Time.deltaTime;
                if (productionRemainingSeconds <= 0)
                {
                    productionRemainingSeconds = 0;
                    SetPhase(GamePhase.Settlement); 
                }
            }
        }

        if (currentPhase == GamePhase.Prepare)
        {
            HandlePrepareInput();
        }
    }

    public void NewGame()
    {
        currentDay = 1;
        SetPhase(GamePhase.Prepare);
        SceneManager.LoadScene("Factory");
    }

    public void SetPhase(GamePhase nextPhase)
    {
        currentPhase = nextPhase;
        Debug.Log($"[GameFlow] 페이즈 전환 -> {currentPhase}");

        switch (currentPhase)
        {
            case GamePhase.Prepare:
                if(minimapUI != null) minimapUI.SetActive(true);
                if(inventoryUI != null) inventoryUI.SetActive(true);
                if(orderWindow != null) orderWindow.SetActive(false);
                if(shopWindow != null) shopWindow.SetActive(false);
                if(resultWindow != null) resultWindow.SetActive(false);
                break;

            case GamePhase.Production:
                productionRemainingSeconds = productionDuration;
                if(orderWindow != null) orderWindow.SetActive(false);
                if(shopWindow != null) shopWindow.SetActive(false);
                if(minimapUI != null) minimapUI.SetActive(true);
                if(inventoryUI != null) inventoryUI.SetActive(true);
                break;

            case GamePhase.Settlement:
                SceneManager.LoadScene("Settlement");
                if(resultWindow != null) resultWindow.SetActive(true);
                break;
        }
    }

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

    public void StartProduction()
    {
        if (currentPhase == GamePhase.Prepare)
        {
            SetPhase(GamePhase.Production);
        }
    }

    public void AdvanceDay()
    {
        if (currentPhase == GamePhase.Settlement)
        {
            currentDay++;
            SetPhase(GamePhase.Prepare);
            SceneManager.LoadScene("Factory");
        }
    }
}