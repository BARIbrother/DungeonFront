## 생산·Lead 틱 연동

### 이 작업물

Dev **페이즈** ↔ Lead **ProductionTickSystem** 연동. 구 7주 **3주** 「생산 틱·프레임워크, 철 체인 3종」의 **Dev 쪽**.

**Lead**: [week2 03-production-tick](../../week2/week2-lead/03-production-tick.md)

### 계약

- [ ] `StartProduction()` → Lead `ProductionTickSystem.Start()` (또는 이벤트)
- [ ] `phase == Settlement` → Lead `ProductionTickSystem.Stop()`
- [ ] 튜토 패널 **일시정지** 중 Lead 틱 **정지**
- [ ] `ProductionRemainingSeconds` — Week 1과 동일, Lead 타이머 UI 읽기

### 검증

- [ ] Prepare에서 틱 없음
- [ ] Production 5분(또는 디버그 단축) 동안 Lead 철 체인 틱 동작
- [ ] 만료 시 자동 Settlement

### 완료 기준

- [ ] 페이즈 전환과 Lead 틱 start/stop 동기
- [ ] 튜토 일시정지 시 틱 정지
