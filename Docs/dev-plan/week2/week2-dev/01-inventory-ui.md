## 인벤토리 UI

### 이 작업물

Prepare 단계 **아이템·기계 인벤** 표시 UI. Lead [01-placement-mode](../week2-lead/01-placement-mode.md) 배치 목록과 동일 세션을 읽는다.

**코드**: `Assets/Scripts/` (예: `InventoryUI.cs`)

### 표시 항목

| 구역 | 내용 |
|------|------|
| **아이템** | `itemId` → 수량 (아이콘 placeholder OK) |
| **기계** | `InInventory` 기계만 — `displayName`, `machineDefId` |

- [ ] `GameSessionState.inventory` 구독·갱신
- [ ] Prepare에서만 표시 (Production·Settlement 숨김 또는 읽기 전용)

### Lead 연동

- [ ] 기계 행 클릭 → Lead `PlacementController.SelectMachine(instanceId)` (또는 동등 API)
- [ ] 배치·회수 후 UI 자동 갱신

### 완료 기준

- [ ] NewGame 후 기계 4종이 인벤 UI에 보임
- [ ] Lead 배치 후 인벤에서 해당 기계 사라짐, 회수 시 복귀
