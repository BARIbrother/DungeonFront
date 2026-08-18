using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class GameOverOverlay : MonoBehaviour
{
    private static GameOverOverlay instance;
    private GameObject panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null) new GameObject(nameof(GameOverOverlay)).AddComponent<GameOverOverlay>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }

    private void Update()
    {
        GameOverController controller = FindAnyObjectByType<GameOverController>();
        bool shouldShow = controller != null && controller.IsGameOver;
        if (panel != null && panel.activeSelf != shouldShow) panel.SetActive(shouldShow);
    }

    private void Restart()
    {
        GameSessionState.Instance?.NewGame();
        FindAnyObjectByType<GameOverController>()?.ResetGameOver();
        panel.SetActive(false);
    }

    private void Build()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)));

        GameObject canvasObject = new("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        panel = Create("Panel", canvasObject.transform, new Color(0f, 0f, 0f, .85f));
        Stretch(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        TMP_Text title = Label("Title", panel.transform, "필수 의뢰를 마감일 안에 완료하지 못했습니다.", 40);
        Stretch(title.rectTransform, new Vector2(.2f, .57f), new Vector2(.8f, .72f));
        Button retry = MakeButton("Retry", panel.transform, "새 게임 시작");
        Stretch(retry.GetComponent<RectTransform>(), new Vector2(.4f, .39f), new Vector2(.6f, .47f));
        retry.onClick.AddListener(Restart);
        panel.SetActive(false);
    }

    private static GameObject Create(string name, Transform parent, Color color)
    {
        GameObject o = new(name, typeof(RectTransform), typeof(Image));
        o.transform.SetParent(parent, false);
        o.GetComponent<Image>().color = color;
        return o;
    }

    private static TMP_Text Label(string name, Transform parent, string value, float size)
    {
        GameObject o = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        o.transform.SetParent(parent, false);
        TMP_Text text = o.GetComponent<TextMeshProUGUI>();
        text.font = KoreanTmpFontRuntimeFix.EnsureFont() ?? TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static Button MakeButton(string name, Transform parent, string value)
    {
        GameObject o = Create(name, parent, new Color(.3f, .46f, .75f));
        Button button = o.AddComponent<Button>();
        TMP_Text text = Label("Label", o.transform, value, 24);
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
}
