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
    /// 예: AudioManager.Instance.PlaySfx(AudioManager.Instance.Catalog.coin);
    /// </summary>
    public AudioCatalog Catalog => catalog;
    [Header("Audio Data")]
    [SerializeField] private AudioCatalog catalog;

    [SerializeField, Min(1)] private int sfxSourceCount = 8;

    // 재생기(AudioSource)는 AudioCatalog에 넣는 데이터가 아니다.
    // AudioManager가 실행될 때 내부적으로 생성하며 Inspector에서 할당할 필요가 없다.
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateSources();
        ApplyVolumes();
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
        AttachUiButtonSounds();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachUiButtonSounds();
    }

    private static void AttachUiButtonSounds()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.GetComponent<UiButtonSound>() == null)
            {
                button.gameObject.AddComponent<UiButtonSound>();
            }
        }
    }

    /// <summary>
    /// BGM과 반복 효과음에 사용할 AudioSource들을 자동으로 만든다.
    /// 이들은 재생 장치이고, AudioClip은 AudioCatalog에서 가져온다.
    /// </summary>
    private void CreateSources()
    {
        bgmSource = CreateSource("BGM Source", true);
        footstepSource = CreateSource("Footstep Source", true);
        machineBreakingSource = CreateSource("Machine Breaking Source", true);

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

    // ---------------------------------------------------------------------
    // BGM
    // ---------------------------------------------------------------------

    /// <summary>
    /// BGM을 반복 재생한다. 같은 곡이면 재시작하지 않는다.
    /// </summary>
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

    // ---------------------------------------------------------------------
    // One-shot SFX
    // ---------------------------------------------------------------------

    /// <summary>
    /// 짧은 효과음을 재생한다.
    /// Play()가 아니라 PlayOneShot()을 사용하므로 다른 효과음을 끊지 않는다.
    /// </summary>
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

    /// <summary>카탈로그 효과음이 현재 피치 기준으로 재생되는 시간입니다.</summary>
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

    // ---------------------------------------------------------------------
    // Looping SFX
    // ---------------------------------------------------------------------

    /// <summary>
    /// 플레이어가 움직이는 동안 발소리를 반복 재생한다.
    /// PlayerMovement에서 WASD 이동 상태가 바뀔 때 호출한다.
    /// </summary>
    public void StartFootsteps()
    {
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

    /// <summary>
    /// 기계가 고장난 상태에서 나는 지속음을 시작한다.
    /// 한 번만 호출해도 중복으로 겹쳐 재생되지 않는다.
    /// </summary>
    public void StartMachineBreaking()
    {
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

    // ---------------------------------------------------------------------
    // Runtime volume control
    // ---------------------------------------------------------------------

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
