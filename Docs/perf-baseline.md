# Performance Baseline (Profiler)

최적화 전·후 비교용 체크리스트. Unity **Window → Analysis → Profiler** (CPU Usage, GC Alloc).

## 장면 A — Production (벨트 다수)

1. Play → Prepare에서 벨트·기계 라인을 충분히 배치 (벨트 30+ 권장).
2. Production 시작 (`T` 또는 Start).
3. Profiler에서 약 5초 샘플.
4. 기록할 항목:
   - `TickManager.AdvanceTick` / `TickLogisticsPhase` **GC Alloc**
   - `ConveyerBelt.TickLogistics` / `GetMachineAt` 호출 비중
   - `ConveyerBeltItemView.Update` 비중
   - 기계 UI를 연 상태면 `MachineRecipeUI.Update` Alloc

### 기대 (1차 최적화 후)

- `TickLogisticsPhase` 매틱 `new List` Alloc ≈ 0
- 정렬은 벨트 배치·회수·회전 시에만
- pull/push 중 매틱 `GetMachineAt` 재조회 없음

## 장면 B — Prepare (인벤)

1. Prepare에서 **E**로 인벤 개폐 반복, 아이템 추가·제거.
2. Profiler:
   - `InventoryUI.RebuildItemSlots` Destroy/Instantiate
   - `PlacementUI.Refresh` (B키 패널)

### 기대 (2차 후)

- 슬롯 Destroy 폭주 감소 (풀 재사용)
- `GetMachineAt`가 점유 딕셔너리로 O(1)

## 메모

| 일자 | 빌드/커밋 | A Alloc (틱) | A CPU ms | B Rebuild | 비고 |
|------|-----------|--------------|----------|-----------|------|
| | | | | | 최적화 적용 후 기입 |

코드 측 보조: `TickManager`는 에디터에서 벨트 수·정렬 dirty를 로그할 수 있게 `MarkBeltOrderDirty`를 둔다.
