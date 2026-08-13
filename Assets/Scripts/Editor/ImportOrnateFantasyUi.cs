using System.IO;
using UnityEditor;
using UnityEngine;

// LightFantasy 시트/프레임을 TextureImporter로 정식 임포트한다.
public static class ImportOrnateFantasyUi
{
    private const string ArtDir = "Assets/Art/UI/OrnateFantasy";
    private const string ResourcesDir = "Assets/Resources/UI";

    private static readonly string PackSheetsDir = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        "Downloads",
        "UI",
        "Ornate Fantasy UI Assets v1.3",
        "Ornate Fantasy UI Assets v1.3",
        "SpriteSheets");

    private static readonly string UnityPackagePath = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        "Downloads",
        "UI",
        "Ornate Fantasy UI Assets v1.3",
        "Ornate Fantasy UI Assets v1.3",
        "Ornate fantasy UI .unitypackage v1.3.unitypackage");

    private struct FrameImport
    {
        public string FileName;
        public Vector4 Border;
        public bool Readable;
    }

    private static readonly FrameImport[] Frames =
    {
        new FrameImport { FileName = "LightFantasy_panel_lightBorder_filled.png", Border = new Vector4(32f, 32f, 32f, 32f), Readable = true },
        new FrameImport { FileName = "LightFantasy_panel_darkOrnate.png", Border = new Vector4(42f, 42f, 42f, 42f), Readable = true },
        new FrameImport { FileName = "LightFantasy_panel_creamOrnate.png", Border = new Vector4(42f, 42f, 42f, 42f), Readable = true },
        new FrameImport { FileName = "LightFantasy_frame_tall.png", Border = new Vector4(28f, 28f, 28f, 28f), Readable = true },
        new FrameImport { FileName = "LightFantasy_frame_content.png", Border = new Vector4(28f, 28f, 28f, 28f), Readable = true },
        new FrameImport { FileName = "LightFantasy_frame_wide.png", Border = new Vector4(28f, 28f, 28f, 28f), Readable = true },
        new FrameImport { FileName = "LightFantasy_frame_bar.png", Border = new Vector4(28f, 28f, 28f, 28f), Readable = true },
        new FrameImport { FileName = "LightFantasy_frame_parchment.png", Border = new Vector4(14f, 14f, 14f, 14f), Readable = true },
        new FrameImport { FileName = "LightFantasy_banner_cream.png", Border = new Vector4(28f, 8f, 28f, 8f), Readable = true },
        new FrameImport { FileName = "LightFantasy_banner_tan.png", Border = new Vector4(28f, 8f, 28f, 8f), Readable = true },
        new FrameImport { FileName = "LightFantasy_button_dark_normal.png", Border = new Vector4(16f, 16f, 16f, 16f), Readable = true },
        new FrameImport { FileName = "LightFantasy_button_dark_highlight.png", Border = new Vector4(16f, 16f, 16f, 16f), Readable = true },
        new FrameImport { FileName = "LightFantasy_button_dark_pressed.png", Border = new Vector4(16f, 16f, 16f, 16f), Readable = true },
        new FrameImport { FileName = "LightFantasy_button_dark_disabled.png", Border = new Vector4(16f, 16f, 16f, 16f), Readable = true },
        new FrameImport { FileName = "LightFantasy_pill48a.png", Border = new Vector4(24f, 12f, 24f, 12f), Readable = true },
        new FrameImport { FileName = "LightFantasy_pill48b.png", Border = new Vector4(24f, 12f, 24f, 12f), Readable = true },
    };

    [MenuItem("DungeonFront/UI/Import LightFantasy Full (Sheets + Frames)")]
    public static void ImportLightFantasyFull()
    {
        EnsureDirectory(ArtDir);
        EnsureDirectory(ResourcesDir);
        CopySheet("LightFantasyUISheet.png");
        CopySheet("PreSizedOrnateFantasyUI.png");

        int configured = 0;
        foreach (FrameImport frame in Frames)
        {
            string artPath = $"{ArtDir}/{frame.FileName}";
            string resPath = $"{ResourcesDir}/{frame.FileName}";
            SyncArtToResources(artPath, resPath);
            if (ConfigureSprite(artPath, frame.Border, frame.Readable))
            {
                configured++;
            }

            if (ConfigureSprite(resPath, frame.Border, frame.Readable))
            {
                configured++;
            }
        }

        ConfigureSheetAsMultiple($"{ArtDir}/LightFantasyUISheet.png");
        ConfigureSheetAsMultiple($"{ArtDir}/PreSizedOrnateFantasyUI.png");
        AssetDatabase.Refresh();
        Debug.Log(
            $"[ImportOrnateFantasyUi] LightFantasy 임포트 완료. 프레임 설정 {configured}건. " +
            "시트는 Sprite Mode=Multiple로 등록했습니다.");
    }

    [MenuItem("DungeonFront/UI/Reimport LightFantasy Panel (Unity Importer)")]
    public static void ReimportPanel()
    {
        ImportLightFantasyFull();
    }

    [MenuItem("DungeonFront/UI/Import Ornate Fantasy UI .unitypackage")]
    public static void ImportUnityPackage()
    {
        if (!File.Exists(UnityPackagePath))
        {
            Debug.LogError($"[ImportOrnateFantasyUi] unitypackage 없음:\n{UnityPackagePath}");
            return;
        }

        AssetDatabase.ImportPackage(UnityPackagePath, interactive: true);
    }

    [MenuItem("DungeonFront/UI/Import NoteBook Slot Sprites")]
    public static void ImportNoteBookSlots()
    {
        string noteBookSprites = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            "Downloads",
            "UI",
            "Complete_UI_Book_Styles_Pack_Full",
            "Complete_UI_Book_Styles_Pack_Full_v1.0",
            "03_NoteBook",
            "Sprites");

        string artDir = "Assets/Art/UI/NoteBook";
        EnsureDirectory(artDir);
        EnsureDirectory(ResourcesDir);

        string[] files =
        {
            "UI_NoteBook_Slot02a.png",
            "UI_NoteBook_Slot03a.png",
            "UI_NoteBook_Slot04a.png",
            "UI_NoteBook_Select01a.png",
        };

        int configured = 0;
        foreach (string fileName in files)
        {
            string source = Path.Combine(noteBookSprites, fileName);
            string artPath = $"{artDir}/{fileName}";
            string resPath = $"{ResourcesDir}/{fileName}";
            if (!File.Exists(source))
            {
                Debug.LogError($"[ImportOrnateFantasyUi] NoteBook 원본 없음: {source}");
                continue;
            }

            File.Copy(source, ToAbsolute(artPath), overwrite: true);
            File.Copy(source, ToAbsolute(resPath), overwrite: true);
            AssetDatabase.ImportAsset(artPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(resPath, ImportAssetOptions.ForceUpdate);

            // 슬롯은 고정 크기 아이콘 칸이라 border 0으로 Single 임포트.
            Vector4 border = Vector4.zero;
            if (ConfigureSprite(artPath, border, readable: true))
            {
                configured++;
            }

            if (ConfigureSprite(resPath, border, readable: true))
            {
                configured++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ImportOrnateFantasyUi] NoteBook 슬롯 임포트 완료: {configured}건");
    }

    [MenuItem("DungeonFront/UI/Fix NoteBook Slot Import Settings")]
    public static void FixNoteBookImportSettings()
    {
        string[] assetPaths =
        {
            "Assets/Art/UI/NoteBook/UI_NoteBook_Slot02a.png",
            "Assets/Art/UI/NoteBook/UI_NoteBook_Slot03a.png",
            "Assets/Art/UI/NoteBook/UI_NoteBook_Slot04a.png",
            "Assets/Art/UI/NoteBook/UI_NoteBook_Select01a.png",
            "Assets/Resources/UI/UI_NoteBook_Slot02a.png",
            "Assets/Resources/UI/UI_NoteBook_Slot03a.png",
            "Assets/Resources/UI/UI_NoteBook_Slot04a.png",
            "Assets/Resources/UI/UI_NoteBook_Select01a.png",
        };

        int configured = 0;
        foreach (string assetPath in assetPaths)
        {
            if (!File.Exists(ToAbsolute(assetPath)))
            {
                continue;
            }

            if (ConfigureSprite(assetPath, Vector4.zero, readable: true))
            {
                configured++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ImportOrnateFantasyUi] NoteBook Single 스프라이트 재임포트: {configured}건");
    }

    private static void CopySheet(string fileName)
    {
        string source = Path.Combine(PackSheetsDir, fileName);
        string dest = ToAbsolute($"{ArtDir}/{fileName}");
        if (!File.Exists(source))
        {
            Debug.LogWarning($"[ImportOrnateFantasyUi] 시트 원본 없음: {source}");
            return;
        }

        File.Copy(source, dest, overwrite: true);
        AssetDatabase.ImportAsset($"{ArtDir}/{fileName}", ImportAssetOptions.ForceUpdate);
    }

    private static void SyncArtToResources(string artPath, string resPath)
    {
        string artAbs = ToAbsolute(artPath);
        string resAbs = ToAbsolute(resPath);
        if (!File.Exists(artAbs))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(resAbs));
        File.Copy(artAbs, resAbs, overwrite: true);
    }

    private static bool ConfigureSprite(string assetPath, Vector4 border, bool readable)
    {
        if (!File.Exists(ToAbsolute(assetPath)))
        {
            return false;
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        }

        if (importer == null)
        {
            Debug.LogError($"[ImportOrnateFantasyUi] TextureImporter 없음: {assetPath}");
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = readable;
        importer.SaveAndReimport();
        return true;
    }

    // 전체 시트는 Multiple 모드로 두고, Sprite Editor에서 수동 분할할 수 있게 한다.
    private static void ConfigureSheetAsMultiple(string assetPath)
    {
        if (!File.Exists(ToAbsolute(assetPath)))
        {
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        }

        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    private static void EnsureDirectory(string assetPath)
    {
        string abs = ToAbsolute(assetPath);
        Directory.CreateDirectory(abs);
    }

    private static string ToAbsolute(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}

// NoteBook 슬롯 PNG는 항상 Single 스프라이트로 임포트한다. (자동 슬라이스 방지)
public sealed class NoteBookSpritePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        bool noteBook = assetPath.Contains("UI_NoteBook_Slot02a")
            || assetPath.Contains("UI_NoteBook_Slot03a")
            || assetPath.Contains("UI_NoteBook_Slot04a")
            || assetPath.Contains("UI_NoteBook_Select01a");
        bool techIcon = assetPath.Replace('\\', '/').Contains("/UI/TechTree/")
            && assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
            && !assetPath.Replace('\\', '/').Contains("/_preview/");
        if (!noteBook && !techIcon)
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = Vector4.zero;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;
    }
}
