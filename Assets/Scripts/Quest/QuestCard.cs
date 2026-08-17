using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestCard : MonoBehaviour
{
    private const float CardHeight = 148f;

    [SerializeField] private Quest quest;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text clientNameText;
    [SerializeField] private TMP_Text deadlineText;
    [SerializeField] private TMP_Text requireText;
    [SerializeField] private TMP_Text rewardsText;
    [SerializeField] private Button acceptButton;

    private PlayerInventory observedInventory;

    private void Awake() => ApplyFinalLayout();

    private void OnEnable()
    {
        ApplyFinalLayout();
        BindInventory();
    }

    private void Update()
    {
        if (observedInventory == null)
        {
            BindInventory();
        }
    }

    private void OnDisable()
    {
        if (observedInventory != null)
        {
            observedInventory.OnItemsChanged -= RefreshRequirementText;
        }

        observedInventory = null;
    }

    private void Start()
    {
        if (quest != null)
        {
            SetQuest(quest);
        }
    }

    public void SetQuest(Quest newQuest)
    {
        quest = newQuest;
        ApplyFinalLayout();
        if (quest == null) return;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(quest.title) ? "이름 없는 의뢰" : quest.title;
        }

        if (clientNameText != null)
        {
            string client = string.IsNullOrWhiteSpace(quest.clientName) ? "알 수 없음" : quest.clientName;
            clientNameText.text = $"<color=#9AA8BD>의뢰인</color>\n{client}";
        }

        if (deadlineText != null)
        {
            deadlineText.text = $"<color=#9AA8BD>마감</color>\n{FormatDeadline(quest)}";
        }

        RefreshRequirementText();
        if (rewardsText != null && rewardsText != requireText)
        {
            rewardsText.text = "<color=#9AA8BD>보상</color>\n" + MakeItemString(quest.rewards);
        }
    }

    public static string FormatDeadline(Quest target)
    {
        return target == null || target.currentleftDeadlineDays <= 0
            ? "오늘"
            : $"D-{target.currentleftDeadlineDays}";
    }

    public void SetAcceptButtonInteractable(bool interactable)
    {
        if (acceptButton != null) acceptButton.interactable = interactable;
    }

    public void SetAcceptAction(UnityEngine.Events.UnityAction action)
    {
        if (acceptButton == null) return;
        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(action);
    }

    public void SetButtonLabel(string label)
    {
        TMP_Text text = acceptButton != null ? acceptButton.GetComponentInChildren<TMP_Text>() : null;
        if (text != null) text.text = label;
    }

    private void ApplyFinalLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(0f, CardHeight);
        }

        LayoutElement element = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        element.minHeight = CardHeight;
        element.preferredHeight = CardHeight;
        element.flexibleHeight = 0f;

        Image background = GetComponent<Image>();
        if (background != null) background.color = new Color(0.075f, 0.094f, 0.13f, 0.98f);

        ConfigureText(titleText, new Vector2(0.025f, 0.68f), new Vector2(0.55f, 0.96f), 21f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        ConfigureText(clientNameText, new Vector2(0.56f, 0.68f), new Vector2(0.76f, 0.96f), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        ConfigureText(deadlineText, new Vector2(0.77f, 0.68f), new Vector2(0.96f, 0.96f), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        ConfigureText(requireText, new Vector2(0.025f, 0.08f), new Vector2(0.48f, 0.65f), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        ConfigureText(rewardsText, new Vector2(0.50f, 0.08f), new Vector2(0.75f, 0.65f), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        if (acceptButton == null) return;

        SetStretchRect(acceptButton.transform as RectTransform, new Vector2(0.78f, 0.15f), new Vector2(0.96f, 0.58f));
        Image buttonImage = acceptButton.GetComponent<Image>();
        if (buttonImage != null) buttonImage.color = new Color(0.19f, 0.43f, 0.78f, 1f);

        TMP_Text buttonText = acceptButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            ConfigureText(buttonText, Vector2.zero, Vector2.one, 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            buttonText.color = Color.white;
        }
    }

    private static void ConfigureText(TMP_Text text, Vector2 anchorMin, Vector2 anchorMax, float maximumSize, FontStyles style, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        SetStretchRect(text.transform as RectTransform, anchorMin, anchorMax);
        text.color = Color.white;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = 11f;
        text.fontSizeMax = maximumSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.margin = new Vector4(4f, 3f, 4f, 3f);
    }

    private static void SetStretchRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rect == null) return;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private void BindInventory()
    {
        PlayerInventory inventory = PlayerInventory.Instance != null
            ? PlayerInventory.Instance
            : FindAnyObjectByType<PlayerInventory>();
        if (inventory == observedInventory) return;

        if (observedInventory != null) observedInventory.OnItemsChanged -= RefreshRequirementText;
        observedInventory = inventory;
        if (observedInventory != null) observedInventory.OnItemsChanged += RefreshRequirementText;
        RefreshRequirementText();
    }

    private void RefreshRequirementText()
    {
        if (requireText != null && quest != null)
        {
            requireText.text = "<color=#9AA8BD>요구 재료 (보유/필요)</color>\n" + MakeRequirementString(quest.requiredItems);
        }
    }

    private string MakeRequirementString(ItemEntryList list)
    {
        if (list == null || list.entries == null) return "없음";

        StringBuilder builder = new StringBuilder();
        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null) continue;

            string itemName = string.IsNullOrWhiteSpace(entry.item.displayName) ? entry.item.id : entry.item.displayName;
            int owned = observedInventory != null ? observedInventory.GetCount(entry.item.id) : 0;
            bool enough = owned >= entry.count;

            builder.Append(enough ? "<color=#77DB8A>" : "<color=#FF8C95>");
            builder.Append("• ");
            builder.Append(itemName);
            builder.Append("  ");
            builder.Append(owned);
            builder.Append('/');
            builder.Append(entry.count);
            builder.Append("</color>\n");
        }

        return builder.Length > 0 ? builder.ToString().TrimEnd() : "없음";
    }

    private static string MakeItemString(ItemEntryList list)
    {
        if (list == null || list.entries == null) return "없음";

        StringBuilder builder = new StringBuilder();
        foreach (ItemEntry entry in list.entries)
        {
            if (entry == null || entry.item == null) continue;
            builder.Append("• ");
            builder.Append(string.IsNullOrWhiteSpace(entry.item.displayName) ? entry.item.id : entry.item.displayName);
            builder.Append(" ×");
            builder.Append(entry.count);
            builder.Append('\n');
        }

        return builder.Length > 0 ? builder.ToString().TrimEnd() : "없음";
    }
}

// 동적으로 생성된 카드만 안전하게 골라 지우기 위한 표식입니다.
public sealed class GeneratedQuestCard : MonoBehaviour
{
}
