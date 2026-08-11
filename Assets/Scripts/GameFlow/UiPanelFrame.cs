using UnityEngine;
using UnityEngine.UI;

// LightFantasy 밝은 테두리 + 어두운 채움 패널.
// Resources 텍스처에서 9-slice 보더를 코드로 고정해, 수동 meta에 의존하지 않는다.
public static class UiPanelFrame
{
    public const string ResourcesPath = "UI/LightFantasy_panel_lightBorder_filled";

    // PreSized 256×160 기준. 코너 소용돌이 전체가 스트레치되지 않는 크기.
    public static readonly Vector4 SliceBorder = new Vector4(32f, 32f, 32f, 32f);

    // 작을수록 테두리가 커진다. 1이면 원본 픽셀 크기라 큰 창에서 너무 작아 보인다.
    public const float DefaultPixelsPerUnitMultiplier = 0.5f;

    private static Sprite cachedPanelSprite;
    private static bool loggedMissing;

    public static Sprite GetPanelSprite()
    {
        if (cachedPanelSprite != null)
        {
            return cachedPanelSprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(ResourcesPath);
        if (texture == null)
        {
            // Sprite로만 임포트된 경우 텍스처를 다시 꺼낸다.
            Sprite imported = Resources.Load<Sprite>(ResourcesPath);
            if (imported != null)
            {
                texture = imported.texture;
            }
        }

        if (texture == null)
        {
            if (!loggedMissing)
            {
                loggedMissing = true;
                Debug.LogWarning($"[UiPanelFrame] 패널 텍스처를 찾지 못했습니다: Resources/{ResourcesPath}");
            }

            return null;
        }

        // Unity Sprite Editor 보더와 무관하게, 우리가 잰 9-slice 보더로 스프라이트를 만든다.
        cachedPanelSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            SliceBorder);
        cachedPanelSprite.name = "LightFantasy_panel_lightBorder_filled_runtime";
        return cachedPanelSprite;
    }

    public static void Apply(Image image, float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetPanelSprite();
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

    // 패널 루트 GameObject의 Image에 프레임을 적용한다. 없으면 Image를 추가한다.
    public static void ApplyTo(GameObject panelRoot, float pixelsPerUnitMultiplier = DefaultPixelsPerUnitMultiplier)
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

        Apply(image, pixelsPerUnitMultiplier);
    }
}
