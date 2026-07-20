using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using UnityEngine.InputSystem;

public class GlobalHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text questCountText;     // 의뢰: {ActiveQuests.Count}/3 표시용 Text
    [SerializeField] private TMP_Text machineCountText;   // 기계: {InInventoryCount} 표시용 Text

    [Header("Mock Debug Settings")]
    private bool isDebugQuestActive = false; // 디버그 키 토글용 변수

    private void OnEnable()
    {
        // 1. PlayerInventory 계약 API 구독 (기계 변경 시 HUD 갱신)
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnMachinesChanged += RefreshMachineSummary;
        }

        // TODO: Dev2 연동 완료 시 아래 퀘스트 이벤트 구독 해제 주석 제거
        /*
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += RefreshQuestSummary;
            QuestManager.Instance.OnQuestsChanged += RefreshQuestSummary;
        }
        */

        RefreshAll();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnMachinesChanged -= RefreshMachineSummary;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            isDebugQuestActive = !isDebugQuestActive;
            Debug.Log($"[Mock] 퀘스트 디버그 토글 활성화: {isDebugQuestActive}");
            RefreshQuestSummary();
        }
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

        // 실제 Dev2 매니저가 연결되었는지 확인
        // if (QuestManager.Instance != null) { ... 실제 데이터 반영 ... }

        // [Mock 단계] 디버그 키 토글에 따라 완료 기준(1/3) 검증용 하드코딩 출력
        if (isDebugQuestActive)
        {
            questCountText.text = "의뢰: 1/3";
        }
        else
        {
            questCountText.text = "의뢰: 0/3";
        }
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
}