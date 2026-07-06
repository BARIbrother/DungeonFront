## 의뢰 출현 풀 (명성)

### 이 작업물

Prepare **의뢰 목록**에 **어떤 Quest SO가 뜨는지** 결정. `01-core-loop.md` 누적 명성 threshold.

**코드**: `Assets/Scripts/` (예: `QuestPoolService.cs`)  
**연동**: `QuestManager.availableQuestsToday`

### Week 2 규격

| 항목 | 내용 |
|------|------|
| 입력 | `session.reputation`, 등록된 `Quest` SO 에셋 목록 |
| 필터 | 명성 threshold (Week 2는 **하드코드 표** 또는 ScriptableObject 목록 순서 OK) |
| 출력 | `QuestManager.availableQuestsToday` 갱신 |
| Week 2 최소 | 명성 0 → 튜토 Quest **항상** + (선택) 2~3일차 1개 |

### 구현

- [ ] 일차 시작·Prepare 진입 시 `RefreshAvailableQuests()`
- [ ] 이미 수락한 의뢰는 목록에서 제외

### 완료 기준

- [ ] NewGame(명성 0) → `availableQuestsToday`에 튜토 Quest 포함
- [ ] 디버그로 명성 올리면 추가 Quest 출현 (에셋 있을 때)
