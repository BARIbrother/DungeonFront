## HUD — 의뢰·인벤 요약

> **담당**: Dev1 · `Assets/Scripts/GameFlow/`  
> **계약**: [dev-contract.md](../../dev-contract.md)

### 이 작업물

`GameSessionState`·`QuestManager` 이벤트 구독으로 의뢰·인벤 요약 표시.

**코드**: `Assets/Scripts/GameFlow/` (예: `GlobalHUD.cs`)

### 추가 표시

| UI | 데이터 |
|----|--------|
| 수락 의뢰 | `의뢰: {ActiveQuests.Count}/3` |
| (선택) 첫 의뢰 요약 | `ActiveQuests[0].title` truncate |
| (선택) 인벤 기계 | `기계: {InInventoryCount}` |

### Dev2 계약 (읽기·이벤트만)

- [ ] `QuestManager.ActiveQuests` 읽기
- [ ] `QuestManager.OnQuestAccepted` · `OnQuestsChanged` 구독
- [ ] `QuestManager` 내부 수정 금지

### PlayerInventory 계약

- [ ] `OnMachinesChanged` 구독 → `기계: N`
- [ ] `OnItemsChanged` 구독 *(Dev1 W2에 추가)* → 아이템 요약 (선택)

### 독립 개발 (Mock)

- [ ] Dev2 미완: `의뢰: 0/3` 하드코딩 + 디버그 키 토글

### 완료 기준

- [ ] Dev2 수락 후 HUD `의뢰: 1/3`
- [ ] Lead 배치 후 `기계: N` 갱신
