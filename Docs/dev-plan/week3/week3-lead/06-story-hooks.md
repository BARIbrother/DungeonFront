# 스토리 이벤트 트리거

> **역할**: Lead · **Week**: 3 · **Issue**: 06  
> **선행**: [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md) · [04-story.md](../../../04-story.md)  
> **수신**: Dev1 [05-dialogue-story](../week3-dev1/05-dialogue-story.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) — `StoryEventBus`

---

## 1. 이 작업물

Factory·세션 흐름에서 **스토리 이벤트 id**를 발행하는 `FactoryStoryHooks` (또는 동등)를 구현한다.  
Dev1은 `StoryEventBus.OnStoryEvent`만 구독해 대화 UI를 띄운다 — **Lead가 Dev1 UI를 직접 호출하지 않음**.

**코드**: `Assets/Scripts/GameFlow/StoryEventBus.cs`, `FactoryStoryHooks.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 아래 표의 이벤트 id 발행 | 대사 텍스트·초상 (Dev1 + `Docs/Story/`) |
| 1일차 Prepare~Settlement 튜토 구간 | 전체 40일 스토리 전부 (가능한 범위까지) |
| `OnMachinePlaced(type, grid)` | 컷신 카메라 (선택·Dev1) |

---

## 3. API (고정)

[dev-contract.md](../../dev-contract.md):

```csharp
public static class StoryEventBus
{
    public static event Action<string> OnStoryEvent;
    public static void Raise(string eventId) => OnStoryEvent?.Invoke(eventId);
    public static void RaiseMock(string eventId) => OnStoryEvent?.Invoke(eventId);
}
```

- **Lead만** `Raise` 호출
- `eventId` = 기획 **8자리+접두** 문자열 — [04-story.md](../../../04-story.md) `001E00001` 형식

---

## 4. 발행 이벤트 (코드 훅)

| Bus eventId (내부) | 발행 시점 | 페이로드 |
|--------------------|-----------|----------|
| `OnPrepareEntered` | `GamePhase.Prepare` 진입 (일차 포함) | `day` — 문자열에 포함 또는 별도 |
| `OnProductionStarted` | `StartProduction()` 직후 | — |
| `OnProductionEnded` | 생산 요약 직전/직후 | — |
| `OnMachinePlaced` | 배치 성공 | `machineTypeId`, grid 좌표 |

**기획 eventId 매핑**: Bus 이벤트 → 기획 id 변환 테이블을 `FactoryStoryHooks`에 유지.

예: 1일차 Prepare 첫 진입 → `Raise("001E00001")`

---

## 5. 1일차 이벤트 매핑 (최소 구현)

[04-story.md](../../../04-story.md) 마스터 표:

| eventId | 일차 | 페이즈 | 트리거 | npcId | 요약 |
|---------|------|--------|--------|-------|------|
| `001E00001` | 1 | Prepare | 1일차 Prepare **첫** 진입 | — | 오프닝 |
| `001E00002` | 1 | Prepare | `001E00001` 대화 종료 직후 | `eve` | 첫 의뢰 안내 |
| `001E00003` | 1 | Prepare | *(튜토 단계)* | | 배치/의뢰 강조 |
| `001E00004` | 1 | Production | 생산 시작 후 | | 생산 튜토 |
| `001E00005` | 1 | Settlement | 결산 진입 | | 납품 안내 |

**구현 패턴**:

```csharp
// GameSessionState.OnPhaseChanged 구독
void OnPhaseChanged(GamePhase phase) {
    if (phase == GamePhase.Prepare && day == 1 && !firedOpening)
        StoryEventBus.Raise("001E00001");
}
```

**연쇄**: Dev1 대화 종료 시 `StoryEventListener.OnDialogueClosed(eventId)` → Lead가 다음 id 발행 **또는** Dev1이 Bus에 `DialogueFinished` 역발행 — **팀 1안 선택**.  
MVP 최소: Lead가 `001E00001`만 발행, 나머지는 Dev1 튜토리얼 패널이 담당해도 됨.

---

## 6. 튜토~클리어 구간

[04-story.md](../../../04-story.md) 필수 의뢰·이벤트 표를 순회하며:

| 트리거 유형 | 예 |
|-------------|-----|
| 일차 + 페이즈 | 3일차 Prepare → `001E00006` (레이) |
| 의뢰 수락/완료 | `QuestManager` 이벤트 구독 (읽기만) |
| 기계 배치 | 첫 채굴기 배치 → 튜토 강조 |

**우선순위**: 1일차 5이벤트 > 레이 3일차 > 엔딩 트리거

---

## 7. 구현 단계

- [ ] `StoryEventBus.cs` — 정적 클래스, `Assets/Scripts/GameFlow/`
- [ ] `FactoryStoryHooks.cs` — `GameSessionState`, `PlacementController` 구독
- [ ] 1일차 `001E00001` Prepare 진입
- [ ] `OnProductionStarted` / `OnProductionEnded` — [04-recipe-ui-summary](./04-recipe-ui-summary.md) 연동
- [ ] `OnMachinePlaced` — `PlacementController` 배치 성공 콜백
- [ ] 기획 표와 **eventId 문자열** 1:1 대조 문서 (주석 또는 README)
- [ ] Dev1 테스트: `RaiseMock("001E00001")` 수신 확인

---

## 8. Dev1 연동

```csharp
// Dev1 — StoryEventListener.cs
StoryEventBus.OnStoryEvent += eventId => {
    var line = StoryDatabase.Get(eventId); // npcId, expression, text
    DialogueUI.Show(line);
};
```

대사 본문: `Docs/Story/001E00001-Start-Tutorial.md` 등 — Lead는 **id만** 맞춤.

---

## 9. 완료 기준

- [ ] `FactoryStoryHooks` 존재·씬에 배치
- [ ] 1일차 Prepare 진입 시 `001E00001` Dev1 수신
- [ ] 생산 시작/종료 훅 발행
- [ ] 기계 배치 훅 1회 이상 동작
- [ ] 튜토~클리어 표 기준 **구현된 행** 목록 팀 공유 (미구현 행 명시)

---

## 10. 관련 문서

- [04-story.md](../../../04-story.md)
- [week3-dev1/05-dialogue-story](../week3-dev1/05-dialogue-story.md)
- [week3-dev1/01-tutorial-panel](../week3-dev1/01-tutorial-panel.md)
