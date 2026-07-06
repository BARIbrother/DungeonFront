## 생산 틱·프레임워크 + 철 체인 3종

### 이 작업물

**10 tick/s** 루프 + 기계 공통 프레임워크. 철 **3종 체인** 가동. 구 7주 **3주**.

**코드**: `Assets/Scripts/` (예: `ProductionTickSystem`, `Machine`)

### 틱

| 항목 | 값 |
|------|-----|
| 속도 | 10 tick/s |
| 생산 | 3000 tick = 5분 (Dev1 타이머 동기, W3 `IFactoryProduction` 연동) |

### 프레임워크

- [ ] `Machine` 베이스 — definition, grid, 현재 레시피
- [ ] 자동 틱 — `durationTicks` 완료 시 출력 버퍼
- [ ] 수작업 — `manualClickCount` (제작기)
- [ ] 입·출력 버퍼 (`inputPort` / `outputPort` — `Machine` 기존 필드, 방향 없음)
- [ ] Production 시작/종료 시 기계 Start/Stop

### 철 체인 (Week 2 말)

채굴기(노드) → 용광로 → 제작기. [06-outputter](./06-outputter.md) 또는 **수동 투입**으로 연결.

### Dev1 계약 (W2 스텁 · W3 본격)

- [ ] `IFactoryProduction` — `StartTick()`, `StopTick()`, `IsRunning` 인터페이스 정의
- [ ] W2: Dev1 `StartProduction()` 없이 Lead 테스트 씬에서 틱 단독 검증 OK
- [ ] W3: Dev1 `OnPhaseChanged(Production)` → `StartTick()` 연동

### 완료 기준

- [ ] Production 중 채굴기 틱으로 iron_ore 출력
- [ ] 용광로·제작기 연결 (수동 투입 OK)
- [ ] Production 종료 시 틱 정지
- [ ] `IFactoryProduction` 스텁 구현체 존재
