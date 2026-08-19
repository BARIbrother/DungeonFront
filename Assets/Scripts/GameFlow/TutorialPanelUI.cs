using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>첫 플레이의 핵심 3단계만 안내하는 튜토리얼. 대화 이후에 재생되며 진행 중 게임을 멈춘다.</summary>
public sealed class TutorialPanelUI : MonoBehaviour
{
    private const string PauseRequester = "TutorialPanel";
    private const string CompletionKey = "Tutorial.Completed";
    private static TutorialPanelUI instance;

    private readonly TutorialStep[] steps =
    {
        new("첫 의뢰 확인", "준비 단계의 의뢰 목록에서 원하는 의뢰를 확인하세요.", "AvailableQuestPanel"),
        new("의뢰 수락", "의뢰 카드를 선택해 수락하세요. 동시에 최대 3개까지 진행할 수 있습니다.", "AvailableQuestPanel"),
        new("생산 시작", "기계를 배치한 뒤 생산 시작 버튼을 누르세요. 결산에서 물품을 한 번에 납품합니다.", "StartProductionButton"),
    };

    private GameObject modal;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private TMP_Text stepText;
    private Button nextButton;
    private int index;
    private bool showing;
    private bool skipConfirmPending;

    public static bool IsOpen => instance != null && instance.showing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            new GameObject("TutorialPanelUI").AddComponent<TutorialPanelUI>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateUi();
    }

    private void OnEnable() => DialogueUI.OnDialogueClosed += HandleDialogueClosed;

    private void OnDisable()
    {
        DialogueUI.OnDialogueClosed -= HandleDialogueClosed;
        GamePauseService.ReleasePause(PauseRequester);
    }

    private void HandleDialogueClosed(string eventId)
    {
        // E1 뒤에는 E2가 자동으로 이어지므로, 첫 의뢰 설명까지 끝난 시점에 안내를 시작한다.
        if (eventId == "001E00002" && PlayerPrefs.GetInt(CompletionKey, 0) == 0)
        {
            Show();
        }
    }

    public void Show()
    {
        if (showing || GameSessionState.Instance == null || GameSessionState.Instance.day != 1)
        {
            return;
        }
        index = 0;
        showing = true;
        modal.SetActive(true);
        GamePauseService.RequestPause(PauseRequester);
        Refresh();
    }

    private void Update()
    {
        if (showing && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Skip();
        }
    }

    public void Advance()
    {
        if (!showing)
        {
            return;
        }

        index++;
        if (index < steps.Length)
        {
            Refresh();
            return;
        }
        Complete();
    }

    public void Skip()
    {
        if (!skipConfirmPending)
        {
            skipConfirmPending = true;
            bodyText.text = "튜토리얼을 건너뛸까요? 다시 누르면 건너뜁니다.";
            return;
        }
        Complete();
    }

    private void Complete()
    {
        showing = false;
        skipConfirmPending = false;
        modal.SetActive(false);
        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
        GamePauseService.ReleasePause(PauseRequester);
    }

    private void Refresh()
    {
        skipConfirmPending = false;
        TutorialStep step = steps[index];
        titleText.text = step.title;
        bodyText.text = step.body + "\n\n강조 대상: " + step.targetName;
        stepText.text = $"튜토리얼 {index + 1}/{steps.Length}";
        TMP_Text label = nextButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = index == steps.Length - 1 ? "시작하기" : "다음";
        }
    }

    private void CreateUi()
    {
        EnsureEventSystem();
        var canvasObject = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        modal = Panel("TutorialModal", canvasObject.transform, new Color(0f, 0f, 0f, 0.7f));
        Stretch(modal.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject box = Panel("TutorialBox", modal.transform, new Color(0.08f, 0.12f, 0.2f, 1f));
        Stretch(box.GetComponent<RectTransform>(), new Vector2(0.28f, 0.31f), new Vector2(0.72f, 0.69f));

        stepText = Text("Step", box.transform, 21, TextAlignmentOptions.Center, new Color(0.55f, 0.82f, 1f));
        Stretch(stepText.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.95f));
        titleText = Text("Title", box.transform, 37, TextAlignmentOptions.Center, Color.white);
        Stretch(titleText.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.82f));
        bodyText = Text("Body", box.transform, 25, TextAlignmentOptions.Center, Color.white);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        Stretch(bodyText.rectTransform, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.62f));
        nextButton = Button("Next", box.transform, "다음");
        Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.07f), new Vector2(0.86f, 0.22f));
        nextButton.onClick.AddListener(Advance);
        Button skip = Button("Skip", box.transform, "건너뛰기");
        Stretch(skip.GetComponent<RectTransform>(), new Vector2(0.14f, 0.07f), new Vector2(0.46f, 0.22f));
        skip.onClick.AddListener(Skip);
        modal.SetActive(false);
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text Text(string name, Transform parent, float size, TextAlignmentOptions alignment, Color color)
    {
        var result = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        result.transform.SetParent(parent, false);
        TMP_Text text = result.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button Button(string name, Transform parent, string label)
    {
        GameObject result = Panel(name, parent, new Color(0.24f, 0.44f, 0.7f));
        Button button = result.AddComponent<Button>();
        TMP_Text text = Text("Label", result.transform, 23, TextAlignmentOptions.Center, Color.white);
        text.text = label;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)));
        }
    }

    private readonly struct TutorialStep
    {
        public readonly string title;
        public readonly string body;
        public readonly string targetName;

        public TutorialStep(string title, string body, string targetName)
        {
            this.title = title;
            this.body = body;
            this.targetName = targetName;
        }
    }
}
