## 배치 모드

### 이 작업물

**Prepare** 단계 인벤 기계 → 맵 **배치·회수** + `GameSessionState` **동기화**.  
구 7주 **2주** 「기계 배치·노드」 **전부 Lead**.

**코드**: `Assets/Scripts/` (예: `PlacementController`, `PlacementUI`)  
**씬**: `Factory`

### 규격

| 항목 | 내용 |
|------|------|
| 선택 | 하단 인벤 기계 목록 클릭 (Dev [01-inventory-ui](../week2-dev/01-inventory-ui.md)와 연동) |
| 배치 | 맵 그리드 클릭 → 맵 인스턴스 + 세션 갱신 |
| 회수 | 맵 기계 클릭 → 인벤 복귀 + 세션 갱신 |
| 채굴기 | 자원 **노드 위 1대**만 |
| 충돌 | 점유 칸 겹침·맵 밖 불가 |
| 페이즈 | `Prepare`만 ([week1 Dev 세션](../../week1/week1-dev/01-session-phase.md)) |

### 세션 동기화 (Lead 소유)

`GameSessionState.inventory.machines` / `factory`를 **배치·회수 시 Lead가 직접** 갱신한다.

- [ ] `PlaceMachine(instanceId, gridX, gridY)`  
  - `placement` → `PlacedOnMap`, 좌표 설정  
  - 동일 리스트에서 필드만 변경 (Week 1 구조)
- [ ] `PickupMachine(instanceId)`  
  - `InInventory`, grid 클리어, 맵 인스턴스 제거
- [ ] `CanPlace(machineDefId, gridX, gridY)` — 노드·충돌·채굴기 규칙
- [ ] `GetPlacedMachines()` — 세이브·틱 시스템용 (`02-data-structure.md`)

Dev는 `GameSessionState` **싱글톤 제공**만. 배치 API는 Dev에 두지 않음.

### Dev·인벤 UI 계약

- [ ] Dev 인벤 UI — 기계 행 클릭 → `PlacementController.SelectMachine(instanceId)`
- [ ] Lead 갱신 후 Dev UI는 `OnInventoryChanged` 등으로 **자동 갱신**

### 완료 기준

- [ ] 채굴기를 철광석 노드 위에 배치·회수 가능
- [ ] 배치 후 `PlacedOnMap` + 좌표가 세션에 반영
- [ ] Dev 인벤 UI에서 배치한 기계가 목록에서 사라짐
- [ ] Production 중 배치 UI 비활성
