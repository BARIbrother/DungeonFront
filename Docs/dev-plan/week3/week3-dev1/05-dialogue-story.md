## 대화 UI·스토리 연출

### 이 작업물

**대화 UI** + Lead **스토리 이벤트** 구독. 구 7주 4~6주 「대화 UI」「스토리 이벤트 연동」.

**Lead 훅**: [week3-lead 06-story-hooks](../week3-lead/06-story-hooks.md)  
**아트**: `Assets/Art/UI/Portraits/` — placeholder 허용

**코드**: `Assets/Scripts/GameFlow/DialogueUI.cs`, `StoryEventListener.cs`

### UI

- [ ] 초상화 슬롯·이름·본문·**다음**
- [ ] 재생 중 게임·틱 일시정지 (튜토와 동일 패턴)
- [ ] 엔딩 — 대화만 (별도 씬 불필요)

### 계약

```csharp
// Lead 발행 — Dev1 구독만
StoryEventBus.OnStoryEvent += (eventId) => ShowDialogue(eventId);
```

### 독립 개발

- [ ] `StoryEventBus.RaiseMock("day1_opening")` 로 UI 개발
- [ ] 초상화 없으면 placeholder 색상 박스

### 완료 기준

- [ ] 1일차 오프닝 또는 필수 의뢰 전후 대화 1회 이상 표시
- [ ] Lead `StoryEventBus` 실이벤트 연동 (금요일)
