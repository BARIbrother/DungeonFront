## Week 2 마일스톤 — Lead

> 상위: [lead-plan.md](../../lead-plan.md) · [parallel-roadmap.md](../../parallel-roadmap.md) · Week 1 [완료](../../week1/week1-lead/00-week1-milestone.md)

### 이 주에 만드는 것

**배치 + 생산 가동 + 6종+(출력기 포함)·WIP·고장·출력기 물류**.  
Dev1·Dev2 완료를 **기다리지 않고** 독립 브랜치에서 개발한다.

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 배치 모드 (인벤→맵·회수) | [01-placement-mode](./01-placement-mode.md) |
| 2 | 레시피 데이터 v1 — 철 체인 3종 *(SO 구현 완료)* | [02-so-v1](./02-so-v1.md) |
| 3 | 생산 틱·프레임워크 + 철 체인 3종 | [03-production-tick](./03-production-tick.md) |
| 4 | 기계 6종+ Prefab (출력기 포함) | [04-machines-6plus](./04-machines-6plus.md) |
| 5 | WIP·고장·수리 | [05-wip-malfunction](./05-wip-malfunction.md) |
| 6 | **출력기** (전방향 포트 접근) | [06-outputter](./06-outputter.md) |
| 7 | 금요일 통합 | [../team-integration.md](../team-integration.md) |

### 독립 개발 (Mock)

| 의존 대상 | Mock 방법 |
|-----------|-----------|
| Dev1 `GameSessionState` | 페이즈 수동 토글 테스트 씬 |
| Dev1 `PlayerInventory` | 배치 시 세션 인벤 직접 갱신 (W1 구조) |
| Dev1 인벤 UI | 배치 API만 구현 — UI 연동은 금요일 |

### Week 2 계약 제공

- [ ] `Assets/Data/Contracts/Items/` — iron 체인 4종 Item SO (**Dev2 Quest SO용**, W2 초반)
- [ ] 배치 시 `PlayerInventory.TryRemoveMachine` / `ReturnMachine`
- [ ] `GetPlacedMachines()`
- [ ] `IFactoryProduction` 스텁

### 금요일 데모 시나리오

1. NewGame → 채굴기·용광로·제작기·**출력기** 배치 (채굴기는 노드 위)
2. **생산 시작** → 채굴기 틱, 출력기로 체인 1구간 이상 연결
3. 철 체인 3종 (출력기 또는 수동 투입)
4. 6종+ 중 추가 기계 1종 이상 가동
5. 고장 → **수리 클릭** 후 재가동
6. (Dev1) 결산 1회전 · (Dev2) 의뢰 목록 n/m

### 완료 기준

- [ ] 위 1~6 Issue 완료 기준 충족 (02는 데이터·연결 확인)
- [ ] [team-integration.md](../team-integration.md) 데모 3·4 재현
