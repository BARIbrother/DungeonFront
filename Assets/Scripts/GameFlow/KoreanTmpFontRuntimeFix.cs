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

        // 에디터 설치기가 생성한 프로젝트 포함 폰트를 최우선으로 사용한다.
        // 이 에셋은 빌드에도 포함되므로 다른 PC에서도 한글이 깨지지 않는다.
        SharedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR SDF");
        if (SharedFont != null)
        {
            Debug.Log("[KoreanFont] 프로젝트 Noto Sans KR TMP 폰트를 적용했습니다.");
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

        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text == null || text.font == SharedFont) continue;
            text.font = SharedFont;
            text.SetAllDirty();
        }
    }
}
