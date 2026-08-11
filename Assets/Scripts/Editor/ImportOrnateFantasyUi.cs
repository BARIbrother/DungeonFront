using System.IO;
using UnityEditor;
using UnityEngine;

// Ornate Fantasy UI 패널을 Unity TextureImporter로 정식 설정한다.
// (PNG만 복사한 뒤 손 meta에 의존하지 않도록 에디터 API로 재임포트한다.)
public static class ImportOrnateFantasyUi
{
    private const string ArtPanelPath =
        "Assets/Art/UI/OrnateFantasy/LightFantasy_panel_lightBorder_filled.png";
    private const string ResourcesPanelPath =
        "Assets/Resources/UI/LightFantasy_panel_lightBorder_filled.png";

    private static readonly Vector4 PanelSliceBorder = new Vector4(32f, 32f, 32f, 32f);

    [MenuItem("DungeonFront/UI/Reimport LightFantasy Panel (Unity Importer)")]
    public static void ReimportPanel()
    {
        EnsureReadableCopy();
        ConfigureSprite(ArtPanelPath);
        ConfigureSprite(ResourcesPanelPath);
        AssetDatabase.Refresh();
        Debug.Log("[ImportOrnateFantasyUi] LightFantasy 패널을 TextureImporter로 재설정했습니다. Sprite Mode=Single, Border=32, Filter=Point, Readable=On");
    }

    [MenuItem("DungeonFront/UI/Import Ornate Fantasy UI .unitypackage")]
    public static void ImportUnityPackage()
    {
        string downloads = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            "Downloads",
            "UI",
            "Ornate Fantasy UI Assets v1.3",
            "Ornate Fantasy UI Assets v1.3",
            "Ornate fantasy UI .unitypackage v1.3.unitypackage");

        if (!File.Exists(downloads))
        {
            Debug.LogError($"[ImportOrnateFantasyUi] unitypackage 없음:\n{downloads}");
            return;
        }

        AssetDatabase.ImportPackage(downloads, interactive: true);
    }

    private static void EnsureReadableCopy()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string artAbs = Path.Combine(projectRoot, ArtPanelPath.Replace('/', Path.DirectorySeparatorChar));
        string resAbs = Path.Combine(projectRoot, ResourcesPanelPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(artAbs))
        {
            Debug.LogError($"[ImportOrnateFantasyUi] 아트 파일 없음: {ArtPanelPath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(resAbs));
        File.Copy(artAbs, resAbs, overwrite: true);
    }

    private static void ConfigureSprite(string assetPath)
    {
        if (!File.Exists(assetPath) && !File.Exists(ToAbsolute(assetPath)))
        {
            Debug.LogWarning($"[ImportOrnateFantasyUi] 건너뜀(파일 없음): {assetPath}");
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
            Debug.LogError($"[ImportOrnateFantasyUi] TextureImporter 없음: {assetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = PanelSliceBorder;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    private static string ToAbsolute(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
