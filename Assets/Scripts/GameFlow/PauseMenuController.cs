using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>ESC 메뉴와 W5 설정(해상도, 창 모드, 전체 음량)을 제공한다.</summary>
public sealed class PauseMenuController : MonoBehaviour
{
    private const string PauseRequester = "PauseMenu";
    private const string VolumeKey = "Settings.MasterVolume";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionKey = "Settings.ResolutionIndex";

    private static PauseMenuController instance;
    private readonly List<Resolution> resolutions = new();
    private GameObject modal;
    private GameObject settingsGroup;
    private Button resolutionButton;
    private TMP_Text resolutionButtonText;
    private Toggle fullscreenToggle;
    private Slider volumeSlider;
    private bool menuVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null)
        {
            new GameObject("PauseMenuController").AddComponent<PauseMenuController>();
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
        LoadSettings();
    }

    private void OnDisable() => GamePauseService.ReleasePause(PauseRequester);

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        // 대화처럼 다른 모달이 멈춘 상태에서는 그 모달이 입력을 처리한다.
        if (!menuVisible && GamePauseService.IsPaused)
        {
            return;
        }

        SetMenuVisible(!menuVisible);
    }

    public void Resume() => SetMenuVisible(false);

    private void SetMenuVisible(bool visible)
    {
        menuVisible = visible;
        modal.SetActive(visible);
        if (visible)
        {
            GamePauseService.RequestPause(PauseRequester);
        }
        else
        {
            GamePauseService.ReleasePause(PauseRequester);
        }
    }

    private void LoadSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);

        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        Screen.fullScreen = fullscreen;

        int index = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, FindCurrentResolutionIndex()), 0, resolutions.Count - 1);
        UpdateResolutionButtonLabel(index);
    }

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count)
        {
            return;
        }

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionKey, index);
        PlayerPrefs.Save();
        UpdateResolutionButtonLabel(index);
    }

    private void CycleResolution()
    {
        int current = PlayerPrefs.GetInt(ResolutionKey, FindCurrentResolutionIndex());
        ApplyResolution((current + 1) % resolutions.Count);
    }

    private void ApplyFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void CreateUi()
    {
        EnsureEventSystem();
        var canvasObject = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1900;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        modal = Panel("PauseModal", canvasObject.transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(modal.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject window = Panel("PauseWindow", modal.transform, new Color(0.1f, 0.12f, 0.19f, 1f));
        Stretch(window.GetComponent<RectTransform>(), new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.8f));

        TMP_Text title = Text("Title", window.transform, "일시정지", 42, TextAlignmentOptions.Center);
        Stretch(title.rectTransform, new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.96f));
        Button resume = Button("Resume", window.transform, "계속하기");
        Stretch(resume.GetComponent<RectTransform>(), new Vector2(0.14f, 0.68f), new Vector2(0.86f, 0.79f));
        resume.onClick.AddListener(Resume);

        Button save = Button("Save", window.transform, "저장하기");
        Stretch(save.GetComponent<RectTransform>(), new Vector2(0.14f, 0.53f), new Vector2(0.49f, 0.64f));
        save.onClick.AddListener(() => GameSaveService.Save(0));
        Button load = Button("Load", window.transform, "불러오기");
        Stretch(load.GetComponent<RectTransform>(), new Vector2(0.51f, 0.53f), new Vector2(0.86f, 0.64f));
        load.onClick.AddListener(() =>
        {
            if (GameSaveService.Load(0))
            {
                Resume();
            }
        });
        Button economy = Button("Economy", window.transform, "상점 / 해금");
        Stretch(economy.GetComponent<RectTransform>(), new Vector2(0.14f, 0.41f), new Vector2(0.86f, 0.51f));
        economy.onClick.AddListener(() =>
        {
            SetMenuVisible(false);
            EconomyHubUI.Show();
        });

        settingsGroup = new GameObject("Settings", typeof(RectTransform));
        settingsGroup.transform.SetParent(window.transform, false);
        Stretch(settingsGroup.GetComponent<RectTransform>(), new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.38f));
        CreateSettingsUi(settingsGroup.transform);
        modal.SetActive(false);
    }

    private void CreateSettingsUi(Transform parent)
    {
        TMP_Text settingsTitle = Text("SettingsTitle", parent, "설정", 30, TextAlignmentOptions.Left);
        Stretch(settingsTitle.rectTransform, new Vector2(0f, 0.8f), new Vector2(1f, 1f));

        TMP_Text resolutionLabel = Text("ResolutionLabel", parent, "해상도", 23, TextAlignmentOptions.Left);
        Stretch(resolutionLabel.rectTransform, new Vector2(0f, 0.58f), new Vector2(0.34f, 0.76f));
        resolutionButton = Button("ResolutionButton", parent, "");
        resolutionButtonText = resolutionButton.GetComponentInChildren<TMP_Text>();
        Stretch(resolutionButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.57f), new Vector2(1f, 0.76f));
        BuildResolutionOptions();
        resolutionButton.onClick.AddListener(CycleResolution);

        TMP_Text fullscreenLabel = Text("FullscreenLabel", parent, "전체 화면", 23, TextAlignmentOptions.Left);
        Stretch(fullscreenLabel.rectTransform, new Vector2(0f, 0.34f), new Vector2(0.5f, 0.52f));
        fullscreenToggle = CreateToggle(parent, "전체 화면 사용");
        Stretch(fullscreenToggle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.34f), new Vector2(1f, 0.52f));
        fullscreenToggle.onValueChanged.AddListener(ApplyFullscreen);

        TMP_Text volumeLabel = Text("VolumeLabel", parent, "전체 음량", 23, TextAlignmentOptions.Left);
        Stretch(volumeLabel.rectTransform, new Vector2(0f, 0.1f), new Vector2(0.34f, 0.28f));
        volumeSlider = CreateSlider(parent);
        Stretch(volumeSlider.GetComponent<RectTransform>(), new Vector2(0.35f, 0.1f), new Vector2(1f, 0.28f));
        volumeSlider.onValueChanged.AddListener(ApplyVolume);
    }

    private void BuildResolutionOptions()
    {
        foreach (Resolution resolution in Screen.resolutions)
        {
            if (!resolutions.Exists(candidate => candidate.width == resolution.width && candidate.height == resolution.height))
            {
                resolutions.Add(resolution);
            }
        }

        if (resolutions.Count == 0)
        {
            resolutions.Add(Screen.currentResolution);
        }

        UpdateResolutionButtonLabel(FindCurrentResolutionIndex());
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                return i;
            }
        }
        return 0;
    }

    private static GameObject Panel(string name, Transform parent, Color color)
    {
        var result = new GameObject(name, typeof(RectTransform), typeof(Image));
        result.transform.SetParent(parent, false);
        result.GetComponent<Image>().color = color;
        return result;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        var result = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        result.transform.SetParent(parent, false);
        TMP_Text text = result.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        return text;
    }

    private static Button Button(string name, Transform parent, string label)
    {
        GameObject result = Panel(name, parent, new Color(0.24f, 0.44f, 0.7f));
        Button button = result.AddComponent<Button>();
        TMP_Text text = Text("Label", result.transform, label, 26, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private void UpdateResolutionButtonLabel(int index)
    {
        if (resolutionButtonText == null || index < 0 || index >= resolutions.Count)
        {
            return;
        }

        Resolution resolution = resolutions[index];
        resolutionButtonText.text = $"{resolution.width} x {resolution.height}  (클릭하여 변경)";
    }

    private static Toggle CreateToggle(Transform parent, string label)
    {
        GameObject root = Panel("FullscreenToggle", parent, new Color(0.2f, 0.25f, 0.34f));
        Toggle toggle = root.AddComponent<Toggle>();
        TMP_Text text = Text("Label", root.transform, label, 20, TextAlignmentOptions.Left);
        Stretch(text.rectTransform, new Vector2(.08f,0), new Vector2(.72f,1));
        GameObject check = Panel("Checkmark", root.transform, new Color(.35f,.85f,.5f));
        Stretch(check.GetComponent<RectTransform>(), new Vector2(.78f,.2f), new Vector2(.94f,.8f));
        toggle.targetGraphic = root.GetComponent<Image>();
        toggle.graphic = check.GetComponent<Image>();
        return toggle;
    }

    private static Slider CreateSlider(Transform parent)
    {
        GameObject root = Panel("VolumeSlider", parent, new Color(0.2f, 0.25f, 0.34f));
        Slider slider = root.AddComponent<Slider>();
        GameObject fill = Panel("Fill", root.transform, new Color(.28f,.65f,.9f));
        Stretch(fill.GetComponent<RectTransform>(), new Vector2(0,.25f), new Vector2(1,.75f));
        GameObject handle = Panel("Handle", root.transform, new Color(.95f,.95f,1f));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22,22);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        return slider;
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
}
