using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 게임 전체의 BGM과 효과음을 재생한다.
///
/// AudioClip 자체는 AudioCatalog에서 관리하고,
/// 이 클래스는 언제 어떤 클립을 재생할지만 담당한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// 다른 시스템이 필요한 AudioClip을 AudioCatalog에서 가져갈 때 사용한다.
    /// </summary>
    public AudioCatalog Catalog
    {
        get
        {
            EnsureExists();
            if (Instance != null && Instance.catalog == null)
            {
                Instance.EnsureCatalogAssigned(forceRebuild: true);
            }

            return Instance != null ? Instance.catalog : null;
        }
    }

    [Header("Audio Data")]
    [SerializeField] private AudioCatalog catalog;

    [SerializeField, Min(1)] private int sfxSourceCount = 8;

    private AudioSource bgmSource;
    private AudioSource footstepSource;
    private AudioSource machineBreakingSource;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float machineBreakingVolume = 1f;

    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private int nextSfxSourceIndex;
    private float currentBgmEntryVolume = 1f;
    private float currentFootstepEntryVolume = 1f;
    private float currentMachineBreakingEntryVolume = 1f;
    private bool sourcesCreated;

    // 씬 AudioManager가 Missing Script여도 플레이 시 반드시 하나 만든다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static AudioManager EnsureExists()
    {
        if (Instance != null)
        {
            Instance.EnsureCatalogAssigned(forceRebuild: false);
            Instance.EnsureSourcesCreated();
            return Instance;
        }

        AudioManager existing = Object.FindAnyObjectByType<AudioManager>();
        if (existing != null)
        {
            existing.InitializeInstance();
            return Instance;
        }

        var root = new GameObject("AudioManager");
        AudioManager created = root.AddComponent<AudioManager>();
        if (Instance == null)
        {
            created.InitializeInstance();
        }

        Debug.LogWarning("[AudioManager] 씬에 유효한 AudioManager가 없어 런타임에 생성했습니다.");
        return Instance;
    }

    public static string ForceRebuildCatalog()
    {
        AudioManager audio = EnsureExists();
        if (audio == null)
        {
            return "AudioManager 생성 실패";
        }

        audio.EnsureCatalogAssigned(forceRebuild: true);
        audio.EnsureSourcesCreated();
        if (audio.catalog == null)
        {
            return "Catalog 여전히 null (Music 클립 로드 실패?)";
        }

        return $"Catalog OK / clip {CountReadyClips(audio.catalog)}개";
    }

    private void Awake()
    {
        InitializeInstance();
    }

    private void InitializeInstance()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCatalogAssigned(forceRebuild: false);
        EnsureSourcesCreated();
        ApplyVolumes();
    }

    private void EnsureSourcesCreated()
    {
        if (sourcesCreated && sfxSources.Count > 0 && bgmSource != null)
        {
            return;
        }

        CreateSources();
        sourcesCreated = true;
    }

    private void EnsureCatalogAssigned(bool forceRebuild)
    {
        if (!forceRebuild && catalog != null)
        {
            return;
        }

        AudioCatalog loaded = null;

#if UNITY_EDITOR
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioCatalog>(
            "Assets/Scripts/Audio/AudioCatalog.asset");

        if (loaded == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioCatalog");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioCatalog>(path);
                if (loaded != null)
                {
                    break;
                }
            }
        }
