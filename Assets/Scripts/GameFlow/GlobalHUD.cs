using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using UnityEngine.InputSystem;

public class GlobalHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text questCountText;     // 의뢰: {ActiveQuests.Count}/3 표시용 Text
    [SerializeField] private TMP_Text machineCountText;   // 기계: {InInventoryCount} 표시용 Text

    private QuestManager questManager;

    private void OnEnable()
    {
        // 1. PlayerInventory 계약 API 구독 (기계 변경 시 HUD 갱신)
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnMachinesChanged += RefreshMachineSummary;
        }

        BindQuestManager();

        RefreshAll();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnMachinesChanged -= RefreshMachineSummary;
        }
        if (questManager != null)
        {
            questManager.OnQuestsChanged -= RefreshQuestSummary;
        }
    }

    private void Update()
    {
        BindQuestManager();
    }

    private void RefreshAll()
    {
        RefreshQuestSummary();
        RefreshMachineSummary();
    }

    /// <summary>
    /// 수락 의뢰 HUD 표시 업데이트
    /// </summary>
    private void RefreshQuestSummary()
    {
        if (questCountText == null) return;

        int count = questManager != null ? questManager.currentQuests.Count : 0;
        questCountText.text = $"의뢰: {count}/{QuestManager.MaxActiveQuestCount}";
    }

    /// <summary>
    /// 인벤토리 기계 요약 HUD 표시 업데이트
    /// </summary>
    private void RefreshMachineSummary()
    {
        if (machineCountText == null) return;

        if (PlayerInventory.Instance != null)
        {
            // 인벤토리에 존재하는 기계 리스트 개수를 가져옵니다.
            int inInventoryCount = PlayerInventory.Instance.GetInInventoryMachines().Count;
            machineCountText.text = $"기계: {inInventoryCount}";
        }
        else
        {
            machineCountText.text = "기계: 0";
        }
    }

    private void BindQuestManager()
    {
        QuestManager candidate = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
        if (candidate == questManager) return;
        if (questManager != null) questManager.OnQuestsChanged -= RefreshQuestSummary;
        questManager = candidate;
        if (questManager != null)
        {
            questManager.OnQuestsChanged += RefreshQuestSummary;
            RefreshQuestSummary();
        }
    }
}
