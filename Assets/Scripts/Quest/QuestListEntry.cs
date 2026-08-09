using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Prepare 의뢰 목록의 한 줄. 제목·납기만 보이고 클릭 시 상세를 연다.
public class QuestListEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text deadlineText;
    [SerializeField] private Button button;

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

        if (deadlineText != null)
        {
            deadlineText.text = quest != null
                ? QuestCard.FormatDeadline(quest)
                : "";
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
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = selected
            ? new Color(0.35f, 0.45f, 0.7f, 1f)
            : Color.white;
        button.colors = colors;
    }

    private void HandleClick()
    {
        onSelected?.Invoke(boundQuest);
    }

    // 런타임 목록 행을 만든다.
    public static QuestListEntry Create(Transform parent)
    {
        GameObject root = new GameObject(
            "QuestListEntry",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(QuestListEntry));
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(0f, 48f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.15f, 0.18f, 0.28f, 0.95f);

        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;

        QuestListEntry entry = root.GetComponent<QuestListEntry>();
        entry.button = button;
        entry.titleText = CreateLabel(root.transform, "Title", TextAlignmentOptions.Left, 18f);
        entry.deadlineText = CreateLabel(root.transform, "Deadline", TextAlignmentOptions.Right, 14f);

        RectTransform titleRect = entry.titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.7f, 1f);
        titleRect.offsetMin = new Vector2(10f, 4f);
        titleRect.offsetMax = new Vector2(-4f, -4f);

        RectTransform deadlineRect = entry.deadlineText.rectTransform;
        deadlineRect.anchorMin = new Vector2(0.7f, 0f);
        deadlineRect.anchorMax = new Vector2(1f, 1f);
        deadlineRect.offsetMin = new Vector2(4f, 4f);
        deadlineRect.offsetMax = new Vector2(-10f, -4f);

        return entry;
    }

    private static TMP_Text CreateLabel(
        Transform parent,
        string name,
        TextAlignmentOptions alignment,
        float fontSize)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.alignment = alignment;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }
}
