## 씬·페이즈 흐름 보강

> **담당**: Dev1 · `Assets/Scripts/GameFlow/`

### 이 작업물

`GameSessionState` 단일 소스로 통합. `GameFlowController`와 페이즈·씬 로드 중복 정리.  
`GameSessionState`를 **단일 세션 소스**로 정리하고 `GameFlowController`는 **제거·통합**한다.

**코드**: `Assets/Scripts/GameFlow/`  
**계약**: [dev-contract.md](../../dev-contract.md)

### 보강 항목

| 항목 | 내용 |
|------|------|
| 세션 단일화 | `GameFlowController` 로직 → `GameSessionState`로 이전 후 레거시 삭제 |
| `OnPhaseChanged` | Lead·Dev2가 구독할 공개 이벤트 (시그니처 고정) |
| 씬 전환 | `Settlement` → Settlement 씬, `AdvanceDay()` → Factory 씬 |
| Production 타이머 | `ProductionRemainingSeconds` — Lead 틱은 W3, W2는 카운트만 |
| UI 정리 | `orderWindow`·`shopWindow` **제거** — Dev2가 자체 UI를 `OnPhaseChanged`로 제어 |

### Dev1이 하지 않는 것

- [ ] Dev2 의뢰·상점 패널 `SetActive` — **Dev2 책임**
- [ ] `GameObject.Find("orderWindow")` 등 이름 기반 Dev2 UI 검색

### Lead 계약

- [ ] `StartProduction()` → `phase == Production`
- [ ] Lead는 `GameFlow/` **읽기만**

### Dev2 계약

- [ ] Dev2 UI는 Dev2 프리팹 + `OnPhaseChanged` 자체 구독
- [ ] Dev1은 Factory·Settlement **씬 로드**와 공통 HUD만 제공

### 독립 개발 (Mock)

- [ ] Dev2 UI 없을 때: 페이즈 전환·씬 로드만 검증
- [ ] Lead 틱 없을 때: 디버그 10초 카운트다운

### 완료 기준

- [ ] `GameFlowController` 미사용 (씬·프리팹 참조 제거)
- [ ] Play → Factory → 생산 → Settlement → 다음 날 → Factory
- [ ] `OnPhaseChanged` 구독으로 Lead `PlacementController` Prepare 체크 동작
