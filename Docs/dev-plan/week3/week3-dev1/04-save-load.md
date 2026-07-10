## JSON 세이브·로드

### 이 작업물

W2 [SaveData DTO](../../week2/week2-dev1/04-save-dto-freeze.md) 기반 **JSON 저장·불러오기**.  
Title [05-title-shell](../../week2/week2-dev1/05-title-shell.md) 슬롯 연동.

**코드**: `Assets/Scripts/GameFlow/SaveLoad.cs`

### 범위

| 필드 | W3 담당 |
|------|---------|
| `day`, `phase`, `gold`, `reputation` | Dev1 |
| `inventoryItems`, `machines` | Dev1 |
| `factory` | Dev1 — Lead `IFactorySave` 호출 (미구현 시 null) |
| `quests[]` | **Dev2** 직렬화 ([04-quest-serialize](../week3-dev2/04-quest-serialize.md)) |

### UI

- [ ] Title 슬롯 1~3 — 저장·불러오기
- [ ] 일차·필수 의뢰 클리어 시 **autosave** (선택 슬롯)

### 독립 개발

- [ ] `factory` — Lead `IFactorySave` 스텁 → null 저장으로 개발 진행
- [ ] `quests` — Dev2 `IQuestSaveProvider` 스텁 → `[]` 저장

### 완료 기준

- [ ] 저장 → 종료 → 불러오기 후 일차·골드·인벤 복원
- [ ] Lead 배치·Dev2 의뢰는 금요일 통합 후 재검증
