using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Prepare 의뢰 목록 카드. 제목만 보이며 클릭 시 상세를 연다.
public class QuestListEntry : MonoBehaviour
{
    // 기존 대비 약 1/3 크기. 가로:세로 = 1:1.4
    public const float CellWidth = 108f;
    public const float CellHeight = CellWidth * 1.4f;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button button;
    [SerializeField] private Image panelImage;

    private Quest boundQuest;
    private Action<Quest> onSelected;

    public Quest BoundQuest => boundQuest;

    public void Bind(Quest quest, Action<Quest> selected)
    {
        boundQuest = quest;
        onSelected = selected;

        if (titleText != null)
        {
            titleText.text = quest != null ? quest.title : "";
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetSelected(bool selected)
    {
        if (panelImage == null)
        {
            panelImage = GetComponent<Image>();
        }

        if (panelImage == null)
        {
            return;
        }

        panelImage.color = selected
            ? new Color(1f, 0.96f, 0.88f, 1f)
            : Color.white;
    }

    private void HandleClick()
    {
        onSelected?.Invoke(boundQuest);
    }

    public static QuestListEntry Create(Transform parent)
    {
        GameObject root = new GameObject(
            "QuestListEntry",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(QuestListEntry));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(CellWidth, CellHeight);

        LayoutElement layoutElement = root.GetComponent<LayoutElement>();
        layoutElement.minWidth = CellWidth;
        layoutElement.minHeight = CellHeight;
        layoutElement.preferredWidth = CellWidth;
        layoutElement.preferredHeight = CellHeight;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        // 예전 AspectRatioFitter가 남아 있으면 제거한다.
        AspectRatioFitter aspect = root.GetComponent<AspectRatioFitter>();
        if (aspect != null)
        {
            UnityEngine.Object.Destroy(aspect);
        }

        Image background = root.GetComponent<Image>();
        UiPanelFrame.Apply(background, UiPanelFrame.Kind.Wide, 0.85f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.9f, 1f);
        colors.pressedColor = new Color(0.9f, 0.86f, 0.78f, 1f);
        colors.selectedColor = new Color(1f, 0.96f, 0.88f, 1f);
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        button.colors = colors;

        QuestListEntry entry = root.GetComponent<QuestListEntry>();
        entry.button = button;
        entry.panelImage = background;
        entry.titleText = CreateTitle(root.transform);

        return entry;
    }

    private static TMP_Text CreateTitle(Transform parent)
    {
        GameObject labelObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();

        const float inset = 10f;
        RectTransform titleRect = label.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(inset, inset);
        titleRect.offsetMax = new Vector2(-inset, -inset);

        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        TmpUiStyle.Apply(label, TmpUiStyle.Role.Caption);
        label.fontStyle = FontStyles.Bold;
        return label;
    }
}
