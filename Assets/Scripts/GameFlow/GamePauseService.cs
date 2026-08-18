using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 UI가 동시에 게임을 멈춰도, 마지막 UI가 닫힐 때만 시간을 복구하는 공용 일시정지 서비스입니다.
/// </summary>
public static class GamePauseService
{
    private static readonly HashSet<string> requesters = new();
    private static float timeScaleBeforePause = 1f;

    public static bool IsPaused => requesters.Count > 0;
    public static event Action<bool> OnPauseChanged;

    public static void RequestPause(string requester)
    {
        if (string.IsNullOrWhiteSpace(requester) || !requesters.Add(requester))
        {
            return;
        }

        if (requesters.Count == 1)
        {
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }
    }

    public static void ReleasePause(string requester)
    {
        if (string.IsNullOrWhiteSpace(requester) || !requesters.Remove(requester) || requesters.Count != 0)
        {
            return;
        }

        Time.timeScale = timeScaleBeforePause;
        OnPauseChanged?.Invoke(false);
    }
}
