using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 준비/생산 단계에서 수락한 의뢰를 항상 확인할 수 있는 작은 진행 의뢰 패널입니다.
/// 정산 단계에는 기존 제출 패널이 같은 정보를 담당하므로 숨깁니다.
/// </summary>
public sealed class ActiveQuestTrackerUI : MonoBehaviour
{
    private static ActiveQuestTrackerUI instance;
    private GameObject panel;
    private TMP_Text body;
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null) new GameObject(nameof(ActiveQuestTrackerUI)).AddComponent<ActiveQuestTrackerUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh();
    }

    private void Build()
    {
        GameObject canvasObject = new("ActiveQuestTrackerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        panel = new GameObject("ActiveQuestPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.055f, 0.08f, 0.12f, 0.96f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -112f);
        panelRect.sizeDelta = new Vector2(360f, 180f);

        GameObject textObject = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        body = textObject.GetComponent<TextMeshProUGUI>();
        body.font = KoreanTmpFontRuntimeFix.EnsureFont() ?? TMP_Settings.defaultFontAsset;
        body.color = Color.white;
        body.fontSize = 18f;
        body.enableAutoSizing = true;
        body.fontSizeMin = 13f;
        body.fontSizeMax = 18f;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.margin = new Vector4(16f, 14f, 16f, 12f);
        body.raycastTarget = false;
        RectTransform textRect = body.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void Refresh()
    {
        QuestManager manager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
        GameSessionState session = GameSessionState.Instance ?? FindAnyObjectByType<GameSessionState>();
        bool isSettlement = session != null && session.Phase == GamePhase.Settlement;
        bool hasQuest = manager != null && manager.currentQuests.Count > 0;
        if (panel == null) return;

        panel.SetActive(hasQuest && !isSettlement);
        if (!hasQuest || isSettlement || body == null) return;

        PlayerInventory inventory = PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>();
        StringBuilder builder = new StringBuilder("<b>진행 중인 의뢰</b>\n");
        foreach (Quest quest in manager.currentQuests)
        {
            if (quest == null) continue;
            builder.Append("<color=#AFC8F5>").Append(quest.title).Append("</color>  ");
            builder.Append(QuestCard.FormatDeadline(quest)).Append('\n');
            foreach (ItemEntry entry in quest.requiredItems?.entries ?? System.Array.Empty<ItemEntry>())
            {
                if (entry?.item == null) continue;
                int owned = inventory != null ? inventory.GetCount(entry.item.Id) : 0;
                bool enough = owned >= entry.count;
                string itemName = string.IsNullOrWhiteSpace(entry.item.DisplayName)
                    ? entry.item.Id
                    : entry.item.DisplayName;
                builder.Append(enough ? "<color=#78DA91>" : "<color=#FF9AA4>");
                builder.Append("• ").Append(itemName).Append(' ')
                    .Append(owned).Append('/').Append(entry.count).Append("</color>\n");
            }
        }
        builder.Append("<color=#AAB6C8>생산 완료 후 정산 단계에서 제출하세요.</color>");
        body.text = builder.ToString();
    }
}
