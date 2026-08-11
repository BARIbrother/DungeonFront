using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 씬에 깔린 주요 패널 Image에 LightFantasy 밝은 테두리 프레임을 붙인다.
public static class ApplyLightFantasyPanelFrames
{
    private const string PanelSpritePath =
        "Assets/Art/UI/OrnateFantasy/LightFantasy_panel_lightBorder_filled.png";

    private static readonly string[] PanelObjectNames =
    {
        "TechTreePanel",
        "OrderWindow",
        "ConfirmPopupPanel",
        "SettlementUI",
        "ShopWindow",
        "UnlockPanel",
    };

    [MenuItem("DungeonFront/UI/Apply LightFantasy Panel Frames To Open Scenes")]
    public static void ApplyToOpenScenes()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
        if (sprite == null)
        {
            Debug.LogError($"[ApplyLightFantasyPanelFrames] 스프라이트 없음: {PanelSpritePath}");
            return;
        }

        int changed = 0;
        foreach (string name in PanelObjectNames)
        {
            GameObject[] roots = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in roots)
            {
                if (go == null || go.name != name)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(go))
                {
                    continue;
                }

                Image image = go.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                Undo.RecordObject(image, "Apply LightFantasy Panel Frame");
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.pixelsPerUnitMultiplier = UiPanelFrame.DefaultPixelsPerUnitMultiplier;
                EditorUtility.SetDirty(image);
                changed++;
            }
        }

        Debug.Log($"[ApplyLightFantasyPanelFrames] 패널 {changed}개에 프레임 적용");
    }
}
