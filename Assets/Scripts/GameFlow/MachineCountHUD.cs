using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class MachineCountHUD : MonoBehaviour
{
    [Header("[기계 수량 TMP 텍스트 참조]")]
    [SerializeField] private TextMeshProUGUI machine1Text;
    [SerializeField] private TextMeshProUGUI machine2Text;
    [SerializeField] private TextMeshProUGUI machine3Text;
    [SerializeField] private TextMeshProUGUI machine4Text;

    [Header("[Machine 1 데이터 (디버그 테스트용)]")]
    public int m1MaxCount = 5;      // 최대 보유 가능 개수
    public int m1CurrentCount = 5;  // 현재 설치 가능한 남은 개수

    [Header("[Machine 2~4 데이터]")]
    public int m2MaxCount = 3;
    public int m2CurrentCount = 3;

    public int m3MaxCount = 2;
    public int m3CurrentCount = 2;

    public int m4MaxCount = 1;
    public int m4CurrentCount = 1;

    private void Start()
    {
        UpdateAllUI();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // 디버그 키 테스트: M키 누르면 Machine 1 설치 (수량 감소)
        if (keyboard.mKey.wasPressedThisFrame)
        {
            PlaceMachine1();
        }

        // 디버그 키 테스트: N키 누르면 Machine 1 회수 (수량 증가, 최대치 제한)
        if (keyboard.nKey.wasPressedThisFrame)
        {
            RecallMachine1();
        }
    }

    /// <summary>
    /// Machine 1 설치 시 호출 (M 키)
    /// </summary>
    public bool PlaceMachine1()
    {
        if (m1CurrentCount > 0)
        {
            m1CurrentCount--;
            UpdateUI(machine1Text, "Machine 1", m1CurrentCount, m1MaxCount);
            Debug.Log($"[Machine 1] 설치 완료! 남은 개수: {m1CurrentCount}/{m1MaxCount}");
            return true;
        }

        Debug.LogWarning("[Machine 1] 더 이상 설치할 수 있는 기계가 없습니다!");
        return false;
    }

    /// <summary>
    /// Machine 1 회수 시 호출 (N 키)
    /// </summary>
    public bool RecallMachine1()
    {
        if (m1CurrentCount < m1MaxCount)
        {
            m1CurrentCount++;
            UpdateUI(machine1Text, "Machine 1", m1CurrentCount, m1MaxCount);
            Debug.Log($"[Machine 1] 회수 완료! 남은 개수: {m1CurrentCount}/{m1MaxCount}");
            return true;
        }

        Debug.LogWarning("[Machine 1] 이미 최대 개수입니다. 더 이상 회수할 수 없습니다!");
        return false;
    }

    /// <summary>
    /// 전체 UI 텍스트 갱신
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateUI(machine1Text, "Machine 1", m1CurrentCount, m1MaxCount);
        UpdateUI(machine2Text, "Machine 2", m2CurrentCount, m2MaxCount);
        UpdateUI(machine3Text, "Machine 3", m3CurrentCount, m3MaxCount);
        UpdateUI(machine4Text, "Machine 4", m4CurrentCount, m4MaxCount);
    }

    private void UpdateUI(TextMeshProUGUI textTarget, string machineName, int current, int max)
    {
        if (textTarget != null)
        {
            textTarget.text = $"{machineName}: {current}/{max}";
        }
    }
}