using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TechTreeUI : MonoBehaviour
{
    [Header("[패널 연결]")]
    [SerializeField] private GameObject techTreePanel;
    [SerializeField] private GameObject confirmPopupPanel;

    [Header("[팝업 UI 텍스트/버튼 연결]")]
    [SerializeField] private TMP_Text popupTitleText;
    [SerializeField] private TMP_Text popupDescText;
    [SerializeField] private TMP_Text popupCostText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private TechNodeSO selectedNode;

    private void Start()
    {
        // 평소 및 게임 시작 시 팝업과 패널 비활성화
        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
        if (techTreePanel != null) techTreePanel.SetActive(false);
    }

    // 런타임/에디터 복사 후 참조를 연결할 때 사용한다.
    public void Bind(
        GameObject treePanel,
        GameObject confirmPopup,
        TMP_Text title,
        TMP_Text desc,
        TMP_Text cost,
        Button confirm,
        Button cancel)
    {
        techTreePanel = treePanel;
        confirmPopupPanel = confirmPopup;
        popupTitleText = title;
        popupDescText = desc;
        popupCostText = cost;
        confirmButton = confirm;
        cancelButton = cancel;
    }

    // 테크 트리 전체 패널 열기/닫기 토글
    public void ToggleTechTreePanel()
    {
        if (techTreePanel != null)
        {
            bool isActive = !techTreePanel.activeSelf;
            techTreePanel.SetActive(isActive);

            // 패널을 닫을 때 팝업창도 같이 닫기
            if (!isActive && confirmPopupPanel != null)
            {
                confirmPopupPanel.SetActive(false);
            }
        }
    }

    // 테크 노드 버튼 클릭 시 호출
    public void OnClickTechNode(TechNodeSO node)
    {
        if (node == null) return;
        selectedNode = node;

        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(true);

        // 1. 이미 해금되었는지 확인
        if (UnlockManager.Instance != null && UnlockManager.Instance.IsUnlocked(node.techId))
        {
            if (popupTitleText != null) popupTitleText.text = node.techName;
            if (popupDescText != null) popupDescText.text = "이미 해금된 기술입니다.";
            if (popupCostText != null) popupCostText.text = "";
            if (confirmButton != null) confirmButton.interactable = false;
            return;
        }

        // 2. 선행 기술 해금 여부 확인
        if (UnlockManager.Instance != null && !UnlockManager.Instance.CanUnlock(node))
        {
            string parentName = node.parentNode != null ? node.parentNode.techName : "선행 기술";
            if (popupTitleText != null) popupTitleText.text = node.techName;
            if (popupDescText != null) popupDescText.text = $"<color=red>선행 기술 [{parentName}]을(를)\n먼저 해금해야 합니다.</color>";
            if (popupCostText != null) popupCostText.text = $"비용: {node.requiredGold}Gold / {node.requiredReputation}Rep";
            if (confirmButton != null) confirmButton.interactable = false;
            return;
        }

        // 3. 해금 가능 상태 - 비용 및 정보 출력
        if (popupTitleText != null) popupTitleText.text = node.techName;
        if (popupDescText != null) popupDescText.text = node.description;
        if (popupCostText != null) popupCostText.text = $"비용: {node.requiredGold}Gold / {node.requiredReputation}Rep\n해금하시겠습니까?";
        if (confirmButton != null) confirmButton.interactable = true;
    }

    // 팝업 내 [확인/해금] 버튼 클릭 시
    public void OnConfirmUnlock()
    {
        if (selectedNode == null) return;

        int currentGold = GameSessionState.Instance != null ? GameSessionState.Instance.gold : 9999;
        int currentReputation = GameSessionState.Instance != null ? GameSessionState.Instance.reputation : 9999;

        if (UnlockManager.Instance != null && UnlockManager.Instance.TryUnlock(selectedNode, ref currentGold, ref currentReputation))
        {
            if (GameSessionState.Instance != null)
            {
                int costGold = GameSessionState.Instance.gold - currentGold;
                int costReputation = GameSessionState.Instance.reputation - currentReputation;

                // 재화 차감 및 초록색 UI 실시간 갱신
                GameSessionState.Instance.AddGold(-costGold);
                GameSessionState.Instance.AddReputation(-costReputation);
            }

            Debug.Log($"[TechUI] {selectedNode.techName} 해금 완료!");
        }

        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
    }

    // 팝업 내 [취소] 버튼 클릭 시
    public void OnCancelPopup()
    {
        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
        selectedNode = null;
    }
}