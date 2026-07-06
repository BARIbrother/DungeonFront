## 의뢰 수락 UI

> **담당**: Dev2 · `Assets/Scripts/Quest/`  
> **권장 순서**: Dev2 **3번째** ([01-quest-assets-v1](./01-quest-assets-v1.md) → [02-quest-pool](./02-quest-pool.md) 후)

### 이 작업물

Prepare 단계 **의뢰 목록·수락**. 하루 **최대 3개** (`01-core-loop.md`).

**코드**: `Assets/Scripts/Quest/` (예: `QuestAcceptUI.cs`)

### 규격

| 항목 | 내용 |
|------|------|
| 표시 | `QuestManager.AvailableQuestsToday` |
| 수락 | 클릭 → `QuestManager.TryAccept(questId)` |
| 상한 | `ActiveQuests.Count >= 3` → 수락 비활성 |
| 납기 | `deadlineDays` / `currentleftDeadlineDays` 표시 |

### UI 표시 (Dev1 비의존)

- [ ] `GameSessionState.OnPhaseChanged` **자체 구독** — `Prepare`일 때만 패널 활성
- [ ] Dev1 `orderWindow`·`GameObject.Find` **사용 금지** ([dev-contract.md](../../dev-contract.md))
- [ ] Dev1 캔버스 없을 때: 독립 테스트 씬에 UI 배치

### Dev1 계약

- [ ] 수락 성공 시 `OnQuestAccepted(activeCount)` 발행 — Dev1 HUD가 구독
- [ ] Dev1 `GameFlow/` 수정 금지

### 독립 개발 (Mock)

- [ ] 풀 미완 시: 튜토 Quest 1개 하드코딩 목록

### 없어도 되는 것 (Week 3~)

- 결산 납품·보상·미납 처리

### 완료 기준

- [ ] Prepare에서 의뢰 1개 이상 목록 표시
- [ ] 수락 후 `ActiveQuests` 반영, 3개 초과 불가
- [ ] `OnQuestAccepted` 발행 확인
