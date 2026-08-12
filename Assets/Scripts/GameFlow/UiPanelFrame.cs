using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// LightFantasy 패널/프레임. 용도별로 다른 스프라이트를 쓴다.
public static class UiPanelFrame
{
    public enum Kind
    {
        Default,
        DarkOrnate,
        CreamOrnate,
        Tall,
        Content,
        Wide,
        Bar,
        Parchment,
        BannerCream,
        BannerTan
    }

    public const string DefaultResourcesPath = "UI/LightFantasy_panel_lightBorder_filled";

    // PreSized/업스케일 기준. 코너 장식이 스트레치되지 않게 둔다.
    public static readonly Vector4 DefaultSliceBorder = new Vector4(32f, 32f, 32f, 32f);
    public const float DefaultPixelsPerUnitMultiplier = 0.5f;

    private static readonly Dictionary<Kind, string> ResourcePaths = new Dictionary<Kind, string>
    {
        { Kind.Default, "UI/LightFantasy_panel_lightBorder_filled" },
        { Kind.DarkOrnate, "UI/LightFantasy_panel_darkOrnate" },
        { Kind.CreamOrnate, "UI/LightFantasy_panel_creamOrnate" },
        { Kind.Tall, "UI/LightFantasy_frame_content" },
        { Kind.Content, "UI/LightFantasy_frame_content" },
        { Kind.Wide, "UI/LightFantasy_frame_wide" },
        { Kind.Bar, "UI/LightFantasy_frame_bar" },
        { Kind.Parchment, "UI/LightFantasy_frame_parchment" },
        { Kind.BannerCream, "UI/LightFantasy_banner_cream" },
        { Kind.BannerTan, "UI/LightFantasy_banner_tan" },
    };

    private static readonly Dictionary<Kind, Vector4> SliceBorders = new Dictionary<Kind, Vector4>
    {
        { Kind.Default, new Vector4(32f, 32f, 32f, 32f) },
        { Kind.DarkOrnate, new Vector4(42f, 42f, 42f, 42f) },
        { Kind.CreamOrnate, new Vector4(42f, 42f, 42f, 42f) },
        { Kind.Tall, new Vector4(28f, 28f, 28f, 28f) },
        { Kind.Content, new Vector4(28f, 28f, 28f, 28f) },
        { Kind.Wide, new Vector4(28f, 28f, 28f, 28f) },
        { Kind.Bar, new Vector4(28f, 28f, 28f, 28f) },
        { Kind.Parchment, new Vector4(14f, 14f, 14f, 14f) },
        { Kind.BannerCream, new Vector4(28f, 8f, 28f, 8f) },
        { Kind.BannerTan, new Vector4(28f, 8f, 28f, 8f) },
    };

    private static readonly Dictionary<Kind, Sprite> CachedSprites = new Dictionary<Kind, Sprite>();
    private static readonly HashSet<Kind> LoggedMissing = new HashSet<Kind>();

    public static readonly Vector4 SliceBorder = DefaultSliceBorder;

    public static void ClearCache()
    {
        CachedSprites.Clear();
        LoggedMissing.Clear();
    }

    public static Sprite GetPanelSprite()
    {
        return GetSprite(Kind.Default);
    }

    public static Sprite GetSprite(Kind kind)
    {
        if (CachedSprites.TryGetValue(kind, out Sprite cached) && cached != null)
        {
            return cached;
        }

        if (!ResourcePaths.TryGetValue(kind, out string path))
        {
            path = DefaultResourcesPath;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Sprite imported = Resources.Load<Sprite>(path);
            if (imported != null)
            {
                texture = imported.texture;
            }
        }

        if (texture == null)
        {
            if (LoggedMissing.Add(kind))
            {
                Debug.LogWarning($"[UiPanelFrame] 프레임 텍스처 없음: Resources/{path}");
            }

            return null;
        }

        Vector4 border = SliceBorders.TryGetValue(kind, out Vector4 custom)
            ? custom
            : DefaultSliceBorder;

        if (!texture.isReadable)
        {
            Sprite imported = Resources.Load<Sprite>(path);
            if (imported != null)
            {
                CachedSprites[kind] = imported;
                return imported;
            }
        }

        // 임포트 Sprite border에 의존하지 않고, 코드에서 9-slice를 고정한다.
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            border);
        sprite.name = $"{path.Replace('/', '_')}_runtime";
        CachedSprites[kind] = sprite;
        return sprite;
    }

    public static void Apply(
        Image image,
        float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        Apply(image, Kind.Default, pixelsPerUnitMultiplier);
    }

    public static void Apply(
        Image image,
        Kind kind,
        float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetSprite(kind);
        if (sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
        image.useSpriteMesh = false;
        image.fillCenter = true;
    }

    public static void ApplyTo(
        GameObject panelRoot,
        float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        ApplyTo(panelRoot, Kind.Default, pixelsPerUnitMultiplier);
    }

    public static void ApplyTo(
        GameObject panelRoot,
        Kind kind,
        float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        if (panelRoot == null)
        {
            return;
        }

        Image image = panelRoot.GetComponent<Image>();
        if (image == null)
        {
            image = panelRoot.AddComponent<Image>();
        }

        Apply(image, kind, pixelsPerUnitMultiplier);
    }
}
