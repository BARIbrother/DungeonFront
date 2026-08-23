using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// 빌드에서 UI Bootstrap 순서가 달라 EventSystem이 중복되면 클릭이 먹통이 될 수 있다.
// 활성 EventSystem 하나만 남기고 InputSystemUIInputModule을 보장한다.
public static class UiEventSystem
{
    public static void Ensure()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        EventSystem keep = ChoosePrimary(systems);

        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system == null || system == keep)
            {
                continue;
            }

            Object.Destroy(system.gameObject);
        }

        if (keep == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(eventSystemObject);
            return;
        }

        if (!keep.gameObject.activeInHierarchy)
        {
            keep.gameObject.SetActive(true);
        }

        if (keep.GetComponent<InputSystemUIInputModule>() == null)
        {
            keep.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static EventSystem ChoosePrimary(EventSystem[] systems)
    {
        EventSystem fallback = null;

        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = system;
            }

            if (!system.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (system.gameObject.scene.name == "DontDestroyOnLoad")
            {
                return system;
            }

            if (fallback == null || !fallback.gameObject.activeInHierarchy)
            {
                fallback = system;
            }
        }

        return fallback;
    }
}
