using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    // 해금된 노드 ID들을 저장하는 집합 (HashSet으로 빠른 검색 지원)
    private HashSet<string> unlockedNodeIds = new HashSet<string>();

    private void Awake()
    {
        // 싱글톤(Singleton) 세팅
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // GameSessionState의 OnNewGame 이벤트 구독 (새 게임 시작 시 해금 초기화)
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnNewGame += ResetUnlockedNodes;
        }

        // 게임 시작 시 해금 목록 리셋
        ResetUnlockedNodes();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.OnNewGame -= ResetUnlockedNodes;
        }
    }

    /// <summary>
    /// 저장되어 있던 모든 테크 해금 내역을 초기화합니다.
    /// </summary>
    public void ResetUnlockedNodes()
    {
        unlockedNodeIds.Clear();
        Debug.Log("[UnlockManager] 테크 해금 목록이 초기화되었습니다.");
    }

    /// <summary>
    /// 해당 노드가 해금되었는지 여부를 확인합니다.
    /// </summary>
    public bool IsUnlocked(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        return unlockedNodeIds.Contains(nodeId);
    }

    /// <summary>
    /// 해당 노드가 해금 가능한 상태인지(선행 노드 해금 여부) 확인합니다.
    /// </summary>
    public bool CanUnlock(TechNodeSO node)
    {
        if (node == null) return false;

        // 이미 해금된 노드라면 해금 불가
        if (IsUnlocked(node.techId)) return false;

        // 선행 노드가 있고, 선행 노드가 아직 해금되지 않았다면 해금 불가
        if (node.parentNode != null && !IsUnlocked(node.parentNode.techId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 기술 해금을 시도합니다. (선행 조건 및 재화 검사 후 차감)
    /// </summary>
    public bool TryUnlock(TechNodeSO node, ref int currentGold, ref int currentReputation)
    {
        if (node == null) return false;

        // 1. 이미 해금된 기술인지 검사
        if (IsUnlocked(node.techId))
        {
            Debug.LogWarning($"[UnlockManager] 이미 해금된 기술입니다: {node.techName}");
            return false;
        }

        // 2. 선행 조건(부모 노드) 검사
        if (!CanUnlock(node))
        {
            Debug.LogWarning($"[UnlockManager] 선행 기술이 해금되지 않았습니다: {node.parentNode?.techName}");
            return false;
        }

        // 3. 재화(골드, 명성) 부족 여부 검사
        if (currentGold < node.requiredGold || currentReputation < node.requiredReputation)
        {
            Debug.LogWarning($"[UnlockManager] 재화가 부족합니다. (필요 Gold: {node.requiredGold}, Rep: {node.requiredReputation})");
            return false;
        }

        // 4. 차감 및 해금 처리
        currentGold -= node.requiredGold;
        currentReputation -= node.requiredReputation;
        unlockedNodeIds.Add(node.techId);

        Debug.Log($"<color=green>[UnlockManager] {node.techName} 기술 해금 성공!</color>");
        return true;
    }
}