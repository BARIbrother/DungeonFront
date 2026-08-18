#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// TMP가 요구하는 atlas/material 서브에셋까지 함께 저장하는 한글 폰트 생성 메뉴입니다.
/// 자동 실행하지 않으며, Tools 메뉴에서 사용자가 한 번 실행할 때만 동작합니다.
/// </summary>
public static class CreateProjectKoreanFont
{
    private const string WindowsNotoPath = "C:/Windows/Fonts/NotoSansKR-VF.ttf";
    private const string FontFolder = "Assets/Fonts";
    private const string ProjectFontPath = FontFolder + "/NotoSansKR-VF.ttf";
    private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansKR SDF.asset";

    [MenuItem("Tools/DungeonFront/Repair Korean TMP Font", priority = 100)]
    public static void Repair()
    {
        if (!File.Exists(WindowsNotoPath))
        {
            EditorUtility.DisplayDialog("한글 폰트 없음", "Windows의 Noto Sans KR 폰트를 찾지 못했습니다.\n" + WindowsNotoPath, "확인");
            return;
        }

        if (!Directory.Exists(FontFolder))
        {
            Directory.CreateDirectory(FontFolder);
        }

        if (!File.Exists(ProjectFontPath))
        {
            File.Copy(WindowsNotoPath, ProjectFontPath);
            AssetDatabase.ImportAsset(ProjectFontPath, ImportAssetOptions.ForceSynchronousImport);
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>(ProjectFontPath);
        if (source == null)
        {
            EditorUtility.DisplayDialog("폰트 가져오기 실패", "TTF를 Unity 폰트로 읽지 못했습니다. Project 창에서 Assets/Fonts/NotoSansKR-VF.ttf가 보이는지 확인하세요.", "확인");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            source,
            48,
            6,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (fontAsset == null || fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0 || fontAsset.atlasTextures[0] == null)
        {
            EditorUtility.DisplayDialog("TMP 폰트 생성 실패", "TMP atlas를 생성하지 못했습니다.", "확인");
            return;
        }

        fontAsset.name = "NotoSansKR SDF";
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        Texture2D atlas = fontAsset.atlasTextures[0];
        atlas.name = "NotoSansKR SDF Atlas";
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);

        Material material = fontAsset.material;
        if (material != null)
        {
            material.name = "NotoSansKR SDF Material";
            AssetDatabase.AddObjectToAsset(material, fontAsset);
        }

        // 자주 보이는 UI 문구는 즉시 atlas에 넣어 첫 프레임부터 표시한다.
        fontAsset.TryAddCharacters("가나다라마바사아자차카타파하의뢰수락납품진행가능보상요구재료보유필요마감오늘없음알수첫번째시험추가현재", out _);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceSynchronousImport);

        if (TMP_Settings.fallbackFontAssets == null)
        {
            TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
        }

        if (!TMP_Settings.fallbackFontAssets.Contains(fontAsset))
        {
            TMP_Settings.fallbackFontAssets.Add(fontAsset);
        }

        EditorUtility.SetDirty(TMP_Settings.instance);
        AssetDatabase.SaveAssets();
        Debug.Log("[KoreanFont] 프로젝트 한글 TMP 폰트 생성 완료. Play를 다시 시작하세요.");
        EditorUtility.DisplayDialog("완료", "프로젝트용 한글 TMP 폰트를 만들었습니다.\nPlay를 중지했다가 다시 시작하세요.", "확인");
    }
}
#endif
