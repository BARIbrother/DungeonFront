using System;

// Factory·세션 흐름에서 스토리 이벤트 id를 발행한다. Dev1은 OnStoryEvent만 구독한다.
public static class StoryEventBus
{
    public static event Action<string> OnStoryEvent;

    public static void Raise(string eventId) => OnStoryEvent?.Invoke(eventId);

    public static void RaiseMock(string eventId) => OnStoryEvent?.Invoke(eventId);
}
