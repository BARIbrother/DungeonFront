# Lead 개발 계획 (3주)

> **기간**: 3주 (Week 1 **완료** · Week 2~3 잔여)  
> **범위**: Factory 씬 — 맵·배치·생산·물류·기계 SO·공장 UI  
> **상위**: [dev-plan.md](./dev-plan.md)

---

## 일정 요약

| 주차 | 상태 | Lead 핵심 | 7주 계획 대응 |
|------|------|-----------|---------------|
| **1** | **완료** | Factory 씬·이동·Prefab·SO·생산 UI·기획 표 | 구 1주 |
| **2** | 진행 | 배치·레시피 데이터·생산 틱·철 체인·**6종+·WIP·고장·출력기** | 구 2~3주 + **4주 일부** |
| **3** | 예정 | **10종+·물류·수작업·레시피 lock·스토리 훅·MVP** | 구 **4~7주 전부** |

> 구 **4~7주** Lead 항목(기계 확장·물류·경제 연동·스토리·MVP 빌드)은 **2·3주에 분할**한다.  
> 구 2~3주(배치·틱·철 체인)는 **2주**에, 구 4~7주는 **2주 후반 + 3주**에 몰아 넣는다.

---

## Week 1 — 완료

Issue: [week1/week1-lead/](./week1/week1-lead/)

- 그리드 맵 + Factory 씬
- WASD + NewGame 스폰
- 기계 4종 + 철광석 노드 Prefab
- Item/Machine/Recipe SO + Database
- 생산 시작 + 5분 타이머 UI
- MVP 기계 10종+ 목록 v0.1
- 튜토 의뢰 수치 + 1일차 스토리 초안
- Dev·Art 연동 데모

---

## Week 2

**목표**: 배치 가능 + **생산 틱·철 체인 3종** 가동 + **기계 6종+·WIP·고장**까지.

| # | 작업 | Issue |
|---|------|-------|
| 1 | 배치 모드 (인벤→맵·회수·노드 규칙) | [01-placement-mode](./week2/week2-lead/01-placement-mode.md) |
| 2 | 레시피 데이터 v1 — 철 체인 3종 *(SO 완료)* | [02-so-v1](./week2/week2-lead/02-so-v1.md) |
| 3 | 생산 틱·프레임워크 + 철 체인 3종 | [03-production-tick](./week2/week2-lead/03-production-tick.md) |
| 4 | 기계 **6종+** Prefab·SO | [04-machines-6plus](./week2/week2-lead/04-machines-6plus.md) |
| 5 | WIP·고장·수리 | [05-wip-malfunction](./week2/week2-lead/05-wip-malfunction.md) |
| 6 | **출력기** (전방향 포트 접근) | [06-outputter](./week2/week2-lead/06-outputter.md) |
| 7 | Dev·Art 연동 데모 | [07-team-integration](./week2/week2-lead/07-team-integration.md) |

**금요일 데모**: 채굴기(노드)→용광로→제작기 틱 동작, 6종+ 배치, 고장 1회 수리.

---

## Week 3

**목표**: MVP Lead 담당분 **전부** — 10종+·벨트·출력기·수작업·운반·레시피 lock·스토리 훅·통합 빌드.

| # | 작업 | Issue |
|---|------|-------|
| 1 | 기계 **10종+** Prefab·SO 일괄 | [01-machines-10plus](./week3/week3-lead/01-machines-10plus.md) |
| 2 | 벨트·출력기 물류 | [02-logistics](./week3/week3-lead/02-logistics.md) |
| 3 | 수작업·수동 운반·회수 | [03-manual-interaction](./week3/week3-lead/03-manual-interaction.md) |
| 4 | 레시피 전환 UI·생산 종료 요약 | [04-recipe-ui-summary](./week3/week3-lead/04-recipe-ui-summary.md) |
| 5 | 레시피·기계 SO lock | [05-recipe-data-lock](./week3/week3-lead/05-recipe-data-lock.md) |
| 6 | 스토리 이벤트 트리거·전 구간 연동 | [06-story-hooks](./week3/week3-lead/06-story-hooks.md) |
| 7 | MVP 통합·잔여 버그 | [07-mvp-integration](./week3/week3-lead/07-mvp-integration.md) |

**금요일 데모**: 10종+·벨트 체인으로 튜토 의뢰 물품 생산 → 결산 납품 (Dev 연동).

---

## 관련 문서

- [week1/week1-lead/](./week1/week1-lead/) — 1주 (완료)
- [week2/week2.md](./week2/week2.md) — 2주 개요 · [week2-lead/](./week2/week2-lead/) · [week2-dev/](./week2/week2-dev/) · [week2-art/](./week2/week2-art/)
- [week3/week3.md](./week3/week3.md) — 3주 개요 · [week3-lead/](./week3/week3-lead/) · [week3-dev/](./week3/week3-dev/) · [week3-art/](./week3/week3-art/)
- [03-machine-plan.md](../03-machine-plan.md) — 기계 목록
