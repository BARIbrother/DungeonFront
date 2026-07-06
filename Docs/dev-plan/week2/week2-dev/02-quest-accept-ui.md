## 의뢰 수락 UI

### 이 작업물

Prepare 단계 **의뢰 목록·수락**. 하루 **최대 3개** (`01-core-loop.md`).

**코드**: `Assets/Scripts/` (예: `QuestAcceptUI.cs`)

### 규격

| 항목 | 내용 |
|------|------|
| 표시 | [05-quest-pool](./05-quest-pool.md)이 채운 `availableQuestsToday` |
| 수락 | 클릭 → `QuestManager.acceptQuest(quest)` |
| 상한 | `QuestManager.currentQuests.Count >= 3` → 수락 비활성 |
| 납기 | `deadlineDays` / `currentleftDeadlineDays` 표시 |

### 데이터

- [ ] [06-quest-assets-v1](./06-quest-assets-v1.md) 에셋
- [ ] `QuestManager.availableQuestsToday` 목록 표시
- [ ] 수락 → `QuestManager.acceptQuest(quest)`

### 없어도 되는 것 (후속)

- 결산 납품·보상·미납 처리

### 완료 기준

- [ ] Prepare에서 의뢰 1개 이상 목록 표시
- [ ] 수락 후 `currentQuests`에 반영, 3개 초과 수락 불가
