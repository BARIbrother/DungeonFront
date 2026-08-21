#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// TextureImporter.spritesheet 대신 ISpriteEditorDataProvider로 슬라이스를 읽고 쓴다.
public static class SpriteSheetImporterUtil
{
    public static SpriteRect[] GetSpriteRects(TextureImporter importer)
    {
        ISpriteEditorDataProvider provider = TryGetProvider(importer);
        if (provider == null)
        {
            return System.Array.Empty<SpriteRect>();
        }

        return provider.GetSpriteRects() ?? System.Array.Empty<SpriteRect>();
    }

    public static void SetSpriteRects(TextureImporter importer, SpriteRect[] rects)
    {
        ISpriteEditorDataProvider provider = TryGetProvider(importer);
        if (provider == null || rects == null)
        {
            return;
        }

        provider.SetSpriteRects(rects);

        ISpriteNameFileIdDataProvider nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIds != null)
        {
            var pairs = new List<SpriteNameFileIdPair>(rects.Length);
            for (int i = 0; i < rects.Length; i++)
            {
                pairs.Add(new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID));
            }

            nameIds.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
    }

    public static SpriteRect CreateRect(string name, Rect pixelRect, Vector2 pivot, GUID existingId)
    {
        return new SpriteRect
        {
            name = name,
            rect = pixelRect,
            alignment = SpriteAlignment.Custom,
            pivot = pivot,
            spriteID = existingId.Empty() ? GUID.Generate() : existingId,
        };
    }

    public static GUID FindExistingId(SpriteRect[] existing, string name)
    {
        if (existing == null)
        {
            return new GUID();
        }

        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null && existing[i].name == name)
            {
                return existing[i].spriteID;
            }
        }

        return new GUID();
    }

    private static ISpriteEditorDataProvider TryGetProvider(TextureImporter importer)
    {
        if (importer == null)
        {
            return null;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            return null;
        }

        provider.InitSpriteEditorDataProvider();
        return provider;
    }
}
#endif
