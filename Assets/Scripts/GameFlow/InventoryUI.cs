using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform itemContainer; 
    [SerializeField] private Transform machineContainer; 

    [Header("Prefabs / Templates")]
    [SerializeField] private Button machineSlotPrefab; 
    
    [SerializeField] private Text itemMockText; 

    private void OnEnable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnItemsChanged += RefreshItems;
            PlayerInventory.Instance.OnMachinesChanged += RefreshMachines;
        }

        InitializeInventory();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnItemsChanged -= RefreshItems;
            PlayerInventory.Instance.OnMachinesChanged -= RefreshMachines;
        }
    }

    private void InitializeInventory()
    {
        // 기본적으로 패널은 켜주되, 세부 버튼 제어는 Refresh 단계에서 진행합니다.
        if (inventoryPanel != null) inventoryPanel.SetActive(true);

        RefreshItems();
        RefreshMachines();
    }

    private void RefreshItems()
    {
        if (PlayerInventory.Instance == null) return;

        int itemCount = PlayerInventory.Instance.GetCount("Item01");

        if (itemMockText != null)
        {
            itemMockText.text = $"Item01: {itemCount}개";
        }
        else
        {
            Debug.Log($"[Mock Item] Item01 보유량: {itemCount}개");
        }
    }

    private void RefreshMachines()
    {
        if (machineContainer == null || machineSlotPrefab == null) return;

        // 1. 기존에 생성된 버튼 리스트를 먼저 깨끗하게 지웁니다.
        foreach (Transform child in machineContainer)
        {
            if (child.gameObject == machineSlotPrefab.gameObject) continue;
            Destroy(child.gameObject);
        }

        // 원본 템플릿 버튼도 숨깁니다.
        machineSlotPrefab.gameObject.SetActive(false);

        // 2. [단계 제한 체크] 만약 현재 준비(Prepare) 단계가 아니라면, 
        // 하단의 버튼 생성 코드를 타지 않고 여기서 함수를 종료하여 아무것도 안 보이게 만듭니다.
        // (※ 아래 주석을 풀고 프로젝트의 실제 GameFlow 매니저와 Phase 변수명으로 매칭해 주세요!)
        /*
        if (GameFlowController.Instance != null && GameFlowController.Instance.CurrentPhase != Phase.Prepare)
        {
            Debug.Log("[InventoryUI] Prepare 단계가 아니므로 기계 목록을 표시하지 않습니다.");
            return; 
        }
        */

        // 3. 준비(Prepare) 단계일 때만 아래 가짜 버튼 4개가 정상적으로 화면에 나타납니다.
        for (int i = 1; i <= 4; i++)
        {
            Button newSlot = Instantiate(machineSlotPrefab, machineContainer, false);
            newSlot.gameObject.SetActive(true); 

            RectTransform rt = newSlot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -(i - 1) * 45f);
            }

            var textComponent = newSlot.GetComponentInChildren<Text>();
            if (textComponent == null)
            {
                var tmpText = newSlot.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmpText != null) tmpText.text = $"Mock Machine {i}";
            }
            else
            {
                textComponent.text = $"Mock Machine {i}";
            }

            string mockInstanceId = $"mock_id_0{i}";
            newSlot.onClick.AddListener(() => OnMachineSlotClicked(mockInstanceId));
        }
    }

    private void OnMachineSlotClicked(string instanceId)
    {
        Debug.Log($"[Mock] 기계 슬롯 클릭됨 - InstanceID: {instanceId}");
    }
}