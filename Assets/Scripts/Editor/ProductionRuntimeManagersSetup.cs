#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Production 씬에 런타임 Bootstrap 매니저가 없으면 empty + 스크립트로 배치한다.
public static class ProductionRuntimeManagersSetup
{
    private const string ScenePath = "Assets/Scenes/ProductionScene.unity";

    private static readonly (string name, System.Type type)[] Managers =
    {
        ("ZoneManager", typeof(ZoneManager)),
        ("ZoneExpansionUISystem", typeof(ZoneExpansionUI)),
        ("MapNodeLayoutApplier", typeof(MapNodeLayoutApplier)),
        ("TickManager", typeof(TickManager)),
        ("ProductionEventManager", typeof(ProductionEventManager)),
        ("MachineRecipeUISystem", typeof(MachineRecipeUI)),
        ("MachineGrantUISystem", typeof(MachineGrantUI)),
        ("ProductionSummaryUISystem", typeof(ProductionSummaryUI)),
        ("FactoryStoryHooks", typeof(FactoryStoryHooks)),
        ("UIManager", typeof(UIManager)),
    };

    [MenuItem("DungeonFront/Ensure Production Runtime Managers")]
    public static void EnsureFromMenu()
    {
        Ensure();
    }

    public static void Ensure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform root = FindOrCreateRoot();
        GridManager grid = Object.FindAnyObjectByType<GridManager>();
        PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        foreach ((string name, System.Type type) in Managers)
        {
            if (Object.FindAnyObjectByType(type) != null)
            {
                continue;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(root, false);
            Component component = go.AddComponent(type);
            TryAssign(component, "gridManager", grid);
            TryAssign(component, "playerInventory", inventory);
            TryAssign(component, "targetCanvas", canvas);
            Undo.RegisterCreatedObjectUndo(go, "Add " + name);
            Debug.Log($"[ProductionRuntimeManagersSetup] Added {name}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform FindOrCreateRoot()
    {
        GameObject existing = GameObject.Find("RuntimeManagers");
        if (existing != null)
        {
            return existing.transform;
        }

        GameObject root = new GameObject("RuntimeManagers");
        Undo.RegisterCreatedObjectUndo(root, "Create RuntimeManagers");
        return root.transform;
    }

    private static void TryAssign(Component component, string fieldName, Object value)
    {
        if (component == null || value == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference
            && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
