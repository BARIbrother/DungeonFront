using System;
using UnityEngine;

// Dev2 코드가 팀의 GameSessionState 구현 세부사항에 직접 퍼지지 않게 하는 얇은 어댑터.
// 팀 세션이 있는 실제 게임에서는 세션의 골드·명성을 사용하고,
// 독립 테스트 씬에서는 아래 fallback 값으로 똑같은 로직을 시험한다.
public class Week3EconomyService : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingGold;
    [SerializeField, Min(0)] private int startingReputation;

    private int fallbackGold;
    private int fallbackReputation;

    public event Action OnEconomyChanged;

    public int Gold => GameSessionState.Instance != null
        ? GameSessionState.Instance.gold
        : fallbackGold;

    public int Reputation => GameSessionState.Instance != null
        ? GameSessionState.Instance.reputation
        : fallbackReputation;

    private void Awake()
    {
        fallbackGold = startingGold;
        fallbackReputation = startingReputation;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
        {
            return false;
        }

        AddGold(-amount);
        return true;
    }

    public bool TrySpendReputation(int amount)
    {
        if (amount < 0 || Reputation < amount)
        {
            return false;
        }

        AddReputation(-amount);
        return true;
    }

    public void AddGold(int amount)
    {
        if (GameSessionState.Instance != null)
        {
            int next = Mathf.Max(0, GameSessionState.Instance.gold + amount);
            GameSessionState.Instance.AddGold(next - GameSessionState.Instance.gold);
        }
        else
        {
            fallbackGold = Mathf.Max(0, fallbackGold + amount);
        }

        OnEconomyChanged?.Invoke();
    }

    public void AddReputation(int amount)
    {
        if (GameSessionState.Instance != null)
        {
            int next = Mathf.Max(0, GameSessionState.Instance.reputation + amount);
            GameSessionState.Instance.AddReputation(
                next - GameSessionState.Instance.reputation);
        }
        else
        {
            fallbackReputation = Mathf.Max(0, fallbackReputation + amount);
        }

        OnEconomyChanged?.Invoke();
    }

    public void Restore(int gold, int reputation)
    {
        if (GameSessionState.Instance != null)
        {
            GameSessionState.Instance.AddGold(gold - GameSessionState.Instance.gold);
            GameSessionState.Instance.AddReputation(
                reputation - GameSessionState.Instance.reputation);
        }
        else
        {
            fallbackGold = Mathf.Max(0, gold);
            fallbackReputation = Mathf.Max(0, reputation);
        }

        OnEconomyChanged?.Invoke();
    }
}
