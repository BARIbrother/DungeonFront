## 의뢰 출현 풀 (명성)

> **담당**: Dev2 · `Assets/Scripts/Quest/`  
> **권장 순서**: Dev2 **2번째** ([01-quest-assets-v1](./01-quest-assets-v1.md) 후)

### 이 작업물

Prepare **의뢰 목록**에 **어떤 Quest SO가 뜨는지** 결정. `01-core-loop.md` 누적 명성 threshold.

**코드**: `Assets/Scripts/Quest/` (예: `QuestPoolService.cs`)  
**연동**: `QuestManager.AvailableQuestsToday` · `RefreshAvailableQuests()`

### Week 2 규격

| 항목 | 내용 |
|------|------|
| 입력 | `GameSessionState.Reputation`, `Day` (읽기), [01-quest-assets-v1](./01-quest-assets-v1.md) Quest SO 목록 |
| 필터 | 명성 threshold (Week 2는 **하드코드 표** OK) |
| 출력 | `AvailableQuestsToday` 갱신 |
| Week 2 최소 | 명성 0 → 튜토 Quest **항상** + (선택) 2~3일차 1개 |

### Dev1 계약

- [ ] `GameSessionState` **읽기만** — [dev-contract.md](../../dev-contract.md)
- [ ] `OnPhaseChanged(Prepare)` 구독 시 `RefreshAvailableQuests()` 호출 (Dev2 자체 구독)

### 구현

- [ ] Prepare 진입 시 `RefreshAvailableQuests()`
- [ ] 이미 수락한 의뢰는 목록에서 제외

### 독립 개발 (Mock)

- [ ] `GameSessionState` 없을 때: reputation=0, day=1 하드코딩

### 완료 기준

- [ ] NewGame(명성 0) → `AvailableQuestsToday`에 튜토 Quest 포함
- [ ] 디버그로 명성 올리면 추가 Quest 출현
