## 의뢰 에셋 v1

### 이 작업물

**기존** `Quest` SO·`QuestManager`에 **1~3일차 의뢰 에셋**을 채우고 UI·풀과 연결한다.  
클래스·SO 스키마 정의는 **하지 않음** — 이미 `Assets/Scripts/Quest/`에 있음.

**기존 코드**: `Quest.cs`, `QuestManager.cs`  
**기획**: Lead [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md)

### Week 2 에셋 (최소)

| 에셋 | 비고 |
|------|------|
| 튜토 의뢰 1개 | `title`·`requiredItems`·`rewards`·`deadlineDays` — `00100001` 규칙 |
| (선택) 2~3일차 1~2개 | 기획 v1 있으면 추가 |

### 작업

- [ ] `Assets/Data/Quests/` 등에 **Quest SO 에셋** 생성
- [ ] `requiredItems`·`rewards` — `ItemEntry` / `Item` SO 참조 연결
- [ ] `deadlineDays` — 당일 = 1 (`Quest.currentleftDeadlineDays`는 수락 시 `QuestManager`가 설정)
- [ ] [05-quest-pool](./05-quest-pool.md) → `QuestManager.availableQuestsToday` 채우기
- [ ] [02-quest-accept-ui](./02-quest-accept-ui.md) → `QuestManager.acceptQuest`

### 범위 밖

- `QuestDefinition`·`QuestDatabase` 신규 클래스 (`02-data-structure.md`와 별도 — 후속 통합 시 검토)
- 납품·보상 실행 (Week 3~ `progressQuest` UI 연동)

### 완료 기준

- [ ] 튜토 Quest SO 에셋 1개, 필드 채움
- [ ] Prepare에서 `availableQuestsToday`에 노출
- [ ] 수락 UI·결산 목록에서 `title`·요구·보상 요약 표시 가능
