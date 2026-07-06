## HUD — 의뢰·인벤 요약

### 이 작업물

[week1 글로벌 HUD](../../week1/week1-dev/03-global-hud.md) 확장. Prepare·Production에서 **수락 의뢰·인벤** 한눈에.

**코드**: `Assets/Scripts/` (예: `GlobalHUD.cs` 확장)

### 추가 표시

| UI | 데이터 |
|----|--------|
| 수락 의뢰 | `의뢰: {QuestManager.currentQuests.Count}/3` |
| (선택) 첫 의뢰 요약 | `currentQuests[0].title` 1줄 truncate |
| (선택) 인벤 기계 | `기계: {InInventory count}` |

- [ ] `QuestManager`·`inventory` 변경 시 갱신
- [ ] Factory·Settlement 양쪽 유지

### 완료 기준

- [ ] 의뢰 수락 후 HUD `의뢰: 1/3`
- [ ] Lead 배치 후 `기계: N` 갱신 (구현 시)
