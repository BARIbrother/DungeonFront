using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 씬 TMP에 나눔고딕 + UI 텍스트 스타일(크림/아웃라인)을 적용한다.
public static class TmpUiFontBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AssignMissingUIFonts()
    {
        TMP_FontAsset font = TmpUiStyle.ResolveFont();
        if (font == null)
        {
            Debug.LogWarning("[TmpUiFontBootstrap] NanumGothic SDF를 찾지 못했습니다.");
            return;
        }

        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text == null)
            {
                continue;
            }

            // 폰트가 비었거나 기본 Liberation만 있는 경우 스타일을 덮어쓴다.
            if (text.font == null
                || text.font.name.IndexOf("LiberationSans", System.StringComparison.OrdinalIgnoreCase) >= 0
                || text.font.name.IndexOf("NanumGothic", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                TmpUiStyle.Apply(text, InferBootstrapRole(text));
            }
        }
    }

    private static TmpUiStyle.Role InferBootstrapRole(TextMeshProUGUI text)
    {
        string name = text.gameObject.name;
        Transform parent = text.transform.parent;
        string parentName = parent != null ? parent.name : string.Empty;

        if (parentName.IndexOf("Button", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Button", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.Equals("Label", System.StringComparison.OrdinalIgnoreCase))
        {
            return TmpUiStyle.Role.Button;
        }

        if (name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("DayText", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return TmpUiStyle.Role.Title;
        }

        if (name.IndexOf("Timer", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Gold", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Reputation", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return TmpUiStyle.Role.Caption;
        }

        return TmpUiStyle.Role.Body;
    }
}
