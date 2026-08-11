using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Production Canvas의 HUD를 GameSessionState에 연결한다.
// Day/Time은 Production 씬에 두고, 필요 시 레이아웃만 보정한다.
// 결산 단계용 "다음 일차" 버튼도 Day/Time 아래에 둔다.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button advanceDayButton;

    public TextMeshProUGUI DayText => dayText;
    public TextMeshProUGUI TimerText => timerText;
    public Button AdvanceDayButton => advanceDayButton;

    public void SetTargetCanvas(Canvas canvas)
    {
        targetCanvas = canvas;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<UIManager>() != null)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject go = new GameObject("UIManager");
        UIManager manager = go.AddComponent<UIManager>();
        manager.SetTargetCanvas(canvas);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        ResolveHudRefs();
        EnsureTopLayout(dayText != null ? dayText.rectTransform : null, 24f);
        EnsureTopLayout(timerText != null ? timerText.rectTransform : null, 64f);
        EnsureAdvanceDayButton();
        BindSessionHud();
    }

    private void ResolveHudRefs()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>();
        }

        if (dayText == null && targetCanvas != null)
        {
            Transform day = targetCanvas.transform.Find("DayText");
            if (day != null)
            {
                dayText = day.GetComponent<TextMeshProUGUI>();
            }
        }

        if (timerText == null && targetCanvas != null)
        {
            Transform timer = targetCanvas.transform.Find("TimerText");
            if (timer != null)
            {
                timerText = timer.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void EnsureAdvanceDayButton()
    {
        if (targetCanvas == null)
        {
            return;
        }

        if (advanceDayButton == null)
        {
            Transform existing = targetCanvas.transform.Find("AdvanceDayButton");
            if (existing != null)
            {
                advanceDayButton = existing.GetComponent<Button>();
            }
        }

        if (advanceDayButton == null)
        {
            advanceDayButton = CreateAdvanceDayButton(targetCanvas.transform);
        }

        if (advanceDayButton == null)
        {
            return;
        }

        RectTransform rect = advanceDayButton.transform as RectTransform;
        if (rect != null)
        {
            rect.SetParent(targetCanvas.transform, false);
            EnsureTopLayout(rect, 112f);
            rect.sizeDelta = new Vector2(280f, 40f);
        }

        advanceDayButton.gameObject.SetActive(false);
    }

    private static Button CreateAdvanceDayButton(Transform parent)
    {
        var buttonObject = new GameObject("AdvanceDayButton", typeof(RectTransform));
        var rect = (RectTransform)buttonObject.transform;
        rect.SetParent(parent, false);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.28f, 0.45f, 0.32f, 0.95f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        UiButtonStyle.Apply(button);

        var labelObject = new GameObject("Label", typeof(RectTransform));
        var labelRect = (RectTransform)labelObject.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "다음 일차 시작";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }

    private void BindSessionHud()
    {
        GameSessionState session = GameSessionState.Instance;
        if (session == null)
        {
            session = FindAnyObjectByType<GameSessionState>();
        }

        if (session == null)
        {
            return;
        }

        session.BindPrimaryHud(dayText, timerText);
        session.BindAdvanceDayButton(advanceDayButton);
    }

    private static void EnsureTopLayout(RectTransform rect, float yFromTop)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -yFromTop);
        rect.sizeDelta = new Vector2(480f, 36f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
