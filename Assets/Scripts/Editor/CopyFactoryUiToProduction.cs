#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Factory Canvas의 퀘스트 수락·기술해금 UI를 Production Canvas로 복제한다.
public static class CopyFactoryUiToProduction
{
    private const string FactoryPath = "Assets/Scenes/Factory.unity";
    private const string ProductionPath = "Assets/Scenes/ProductionScene.unity";

    private static readonly string[] RootUiNames =
    {
        "QuestOpenButton",
        "orderWindow",
        "TechTreeOpenButton",
        "TechTreePanel",
    };

    [MenuItem("DungeonFront/Copy Factory Quest+Tech UI To Production")]
    public static void CopyFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene production = EditorSceneManager.OpenScene(ProductionPath, OpenSceneMode.Single);
        Scene factory = EditorSceneManager.OpenScene(FactoryPath, OpenSceneMode.Additive);

        try
        {
            Canvas productionCanvas = FindCanvas(production);
            if (productionCanvas == null)
            {
                Debug.LogError("[CopyFactoryUi] Production Canvas를 찾지 못했습니다.");
                return;
            }

            RemovePreviousCopy(productionCanvas.transform);

            GameObject questOpen = null;
            GameObject orderWindow = null;
            GameObject techOpen = null;
            GameObject techPanel = null;
            GameObject confirmPopup = null;
            TMP_Text popupTitle = null;
            TMP_Text popupDesc = null;
            TMP_Text popupCost = null;
            Button confirmButton = null;
            Button cancelButton = null;

            foreach (string name in RootUiNames)
            {
                GameObject source = FindInScene(factory, name);
                if (source == null)
                {
                    Debug.LogWarning($"[CopyFactoryUi] Factory에 '{name}'이(가) 없습니다.");
                    continue;
                }

                GameObject clone = Object.Instantiate(source, productionCanvas.transform, false);
                clone.name = name;
                Undo.RegisterCreatedObjectUndo(clone, "Copy Factory UI");

                switch (name)
                {
                    case "QuestOpenButton":
                        questOpen = clone;
                        PlaceLeftMiddle(clone.GetComponent<RectTransform>(), 108f, 0f, 168f, 56f);
                        SetButtonLabel(clone, "quest");
                        break;
                    case "orderWindow":
                        orderWindow = clone;
                        PlaceStretch(clone.GetComponent<RectTransform>(), 0.15f, 0.1f, 0.85f, 0.85f);
                        SetImageColor(clone, new Color(0.05f, 0.08f, 0.22f, 0.96f));
                        clone.SetActive(false);
                        break;
                    case "TechTreeOpenButton":
                        techOpen = clone;
                        PlaceLeftMiddle(clone.GetComponent<RectTransform>(), 288f, 0f, 168f, 56f);
                        SetButtonLabel(clone, "tech");
                        break;
                    case "TechTreePanel":
                        techPanel = clone;
                        PlaceStretch(clone.GetComponent<RectTransform>(), 0.2f, 0.1f, 0.8f, 0.9f);
                        clone.SetActive(false);
                        confirmPopup = FindChild(clone.transform, "ConfirmPopupPanel");
                        if (confirmPopup != null)
                        {
                            confirmPopup.SetActive(false);
                            popupTitle = FindTmp(confirmPopup.transform, "PopupTitleText");
                            popupDesc = FindTmp(confirmPopup.transform, "PopupDescText");
                            popupCost = FindTmp(confirmPopup.transform, "PopupCostText");
                            confirmButton = FindButton(confirmPopup.transform, "ConfirmButton");
                            cancelButton = FindButton(confirmPopup.transform, "CancelButton");
                        }
                        break;
                }
            }

            EnsureSceneRoot(production, "@QuestDataStore", typeof(QuestDataStore));
            EnsureSceneRoot(production, "@UnlockManager", typeof(UnlockManager));

            QuestWindowController questController = EnsureComponentHost<QuestWindowController>(
                productionCanvas.transform, "QuestWindowController");
            QuestCard questCardPrefab = AssetDatabase.LoadAssetAtPath<QuestCard>(
                "Assets/Scripts/Quest/QuestCard.prefab");
            GameObject questSystemRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Quest/QuestSystemRoot.prefab");
            questController.Bind(questOpen, orderWindow, questCardPrefab);
            SerializedObject questSo = new SerializedObject(questController);
            SerializedProperty systemRootProp = questSo.FindProperty("questSystemRootPrefab");
            if (systemRootProp != null)
            {
                systemRootProp.objectReferenceValue = questSystemRootPrefab;
            }

            questSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(questController);

            TechTreeUI techUi = EnsureComponentHost<TechTreeUI>(
                productionCanvas.transform, "TechTreeUI");
            techUi.Bind(techPanel, confirmPopup, popupTitle, popupDesc, popupCost, confirmButton, cancelButton);

            if (questOpen != null)
            {
                RetargetListeners(questOpen, questController, techUi);
            }

            if (orderWindow != null)
            {
                RetargetListeners(orderWindow, questController, techUi);
            }

            if (techOpen != null)
            {
                RetargetListeners(techOpen, questController, techUi);
            }

            if (techPanel != null)
            {
                RetargetListeners(techPanel, questController, techUi);
            }

            EditorSceneManager.MarkSceneDirty(production);
            EditorSceneManager.SaveScene(production);
            Debug.Log("[CopyFactoryUi] Quest + Tech UI를 Production에 복사·저장했습니다.");
        }
        finally
        {
            EditorSceneManager.CloseScene(factory, true);
        }
    }

    // Button OnClick의 Factory 컨트롤러 타겟을 Production 컨트롤러로 바꾼다.
    private static void RetargetListeners(
        GameObject root,
        QuestWindowController questController,
        TechTreeUI techUi)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            SerializedObject so = new SerializedObject(button);
            SerializedProperty calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null)
            {
                continue;
            }

            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                string method = call.FindPropertyRelative("m_MethodName").stringValue;
                SerializedProperty targetProp = call.FindPropertyRelative("m_Target");
                if (IsQuestMethod(method) && questController != null)
                {
                    targetProp.objectReferenceValue = questController;
                }
                else if (IsTechMethod(method) && techUi != null)
                {
                    targetProp.objectReferenceValue = techUi;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static bool IsQuestMethod(string method)
    {
        return method == nameof(QuestWindowController.OpenQuestWindow)
            || method == nameof(QuestWindowController.CloseQuestWindow)
            || method == nameof(QuestWindowController.ToggleQuestWindow)
            || method == nameof(QuestWindowController.OnConfirmSelection)
            || method == nameof(QuestWindowController.OnToggleQuest);
    }

    private static bool IsTechMethod(string method)
    {
        return method == nameof(TechTreeUI.ToggleTechTreePanel)
            || method == nameof(TechTreeUI.OnClickTechNode)
            || method == nameof(TechTreeUI.OnConfirmUnlock)
            || method == nameof(TechTreeUI.OnCancelPopup);
    }

    private static void RemovePreviousCopy(Transform canvas)
    {
        foreach (string name in RootUiNames)
        {
            Transform existing = canvas.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
        }

        foreach (string name in new[] { "QuestWindowController", "TechTreeUI" })
        {
            Transform existing = canvas.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }
        }
    }

    private static Canvas FindCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                return canvas;
            }
        }

        return null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindChild(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
            {
                return t.gameObject;
            }
        }

        return null;
    }

    private static TMP_Text FindTmp(Transform root, string name)
    {
        GameObject go = FindChild(root, name);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    private static Button FindButton(Transform root, string name)
    {
        GameObject go = FindChild(root, name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static void EnsureSceneRoot(Scene scene, string name, System.Type type)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name || root.GetComponent(type) != null)
            {
                return;
            }
        }

        GameObject go = new GameObject(name);
        go.AddComponent(type);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Add " + name);
    }

    private static T EnsureComponentHost<T>(Transform parent, string name) where T : Component
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            T found = existing.GetComponent<T>();
            return found != null ? found : existing.gameObject.AddComponent<T>();
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        Undo.RegisterCreatedObjectUndo(go, "Add " + name);
        return go.AddComponent<T>();
    }

    private static void PlaceTopLeft(RectTransform rect, float x, float y, float w, float h)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        rect.localScale = Vector3.one;
    }

    private static void PlaceLeftMiddle(RectTransform rect, float x, float y, float w, float h)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        rect.localScale = Vector3.one;
    }

    private static void SetButtonLabel(GameObject buttonObject, string label)
    {
        if (buttonObject == null)
        {
            return;
        }

        TMP_Text tmp = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = label;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8f;
            tmp.fontSizeMax = 14f;
            return;
        }

        Text legacy = buttonObject.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.text = label;
            legacy.fontSize = 12;
        }
    }

    private static void SetImageColor(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void PlaceTopRight(RectTransform rect, float x, float y, float w, float h)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        rect.localScale = Vector3.one;
    }

    private static void PlaceStretch(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    [MenuItem("DungeonFront/Setup Prepare Quest Window On Open Scenes")]
    public static void SetupPrepareQuestWindowOnOpenScenes()
    {
        QuestCard questCardPrefab = AssetDatabase.LoadAssetAtPath<QuestCard>(
            "Assets/Scripts/Quest/QuestCard.prefab");
        GameObject questSystemRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Quest/QuestSystemRoot.prefab");
        int updated = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (QuestWindowController controller
                    in root.GetComponentsInChildren<QuestWindowController>(true))
                {
                    SerializedObject so = new SerializedObject(controller);
                    SerializedProperty prefabProp = so.FindProperty("questCardPrefab");
                    if (prefabProp != null && questCardPrefab != null)
                    {
                        prefabProp.objectReferenceValue = questCardPrefab;
                    }

                    SerializedProperty systemRootProp = so.FindProperty("questSystemRootPrefab");
                    if (systemRootProp != null && questSystemRootPrefab != null)
                    {
                        systemRootProp.objectReferenceValue = questSystemRootPrefab;
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(controller);
                    updated++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"[SetupPrepareQuestWindow] QuestWindowController {updated}개에 프리팹을 연결했습니다.");
    }
}
#endif
