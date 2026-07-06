## 인벤토리 UI

> **담당**: Dev1 · `Assets/Scripts/GameFlow/`  
> **계약**: [dev-contract.md](../../dev-contract.md)

### 이 작업물

Prepare 단계 **아이템·기계 인벤** 표시 UI.

**코드**: `Assets/Scripts/GameFlow/` (예: `InventoryUI.cs`)

### 데이터 소스 (단일)

| 구역 | 소스 |
|------|------|
| **아이템** | `PlayerInventory.GetCount(itemId)` + `OnItemsChanged` |
| **기계** | `PlayerInventory.Machines` (`InInventory`만) + `OnMachinesChanged` |

- [ ] `GameSessionState.inventory` 스텁 **사용 금지**
- [ ] Prepare에서만 편집 가능 UI (Production·Settlement 숨김 또는 읽기 전용)

### Lead 연동 (금요일)

- [ ] 기계 행 클릭 → `PlacementController.SelectMachine(instanceId)`
- [ ] Lead `TryRemoveMachine` / `ReturnMachine` 후 `OnMachinesChanged`로 갱신

### 독립 개발 (Mock)

- [ ] Lead 미완: NewGame 기계 4종 하드코딩 표시
- [ ] 클릭 시 `Debug.Log`만 출력해도 중반까지 OK

### 완료 기준

- [ ] NewGame 후 기계 4종 표시
- [ ] Lead 배치 후 목록에서 사라짐, 회수 시 복귀
