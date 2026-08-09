#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 한글 UI용 NanumGothic Dynamic SDF를 만들고 LiberationSans / TMP Settings fallback에 연결한다.
public static class KoreanTmpFontSetup
{
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/NanumGothic.ttf";
    private const string KoreanFontAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothic SDF.asset";
    private const string LiberationSansPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string LiberationSansFallbackPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
    private const string SessionKey = "DungeonFront.KoreanTmpFontSetup.Done";

    [MenuItem("DungeonFront/Setup Korean TMP Font")]
    public static void SetupFromMenu()
    {
        Setup(force: true);
    }

    [InitializeOnLoadMethod]
    private static void SetupOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath) != null)
            {
                // 이미 만들어져 있으면 fallback만 다시 연결
                Setup(force: false);
                SessionState.SetBool(SessionKey, true);
                return;
            }

            Setup(force: true);
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath) != null)
            {
                SessionState.SetBool(SessionKey, true);
            }
        };
    }

    public static void Setup(bool force)
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "TextMesh Pro/Fonts/NanumGothic.ttf")))
        {
            Debug.LogWarning("[KoreanTmpFontSetup] NanumGothic.ttf가 없습니다: " + SourceFontPath);
            return;
        }

        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (koreanFont == null || force)
        {
            koreanFont = CreateOrReplaceKoreanFontAsset();
        }

        if (koreanFont == null)
        {
            Debug.LogError("[KoreanTmpFontSetup] NanumGothic SDF 생성 실패");
            return;
        }

        WireFallback(koreanFont);
        AssetDatabase.SaveAssets();
        Debug.Log("[KoreanTmpFontSetup] NanumGothic SDF fallback 연결 완료");
    }

    private static TMP_FontAsset CreateOrReplaceKoreanFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            AssetDatabase.ImportAsset(SourceFontPath);
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        }

        if (sourceFont == null)
        {
            return null;
        }

        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(KoreanFontAssetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            return null;
        }

        fontAsset.name = "NanumGothic SDF";
        AssetDatabase.CreateAsset(fontAsset, KoreanFontAssetPath);

        // CreateFontAsset가 만든 material/atlas를 같은 에셋에 넣는다.
        if (fontAsset.material != null)
        {
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                if (fontAsset.atlasTextures[i] != null)
                {
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                }
            }
        }

        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }

    private static void WireFallback(TMP_FontAsset koreanFont)
    {
        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
        if (liberation != null)
        {
            EnsureFallback(liberation, koreanFont);
            EditorUtility.SetDirty(liberation);
        }

        // 경고에 나온 Fallback 폰트에도 한글을 연결한다.
        TMP_FontAsset liberationFallback =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansFallbackPath);
        if (liberationFallback != null)
        {
            EnsureFallback(liberationFallback, koreanFont);
            EditorUtility.SetDirty(liberationFallback);
        }

        // TMP Settings 기본 fallback 목록에도 추가
        if (TMP_Settings.instance != null)
        {
            SerializedObject settings = new SerializedObject(TMP_Settings.instance);
            SerializedProperty fallbacks = settings.FindProperty("m_fallbackFontAssets");
            if (fallbacks != null && fallbacks.isArray)
            {
                bool found = false;
                for (int i = 0; i < fallbacks.arraySize; i++)
                {
                    Object obj = fallbacks.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (obj == koreanFont)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    fallbacks.arraySize++;
                    fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = koreanFont;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }

    private static void EnsureFallback(TMP_FontAsset primary, TMP_FontAsset fallback)
    {
        if (primary.fallbackFontAssetTable == null)
        {
            primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
        }

        if (!primary.fallbackFontAssetTable.Contains(fallback))
        {
            primary.fallbackFontAssetTable.Add(fallback);
        }
    }
}
#endif
