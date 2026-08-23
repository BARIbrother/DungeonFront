using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 설치된 한글 OS 폰트로 동적 TMP 폰트를 만들고 모든 런타임 UI에 적용합니다.
/// 프로젝트에 유료 폰트 파일을 복사하지 않으면서 한글 글리프 누락을 방지합니다.
/// </summary>
public sealed class KoreanTmpFontRuntimeFix : MonoBehaviour
{
    private static readonly string[] PreferredFonts =
    {
        "Noto Sans KR",
        "맑은 고딕",
        "Malgun Gothic",
        "Arial Unicode MS"
    };

    private static KoreanTmpFontRuntimeFix instance;
    public static TMP_FontAsset SharedFont { get; private set; }

    private float nextRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureFont();
        if (instance == null)
        {
            new GameObject(nameof(KoreanTmpFontRuntimeFix)).AddComponent<KoreanTmpFontRuntimeFix>();
        }
    }

    public static TMP_FontAsset EnsureFont()
    {
        if (SharedFont != null) return SharedFont;

        // NotoSansKR SDF는 VF Thin + 두꺼운 outline에서 글리프 텍스처가 깨지기 쉽다.
        // 테크트리·양피지 UI는 정적 NanumGothic을 우선한다.
        SharedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NanumGothic SDF");
        if (SharedFont != null)
        {
            Debug.Log("[KoreanFont] 프로젝트 NanumGothic TMP 폰트를 적용했습니다.");
            return SharedFont;
        }

        SharedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR SDF");
        if (SharedFont != null)
        {
            Debug.Log("[KoreanFont] NanumGothic이 없어 Noto Sans KR TMP 폰트를 적용했습니다.");
            return SharedFont;
        }

        string[] installed = Font.GetOSInstalledFontNames();
        foreach (string preferred in PreferredFonts)
        {
            string installedName = Array.Find(installed, name => string.Equals(name, preferred, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(installedName)) continue;

            Font source = Font.CreateDynamicFontFromOSFont(installedName, 48);
            if (source == null) continue;

            SharedFont = TMP_FontAsset.CreateFontAsset(
                source,
                48,
                6,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            if (SharedFont != null)
            {
                SharedFont.name = $"Runtime Korean - {installedName}";
                SharedFont.TryAddCharacters("가나다라마바사아자차카타파하한글의뢰인마감오늘보상요구재료보유필요수락납품없음알수", out _);
                Debug.LogWarning($"[KoreanFont] 프로젝트 폰트가 아직 생성되지 않아 OS 폰트 '{installedName}'를 임시 사용 중입니다.");
                return SharedFont;
            }
        }

        Debug.LogError("[KoreanFont] 설치된 한글 폰트를 찾지 못했습니다. Noto Sans KR 또는 맑은 고딕 설치 상태를 확인하세요.");
        return null;
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
        EnsureFont();
        UiEventSystem.Ensure();
        TmpUiCanvas.ConfigureAll();
        ApplyToAllText();
    }

    private void Update()
    {
        if (SharedFont == null || Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + 0.25f;
        ApplyToAllText();
    }

    private static void ApplyToAllText()
    {
        if (SharedFont == null) return;

        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text == null)
            {
                continue;
            }

            TmpUiCanvas.Sharpen(text);
            TmpUiCanvas.Configure(text.canvas);
            if (text.font == SharedFont)
            {
                continue;
            }

            text.font = SharedFont;
            text.SetAllDirty();
        }
    }
}