#endif

        if (loaded == null)
        {
            loaded = Resources.Load<AudioCatalog>("AudioCatalog");
        }

        if (loaded == null || forceRebuild)
        {
            AudioCatalog built = BuildCatalogFromMusicFolder();
            if (built != null)
            {
                loaded = built;
            }
        }

        catalog = loaded;

        if (catalog == null)
        {
            Debug.LogError(
                "[AudioManager] Catalog 복구 실패. Assets/Music mp3 임포트를 확인하세요.",
                this);
        }
        else
        {
            Debug.LogWarning(
                $"[AudioManager] Catalog 준비됨 (clips={CountReadyClips(catalog)})",
                this);
        }
    }

    private static int CountReadyClips(AudioCatalog source)
    {
        if (source == null)
        {
            return 0;
        }

        int count = 0;
        void Add(AudioCatalog.AudioEntry entry)
        {
            if (entry?.clip != null)
            {
                count++;
            }
        }

        Add(source.uiClick);
        Add(source.uiDeny);
        Add(source.coin);
        Add(source.footstep);
        Add(source.gameOver);
        Add(source.machineBreak);
        Add(source.machineBreaking);
        Add(source.repair);
        Add(source.hammerWhoosh);
        Add(source.metalTap);
        Add(source.placeMachine);
        Add(source.pickupMachine);
        Add(source.phaseStart);
        Add(source.phaseEnd);
        Add(source.questAccept);
        Add(source.zoneUnlock);
        Add(source.prepare);
        Add(source.production);
        return count;
    }

    private static AudioCatalog BuildCatalogFromMusicFolder()
    {
        AudioCatalog built = ScriptableObject.CreateInstance<AudioCatalog>();
        built.uiClick = Entry("ui_click", "Assets/Music/ui_click.mp3");
        built.uiDeny = Entry("ui_deny", "Assets/Music/ui_deny.mp3");
        built.coin = Entry("coin", "Assets/Music/coin.mp3");
        built.footstep = Entry("footstep", "Assets/Music/footstep.mp3", pitch: 1.5f);
        built.gameOver = Entry("game_over", "Assets/Music/game_over.mp3");
        built.machineBreak = Entry("machine_break", "Assets/Music/machine_break.mp3");
        built.machineBreaking = Entry("machine_breaking", "Assets/Music/machine_breaking.mp3");
        built.repair = Entry("repair", "Assets/Music/repair.mp3");
        built.hammerWhoosh = Entry("whoosh", "Assets/Music/whoosh.mp3");
        built.metalTap = Entry("metal_tap", "Assets/Music/metal_tap.mp3");
        built.placeMachine = Entry("place_machine", "Assets/Music/place_machine.mp3");
        built.pickupMachine = Entry("pickup_machine", "Assets/Music/pickup_machine.mp3");
        built.phaseStart = Entry("phase_start", "Assets/Music/phase_start.mp3");
        built.phaseEnd = Entry("phase_end", "Assets/Music/phase_end.mp3");
        built.questAccept = Entry("quest_accept", "Assets/Music/quest_accept.mp3");
        built.zoneUnlock = Entry("zone_unlock", "Assets/Music/zone_unlock.mp3");
        built.prepare = Entry("Prepare", "Assets/Music/Prepare.mp3", volume: 0.4f);
        built.production = Entry("Production", "Assets/Music/Production.mp3", volume: 0.4f);

        if (CountReadyClips(built) == 0)
        {
            Object.Destroy(built);
            Debug.LogError("[AudioManager] Music 폴더에서 AudioClip을 하나도 못 불러왔습니다.");
            return null;
        }

        return built;
    }

    private static AudioCatalog.AudioEntry Entry(
        string resourcesName,
        string assetPath,
        float volume = 1f,
        float pitch = 1f)
    {
        AudioClip clip = null;
#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
#endif
        if (clip == null)
        {
            clip = Resources.Load<AudioClip>(resourcesName);
        }

        if (clip == null)
        {
            clip = Resources.Load<AudioClip>("Music/" + resourcesName);
        }

        return new AudioCatalog.AudioEntry
        {
            clip = clip,
            volume = volume,
            pitch = pitch,
        };
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        EnsureCatalogAssigned(forceRebuild: false);
        EnsureSourcesCreated();
        AttachUiButtonSounds();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachUiButtonSounds();
    }

    private static void AttachUiButtonSounds()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.GetComponent<UiButtonSound>() == null)
            {
                button.gameObject.AddComponent<UiButtonSound>();
            }
        }
    }

    private void CreateSources()
    {
        if (bgmSource == null)
        {
            bgmSource = CreateSource("BGM Source", true);
        }

        if (footstepSource == null)
        {
            footstepSource = CreateSource("Footstep Source", true);
        }

        if (machineBreakingSource == null)
        {
            machineBreakingSource = CreateSource("Machine Breaking Source", true);
        }

        while (sfxSources.Count < sfxSourceCount)
        {
            AudioSource source = CreateSource($"SFX Source {sfxSources.Count + 1}", false);
            sfxSources.Add(source);
        }
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource createdSource = sourceObject.AddComponent<AudioSource>();
        createdSource.playOnAwake = false;
        createdSource.loop = loop;
        return createdSource;
    }

    private AudioSource GetNextSfxSource()
    {
        if (sfxSources.Count == 0)
        {
            return null;
        }

        AudioSource source = sfxSources[nextSfxSourceIndex];
        nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Count;
        return source;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume * bgmVolume * currentBgmEntryVolume;
        }

        if (footstepSource != null)
        {
            footstepSource.volume = masterVolume * sfxVolume * footstepVolume
                * currentFootstepEntryVolume;
        }

        if (machineBreakingSource != null)
        {
            machineBreakingSource.volume = masterVolume * sfxVolume * machineBreakingVolume
                * currentMachineBreakingEntryVolume;
        }

        foreach (AudioSource source in sfxSources)
        {
            if (source != null)
            {
                source.volume = masterVolume * sfxVolume;
            }
        }
    }

    public void PlayBgm(AudioClip clip)
    {
        PlayBgmInternal(clip, 1f, 1f);
    }

    public void PlayBgm(AudioCatalog.AudioEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        PlayBgmInternal(entry.clip, entry.volume, entry.pitch);
    }

    private void PlayBgmInternal(AudioClip clip, float entryVolume, float pitch)
    {
        EnsureSourcesCreated();
        if (bgmSource == null || clip == null)
        {
            return;
        }

        currentBgmEntryVolume = Mathf.Clamp01(entryVolume);
        bgmSource.volume = masterVolume * bgmVolume * currentBgmEntryVolume;
        bgmSource.pitch = Mathf.Clamp(pitch, 0.25f, 3f);

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            currentBgmEntryVolume = 1f;
        }
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        PlaySfxInternal(clip, volumeScale, 1f);
    }

    public void PlaySfx(AudioCatalog.AudioEntry entry, float volumeScale = 1f)
    {
        if (entry == null)
        {
            return;
        }

        PlaySfxInternal(entry.clip, entry.volume * volumeScale, entry.pitch);
    }

    public float GetPlaybackDuration(AudioCatalog.AudioEntry entry)
    {
        if (entry == null || entry.clip == null)
        {
            return 0f;
        }

        return entry.clip.length / Mathf.Clamp(entry.pitch, 0.25f, 3f);
    }

    private void PlaySfxInternal(AudioClip clip, float volumeScale, float pitch)
    {
        EnsureSourcesCreated();
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetNextSfxSource();
        if (source == null)
        {
            return;
        }

        source.volume = masterVolume * sfxVolume;
        source.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void StartFootsteps()
    {
        EnsureSourcesCreated();
        AudioCatalog.AudioEntry entry = catalog != null ? catalog.footstep : null;
        if (footstepSource == null || entry == null || entry.clip == null)
        {
            return;
        }

        currentFootstepEntryVolume = Mathf.Clamp01(entry.volume);
        footstepSource.volume = masterVolume * sfxVolume * footstepVolume
            * currentFootstepEntryVolume;
        footstepSource.pitch = Mathf.Clamp(entry.pitch, 0.25f, 3f);

        if (footstepSource.isPlaying && footstepSource.clip == entry.clip)
        {
            return;
        }

        footstepSource.Stop();
        footstepSource.clip = entry.clip;
        footstepSource.loop = true;
        footstepSource.Play();
    }

    public void StopFootsteps()
    {
        if (footstepSource != null)
        {
            footstepSource.Stop();
            footstepSource.clip = null;
            currentFootstepEntryVolume = 1f;
        }
    }

    public void StartMachineBreaking()
    {
        EnsureSourcesCreated();
        AudioCatalog.AudioEntry entry = catalog != null ? catalog.machineBreaking : null;
        if (machineBreakingSource == null || entry == null || entry.clip == null)
        {
            return;
        }

        currentMachineBreakingEntryVolume = Mathf.Clamp01(entry.volume);
        machineBreakingSource.volume = masterVolume * sfxVolume * machineBreakingVolume
            * currentMachineBreakingEntryVolume;
        machineBreakingSource.pitch = Mathf.Clamp(entry.pitch, 0.25f, 3f);

        if (machineBreakingSource.isPlaying && machineBreakingSource.clip == entry.clip)
        {
            return;
        }

        machineBreakingSource.Stop();
        machineBreakingSource.clip = entry.clip;
        machineBreakingSource.loop = true;
        machineBreakingSource.Play();
    }

    public void StopMachineBreaking()
    {
        if (machineBreakingSource != null)
        {
            machineBreakingSource.Stop();
            machineBreakingSource.clip = null;
            currentMachineBreakingEntryVolume = 1f;
        }
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
    }
}
