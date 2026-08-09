using UnityEngine;

// GridManager와 함께 광석 타일맵 렌더러를 보장한다.
public static class GridResourceTilemapBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRenderer()
    {
        GridManager gridManager = Object.FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {
            return;
        }

        if (gridManager.GetComponent<GridResourceTilemapRenderer>() != null)
        {
            return;
        }

        gridManager.gameObject.AddComponent<GridResourceTilemapRenderer>();
    }
}
