using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>준비/정산 퀘스트 목록이 같은 크기와 여백을 사용하도록 정리합니다.</summary>
public static class QuestListLayoutFinalizer
{
    public static void Apply(Transform content, GameObject panel, string heading)
    {
        if (content == null || panel == null) return;

        RectTransform panelRect = panel.transform as RectTransform;
        RectTransform rootRect = panelRect != null ? panelRect.parent as RectTransform : null;
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.one;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = Vector2.one;
            // 게임 화면을 가리지 않도록 우측 상단의 컴팩트한 의뢰 패널로 고정한다.
            rootRect.anchoredPosition = new Vector2(-20f, -20f);
            rootRect.sizeDelta = new Vector2(360f, 300f);
        }

        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null) panelImage.color = new Color(0.10f, 0.13f, 0.18f, 0.97f);

        Transform header = panel.transform.Find("Header");
        if (header != null)
        {
            RectTransform headerRect = header as RectTransform;
            if (headerRect != null)
            {
                headerRect.anchorMin = new Vector2(0f, 1f);
                headerRect.anchorMax = new Vector2(1f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.anchoredPosition = new Vector2(0f, -14f);
                headerRect.sizeDelta = new Vector2(-36f, 52f);
            }

            TMP_Text headerText = header.GetComponent<TMP_Text>();
            if (headerText != null)
            {
                headerText.text = heading;
                headerText.color = Color.white;
                headerText.fontStyle = FontStyles.Bold;
                headerText.fontSize = 28f;
                headerText.enableAutoSizing = true;
                headerText.fontSizeMin = 20f;
                headerText.fontSizeMax = 28f;
                headerText.alignment = TextAlignmentOptions.MidlineLeft;
                headerText.raycastTarget = false;
            }
        }

        RectTransform contentRect = content as RectTransform;
        RectTransform viewportRect = contentRect != null ? contentRect.parent as RectTransform : null;
        if (viewportRect != null)
        {
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12f, 12f);
            viewportRect.offsetMax = new Vector2(-12f, -64f);
        }

        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
        }

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 16);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = panel.GetComponent<ScrollRect>();
        if (scroll != null)
        {
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
        }

        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    public static void ShowEmptyState(Transform content, string message)
    {
        if (content == null) return;

        GameObject empty = new GameObject("EmptyQuestMessage", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        empty.transform.SetParent(content, false);
        empty.AddComponent<GeneratedQuestCard>();

        LayoutElement element = empty.GetComponent<LayoutElement>();
        element.minHeight = 120f;
        element.preferredHeight = 120f;

        TextMeshProUGUI text = empty.GetComponent<TextMeshProUGUI>();
        text.text = message;
        text.color = new Color(0.78f, 0.84f, 0.93f, 1f);
        text.fontSize = 18f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }
}
