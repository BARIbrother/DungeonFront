## 배치 모드

### 이 작업물

**Prepare** 단계 인벤 기계 → 맵 **배치·회수** + `GameSessionState` **동기화**.  
구 7주 **2주** 「기계 배치·노드」 **전부 Lead**.

**코드**: `Assets/Scripts/` (예: `PlacementController`, `PlacementUI`)  
**씬**: `Factory`

### 규격

| 항목 | 내용 |
|------|------|
| 선택 | 하단 인벤 기계 목록 클릭 (Dev1 [01-inventory-ui](../week2-dev1/01-inventory-ui.md)와 연동) |
| 배치 | 맵 그리드 클릭 → 맵 인스턴스 + 세션 갱신 |
| 회수 | 맵 기계 클릭 → 인벤 복귀 + 세션 갱신 |
| 채굴기 | 자원 **노드 위 1대**만 |
| 충돌 | 점유 칸 겹침·맵 밖 불가 |
| 페이즈 | `Prepare`만 ([week1 Dev 세션](../../week1/week1-dev/01-session-phase.md)) |

### 세션 동기화 (Lead 소유)

배치·회수 시 **`PlayerInventory`만** 갱신한다. `GameSessionState.inventory` 스텁은 **사용하지 않는다**.  
계약: [dev-contract.md](../../dev-contract.md)

- [ ] `PlaceMachine(instanceId, gridX, gridY)`  
  - `PlayerInventory.TryRemoveMachine(instanceId)` → 맵에 인스턴스 생성
- [ ] `PickupMachine(instanceId)`  
  - 맵 인스턴스 제거 → `PlayerInventory.ReturnMachine(entry)` *(계약 API)*
- [ ] `CanPlace(machineDefId, gridX, gridY)` — 노드·충돌·채굴기 규칙
- [ ] `GetPlacedMachines()` — 세이브·틱용 (`02-data-structure.md`)

Dev1은 `GameSessionState`·`PlayerInventory` **싱글톤 제공**만. 배치 API는 Lead에 둔다.

### Dev1·인벤 UI 계약 (금요일)

- [ ] Dev1 인벤 UI — 기계 행 클릭 → `PlacementController.SelectMachine(instanceId)`
- [ ] Lead 갱신 후 Dev1 UI는 `OnMachinesChanged`로 **자동 갱신**

### 독립 개발 (Mock)

- [ ] Dev1 인벤 UI 없을 때: Lead 단독 테스트 씬에서 배치·회수 검증

### 완료 기준

- [ ] 채굴기를 철광석 노드 위에 배치·회수 가능
- [ ] 배치 후 `PlacedOnMap` + 좌표가 세션에 반영
- [ ] Dev1 인벤 UI에서 배치한 기계가 목록에서 사라짐
- [ ] Production 중 배치 UI 비활성
