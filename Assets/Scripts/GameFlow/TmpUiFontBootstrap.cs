using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 씬이 열린 직후 기존 TMP UI에도 한글 폰트를 즉시 연결합니다.
public static class TmpUiFontBootstrap
{
    private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AssignUiFonts()
    {
        TMP_FontAsset font = KoreanTmpFontRuntimeFix.EnsureFont() ?? ResolveDefaultFont();
        if (font == null)
        {
            Debug.LogWarning("[TmpUiFontBootstrap] 사용할 TMP 폰트를 찾지 못했습니다.");
            return;
        }

        foreach (TextMeshProUGUI text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text == null) continue;
            text.font = font;
            text.SetAllDirty();
        }
    }

    private static TMP_FontAsset ResolveDefaultFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;

        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) return font;

#if UNITY_EDITOR
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null) return font;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont != null) return TMP_FontAsset.CreateFontAsset(sourceFont);
#endif

        return null;
    }
}
