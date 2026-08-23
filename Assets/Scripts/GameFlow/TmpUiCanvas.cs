using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay 캔버스를 정수 배율에 가깝게 맞추고, TMP가 스케일에서 뭉개지지 않게 한다.
public static class TmpUiCanvas
{
    public const float ReferenceWidth = 1920f;
    public const float ReferenceHeight = 1080f;

    public static void Configure(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return;
        }

        canvas.pixelPerfect = true;
        if (canvas.GetComponent<TmpUiCanvasSnap>() == null)
        {
            canvas.gameObject.AddComponent<TmpUiCanvasSnap>();
        }
    }

    public static void ConfigureAll()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            Configure(canvases[i]);
        }
    }

    public static void Sharpen(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.extraPadding = true;
        if (!text.enableAutoSizing)
        {
            text.fontSize = Mathf.Round(text.fontSize);
        }
    }
}

public sealed class TmpUiCanvasSnap : MonoBehaviour
{
    private Canvas canvas;
    private CanvasScaler scaler;
    private int lastWidth;
    private int lastHeight;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        scaler = GetComponent<CanvasScaler>();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
        {
            return;
        }

        Apply();
    }

    private void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (scaler == null)
        {
            scaler = GetComponent<CanvasScaler>();
        }

        if (canvas == null)
        {
            return;
        }

        canvas.pixelPerfect = true;
        if (scaler == null || Screen.height <= 0 || Screen.width <= 0)
        {
            return;
        }

        float fit = Mathf.Min(Screen.width / TmpUiCanvas.ReferenceWidth, Screen.height / TmpUiCanvas.ReferenceHeight);
        int integerScale = Mathf.Max(1, Mathf.RoundToInt(fit));
        if (fit >= 0.98f && Mathf.Abs(fit - integerScale) <= 0.08f)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = integerScale;
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(TmpUiCanvas.ReferenceWidth, TmpUiCanvas.ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
    }
}
