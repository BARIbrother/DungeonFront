using UnityEngine;
using UnityEngine.UI;

// LightFantasy 둥근 사각 버튼. Normal/Highlight/Pressed/Disabled 스프라이트 스왑.
public static class UiButtonStyle
{
    private const string NormalPath = "UI/LightFantasy_button_dark_normal";
    private const string HighlightPath = "UI/LightFantasy_button_dark_highlight";
    private const string PressedPath = "UI/LightFantasy_button_dark_pressed";
    private const string DisabledPath = "UI/LightFantasy_button_dark_disabled";

    // 192×81(원본 64×27 ×3) 기준. 둥근 모서리만 고정하고 가운데만 늘린다.
    // 값이 크면 짧은 버튼에서 끝이 뾰족/다이아처럼 보인다.
    public static readonly Vector4 SliceBorder = new Vector4(24f, 18f, 24f, 18f);

    public const float DefaultPixelsPerUnitMultiplier = 0.75f;

    private static Sprite normalSprite;
    private static Sprite highlightSprite;
    private static Sprite pressedSprite;
    private static Sprite disabledSprite;
    private static bool loggedMissing;

    public static void Apply(Button button, float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        if (button == null || ShouldSkip(button))
        {
            return;
        }

        EnsureSprites();
        if (normalSprite == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        image.sprite = normalSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.useSpriteMesh = false;
        image.fillCenter = true;

        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = highlightSprite != null ? highlightSprite : normalSprite,
            pressedSprite = pressedSprite != null ? pressedSprite : normalSprite,
            selectedSprite = highlightSprite != null ? highlightSprite : normalSprite,
            disabledSprite = disabledSprite != null ? disabledSprite : normalSprite
        };
    }

    public static void ApplyInChildren(GameObject root, float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Apply(buttons[i], pixelsPerUnitMultiplier);
        }
    }

    private static bool ShouldSkip(Button button)
    {
        if (button == null)
        {
            return true;
        }

        string name = button.gameObject.name;
        if (name.IndexOf("Backdrop", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Dimmer", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Overlay", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Slot", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Port", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Cell", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        Image image = button.GetComponent<Image>();
        if (image != null && image.color.a < 0.05f && image.sprite == null)
        {
            return true;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect != null
            && rect.anchorMin == Vector2.zero
            && rect.anchorMax == Vector2.one
            && Mathf.Abs(rect.offsetMin.x) < 1f
            && Mathf.Abs(rect.offsetMin.y) < 1f
            && Mathf.Abs(rect.offsetMax.x) < 1f
            && Mathf.Abs(rect.offsetMax.y) < 1f
            && rect.parent != null
            && rect.parent.GetComponent<Canvas>() != null)
        {
            return true;
        }

        return false;
    }

    private static void EnsureSprites()
    {
        if (normalSprite != null)
        {
            return;
        }

        normalSprite = LoadSlicedSprite(NormalPath, "btn_normal");
        highlightSprite = LoadSlicedSprite(HighlightPath, "btn_highlight");
        pressedSprite = LoadSlicedSprite(PressedPath, "btn_pressed");
        disabledSprite = LoadSlicedSprite(DisabledPath, "btn_disabled");

        if (normalSprite == null && !loggedMissing)
        {
            loggedMissing = true;
            Debug.LogWarning($"[UiButtonStyle] 버튼 스프라이트를 찾지 못했습니다: Resources/{NormalPath}");
        }
    }

    private static Sprite LoadSlicedSprite(string resourcesPath, string spriteName)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcesPath);
        if (texture == null)
        {
            Sprite imported = Resources.Load<Sprite>(resourcesPath);
            if (imported != null)
            {
                texture = imported.texture;
            }
        }

        if (texture == null)
        {
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            SliceBorder);
        sprite.name = spriteName;
        return sprite;
    }
}
