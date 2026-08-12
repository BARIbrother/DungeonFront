using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 나눔고딕을 유지하되, 크림 텍스트·얇은 아웃라인·크기 계층으로 싸 보이지 않게 만든다.
public static class TmpUiStyle
{
    public static readonly Color BodyColor = new Color(0.92f, 0.88f, 0.78f, 1f);
    public static readonly Color TitleColor = new Color(0.96f, 0.93f, 0.84f, 1f);
    public static readonly Color MutedColor = new Color(0.78f, 0.72f, 0.62f, 1f);
    public static readonly Color OutlineColor = new Color(0.12f, 0.09f, 0.08f, 0.85f);

    public const float TitleSize = 34f;
    public const float BodySize = 26f;
    public const float ButtonSize = 28f;
    public const float CaptionSize = 20f;

    private const string NanumResourcesPath = "Fonts & Materials/NanumGothic SDF";
    private const string NanumAssetPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothic SDF.asset";

    private static TMP_FontAsset cachedFont;

    public enum Role
    {
        Title,
        Body,
        Button,
        Caption
    }

    public static TMP_FontAsset ResolveFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = Resources.Load<TMP_FontAsset>(NanumResourcesPath);
        if (cachedFont != null)
        {
            return cachedFont;
        }

#if UNITY_EDITOR
        cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NanumAssetPath);
        if (cachedFont != null)
        {
            return cachedFont;
        }
#endif

        cachedFont = TMP_Settings.defaultFontAsset;
        return cachedFont;
    }

    public static void Apply(TMP_Text text, Role role = Role.Body)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset font = ResolveFont();
        if (font != null)
        {
            text.font = font;
        }

        switch (role)
        {
            case Role.Title:
                text.fontSize = TitleSize;
                text.fontStyle = FontStyles.Bold;
                text.color = TitleColor;
                text.characterSpacing = -1.5f;
                text.lineSpacing = 8f;
                ApplyOutline(text, 0.22f);
                break;
            case Role.Button:
                text.fontSize = ButtonSize;
                text.fontStyle = FontStyles.Bold;
                text.color = TitleColor;
                text.characterSpacing = -1f;
                text.lineSpacing = 0f;
                text.enableAutoSizing = false;
                text.overflowMode = TextOverflowModes.Overflow;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                ApplyOutline(text, 0.2f);
                break;
            case Role.Caption:
                text.fontSize = CaptionSize;
                text.fontStyle = FontStyles.Bold;
                text.color = MutedColor;
                text.characterSpacing = -0.5f;
                text.lineSpacing = 4f;
                ApplyOutline(text, 0.16f);
                break;
            default:
                text.fontSize = BodySize;
                text.fontStyle = FontStyles.Bold;
                text.color = BodyColor;
                text.characterSpacing = -0.8f;
                text.lineSpacing = 6f;
                ApplyOutline(text, 0.18f);
                break;
        }
    }

    // 밝은 패널(배너·양피지)용. 검정 글씨 + 얇은 밝은 윤곽.
    public static void ApplyOnLightPanel(TMP_Text text, Role role = Role.Body)
    {
        Apply(text, role);
        if (text == null)
        {
            return;
        }

        text.color = new Color(0.08f, 0.06f, 0.05f, 1f);
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = new Color(1f, 0.97f, 0.9f, 0.35f);
        text.outlineWidth = 0.08f;
    }

    public static void ApplyToHierarchy(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text == null)
            {
                continue;
            }

            Apply(text, InferRole(text));
        }
    }

    private static Role InferRole(TMP_Text text)
    {
        string name = text.gameObject.name;
        Transform parent = text.transform.parent;
        string parentName = parent != null ? parent.name : string.Empty;

        if (name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0
            || parentName.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Header", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return Role.Title;
        }

        if (parentName.IndexOf("Button", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.Equals("Label", System.StringComparison.OrdinalIgnoreCase)
            || name.IndexOf("Button", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return Role.Button;
        }

        if (name.IndexOf("Deadline", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Caption", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Hint", System.StringComparison.OrdinalIgnoreCase) >= 0
            || text.fontSize <= 15f)
        {
            return Role.Caption;
        }

        return Role.Body;
    }

    private static void ApplyOutline(TMP_Text text, float thickness)
    {
        text.outlineColor = OutlineColor;
        text.outlineWidth = thickness;
    }
}
