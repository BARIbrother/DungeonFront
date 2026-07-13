## Week 2 마일스톤 — Lead

> 상위: [lead-plan.md](../../lead-plan.md) · [parallel-roadmap.md](../../parallel-roadmap.md)

### 이 주에 만드는 것

**6종+·고장·출력기** — 배치·틱·SO·벨트·WIP는 **이미 구현됨**.

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 기계 6종+ Prefab (출력기 포함) | [04-machines-6plus](./04-machines-6plus.md) |
| 2 | 고장·수리 | [05-wip-malfunction](./05-wip-malfunction.md) |
| 3 | **출력기** (전방향 포트 접근) | [06-outputter](./06-outputter.md) |
| 4 | 금요일 통합 | [07-team-integration](./07-team-integration.md) |

### Week 2 계약 제공

- [ ] `Assets/Data/Contracts/Items/` — iron 체인 4종 Item SO (**Dev2 Quest SO용**)
- [ ] `GetPlacedMachines()` (배치 API는 구현됨)

### 금요일 데모 시나리오

1. NewGame → 채굴기·용광로·제작기·**출력기** 배치
2. **생산 시작** → 철 체인 틱 · 출력기 체인 1구간
3. 6종+ 중 추가 기계 1종 이상 가동
4. 고장 → **수리 클릭** 후 재가동
5. (Dev) 결산 납품·보상

### 완료 기준

- [ ] 위 1~3 Issue 완료 기준 충족
- [ ] [team-integration.md](../team-integration.md) 데모 재현
