using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Factory 씬 TMP UI가 폰트 에셋을 못 찾을 때 기본 폰트를 다시 연결한다.
public static class TmpUiFontBootstrap
{
    private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AssignMissingUIFonts()
    {
        TMP_FontAsset font = ResolveDefaultFont();
        if (font == null)
        {
            Debug.LogWarning("[TmpUiFontBootstrap] LiberationSans SDF를 찾지 못했습니다.");
            return;
        }

        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null || text.font != null)
            {
                continue;
            }

            text.font = font;
        }
    }

    private static TMP_FontAsset ResolveDefaultFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null)
        {
            return font;
        }

        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            return font;
        }

#if UNITY_EDITOR
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null)
        {
            return font;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont != null)
        {
            return TMP_FontAsset.CreateFontAsset(sourceFont);
        }
#endif

        return null;
    }
}
