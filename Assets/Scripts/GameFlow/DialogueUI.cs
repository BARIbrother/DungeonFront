using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// StoryEventBus 이벤트를 실제 대사 창으로 재생한다. 초상 에셋이 없을 때도 화자 이니셜을 표시하는
/// placeholder를 사용하므로 이벤트와 대화 흐름은 항상 검증할 수 있다.
/// </summary>
public sealed class DialogueUI : MonoBehaviour
{
    private const string PauseRequester = "DialogueUI";

    private static DialogueUI instance;
    public static event Action<string> OnDialogueClosed;
    private readonly Queue<string> pendingEventIds = new();
    private readonly Dictionary<string, DialogueLine[]> linesByEvent = new()
    {
        ["001E00001"] = new[]
        {
            new DialogueLine("???", "여보세요, 여보세요? ...이봐요!", "unknown"),
            new DialogueLine("주인공", "하암... 누구세요?", "protagonist"),
            new DialogueLine("이브", "대장장이 협회 감사관 이브입니다. 이 대장간의 주인이신가요?", "eve"),
            new DialogueLine("주인공", "전 주인 친척이에요. 어릴 때부터 대장장이가 되는 게 꿈이었거든요.", "protagonist"),
            new DialogueLine("이브", "운영에는 대장장이 면허가 필요합니다. 우선 실기 시험으로 첫 의뢰를 받아 보시죠.", "eve"),
        },
        ["001E00002"] = new[]
        {
            new DialogueLine("이브", "첫 의뢰는 준비 단계에서 받을 수 있습니다. 의뢰를 수락하고 필요한 물품을 생산하세요.", "eve"),
            new DialogueLine("이브", "생산이 끝나면 결산 단계에서 물품을 한 번에 납품하면 됩니다.", "eve"),
        },
        ["001E00004"] = new[]
        {
            new DialogueLine("주인공", "생산이 시작됐어요. 기계와 운반 흐름을 확인해 볼까요?", "protagonist"),
        },
        ["001E00005"] = new[]
        {
            new DialogueLine("이브", "결산 단계입니다. 의뢰 카드의 보유/요구 수량을 확인한 뒤 납품하세요.", "eve"),
        },
        ["001E00006"] = new[]
        {
            new DialogueLine("레이", "마법 기술은 명성이 쌓이면 사용할 수 있습니다. 해금 조건을 확인해 보세요.", "ray"),
        },
    };

    private GameObject modal;
    private TMP_Text speakerText;
    private TMP_Text bodyText;
    private TMP_Text portraitInitialText;
    private TMP_Text nextText;
    private Button nextButton;
    private DialogueLine[] activeLines;
    private string activeEventId;
    private int lineIndex;

    public bool IsShowing => activeLines != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        var root = new GameObject("DialogueUI");
        root.AddComponent<DialogueUI>();
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

    private void OnEnable() => StoryEventBus.OnStoryEvent += HandleStoryEvent;

    private void OnDisable()
    {
        StoryEventBus.OnStoryEvent -= HandleStoryEvent;
        GamePauseService.ReleasePause(PauseRequester);
    }

    private void Update()
    {
        if (!IsShowing || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame
            || Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            Advance();
        }
    }

    private void HandleStoryEvent(string eventId)
    {
        if (!linesByEvent.ContainsKey(eventId))
        {
            return;
        }

        pendingEventIds.Enqueue(eventId);
        if (!IsShowing)
        {
            ShowNextPending();
        }
    }

    private void ShowNextPending()
    {
        if (pendingEventIds.Count == 0)
        {
            return;
        }

        activeEventId = pendingEventIds.Dequeue();
        activeLines = linesByEvent[activeEventId];
        lineIndex = 0;
        modal.SetActive(true);
        GamePauseService.RequestPause(PauseRequester);
        ShowLine();
    }

    private void Advance()
    {
        if (!IsShowing)
        {
            return;
        }

        lineIndex++;
        if (lineIndex < activeLines.Length)
        {
            ShowLine();
            return;
        }

        string completedEvent = activeEventId;
        activeEventId = null;
        activeLines = null;
        modal.SetActive(false);
        GamePauseService.ReleasePause(PauseRequester);
        OnDialogueClosed?.Invoke(completedEvent);
        FactoryStoryHooks.NotifyDialogueClosed(completedEvent);
        ShowNextPending();
    }

    private void ShowLine()
    {
        DialogueLine line = activeLines[lineIndex];
        speakerText.text = line.speaker;
        bodyText.text = line.body;
        portraitInitialText.text = string.IsNullOrEmpty(line.characterId)
            ? "?"
            : line.characterId.Substring(0, 1).ToUpperInvariant();
        nextText.text = lineIndex == activeLines.Length - 1 ? "닫기" : "다음";
    }

    private void CreateUi()
    {
        EnsureEventSystem();
        var canvasObject = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        modal = CreatePanel("Modal", canvasObject.transform, new Color(0f, 0f, 0f, 0.62f));
        Stretch(modal.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject box = CreatePanel("DialogueBox", modal.transform, new Color(0.08f, 0.1f, 0.16f, 0.98f));
        Stretch(box.GetComponent<RectTransform>(), new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.31f), Vector2.zero, Vector2.zero);

        GameObject portrait = CreatePanel("PortraitPlaceholder", box.transform, new Color(0.26f, 0.39f, 0.57f, 1f));
        Stretch(portrait.GetComponent<RectTransform>(), new Vector2(0.025f, 0.13f), new Vector2(0.18f, 0.87f), Vector2.zero, Vector2.zero);
        portraitInitialText = CreateText("Initial", portrait.transform, 72, TextAlignmentOptions.Center, Color.white);
        Stretch(portraitInitialText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        speakerText = CreateText("Speaker", box.transform, 34, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.38f));
        Stretch(speakerText.rectTransform, new Vector2(0.22f, 0.68f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero);
        bodyText = CreateText("Body", box.transform, 29, TextAlignmentOptions.TopLeft, Color.white);
        bodyText.enableWordWrapping = true;
        Stretch(bodyText.rectTransform, new Vector2(0.22f, 0.17f), new Vector2(0.94f, 0.68f), Vector2.zero, Vector2.zero);

        nextButton = CreateButton("NextButton", box.transform, out nextText, "다음");
        Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(0.81f, 0.02f), new Vector2(0.95f, 0.15f), Vector2.zero, Vector2.zero);
        nextButton.onClick.AddListener(Advance);
        modal.SetActive(false);
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, out TMP_Text label, string labelText)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.24f, 0.44f, 0.7f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        label = CreateText("Label", buttonObject.transform, 24, TextAlignmentOptions.Center, Color.white);
        label.text = labelText;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private readonly struct DialogueLine
    {
        public readonly string speaker;
        public readonly string body;
        public readonly string characterId;

        public DialogueLine(string speaker, string body, string characterId)
        {
            this.speaker = speaker;
            this.body = body;
            this.characterId = characterId;
        }
    }
}
