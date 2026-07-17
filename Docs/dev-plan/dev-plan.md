# 개발 계획 (3주)

> **기간**: 3주 (Week 1 **완료** · Week 2~3 잔여)  
> **팀**: **팀장(Lead)** · **개발1(Dev1)** · **개발2(Dev2)** · **아트(Art)** — 4명  
> **MVP 범위**: `../00-vision.md` · `../01-core-loop.md` · `../02-data-structure.md`  
> **병렬 원칙**: [parallel-roadmap.md](./parallel-roadmap.md) · [dev-contract.md](./dev-contract.md)

---

## MVP 완료 기준

| # | 항목 | 완료 조건 |
|---|------|-----------|
| 1 | 하루 3단계 | 준비 → 생산(5분) → 결산이 끊김 없이 1회 이상 순환 |
| 2 | 생산 | **10종 이상의 기계**·**벨트**·**출력기**·물류로 레시피 제작 가능 |
| 3 | 의뢰 | 수락(하루 최대 3) · 당일/멀티데이 납기 · 결산 납품 · 보상(골드·명성) |
| 4 | 필수 의뢰 | 실패 시 게임오버 → **다른 세이브**로 재도전 (autosave) |
| 5 | 경제 | 골드(재료·**기계**·구역 구매·제작), 명성(**해금**·의뢰 풀) |
| 6 | 세이브 | JSON 저장·불러오기 (일차·인벤·배치·의뢰 진행) |
| 7 | 콘텐츠 | 뒷산 동굴 튜토리얼~클리어까지 의뢰·**스토리** 라인 플레이 가능 |

---

## 팀 역할

| 역할 | 담당 |
|------|------|
| **Lead** | 공장 내부 — 맵·배치·생산·물류·기계 SO |
| **Dev1** | 세션·HUD·튜토·세이브·타이틀·대화 UI |
| **Dev2** | 의뢰·납품·경제·게임오버 |
| **Art** | 초상화·주인공 모션 |

**주차 내 독립 원칙**: 평일 Mock 병렬 · 금요일 통합. [parallel-roadmap.md](./parallel-roadmap.md)

---

## 3주 일정 (잔여만)

| 주차 | Lead | Dev1 | Dev2 | Art |
|------|------|------|------|-----|
| **1** ✅ | *(그리드·배치·틱·벨트·WIP·SO 등 구현 완료)* | *(세션·페이즈·Quest 골격)* | | *(모션 placeholder)* |
| **2** | **6종+·고장·출력기** | 인벤 UI·HUD·세션 통합·SaveDTO·타이틀 | 의뢰 UI·**납품·보상** | 수리 모션·**이브** 초상 |
| **3** | **10종+·수작업·요약·lock·스토리·MVP** | 튜토·세이브·대화·스토리 | 멀티데이·경제·게임오버·lock | 이브·레이 초상·표정·모션 보완 |
| **4** | 공장 폴리시·구역·맵/노드·기계 피드백 | UI·튜토 완성·세이브·엔딩 | 경제 lock·뒷산 의뢰·클리어 라인 | UI 프레임·뒷산 타일·기계 폴리시 |

상세: [week1](./week1/week1.md) · [week2](./week2/week2.md) · [week3](./week3/week3.md) · [week4](./week4/week4.md)  
W4 범위: **뒷산 동굴만** (복수 던전은 [99-further_implementation.md](../99-further_implementation.md))

---

## 잔여 체크리스트

### 이미 구현됨 ✅

- 그리드·배치·생산 틱·벨트·WIP·레시피 UI (`MachineRecipeUI`)
- `GameSessionState`·페이즈·`TickManager` 연동
- `PlayerMovement`·`PlayerInventory`
- `Quest`·`QuestManager` 골격 (UI·납기·경제 미연동)

### Week 2

- [ ] Lead — 6종+·고장·출력기
- [ ] Dev1 — 인벤 UI·HUD 바인딩·세션 단일화·SaveDTO·타이틀
- [ ] Dev2 — 의뢰 풀·수락 UI·결산·납품·보상
- [ ] Art — 수리·작업 모션·이브 초상

### Week 3 (MVP)

- [ ] Lead — 10종+·수작업·생산 요약·데이터 lock·스토리 훅
- [ ] Dev1 — 튜토·세이브·대화 UI
- [ ] Dev2 — 멀티데이·경제·게임오버·의뢰 lock
- [ ] Art — 이브·레이 초상·캐릭터별 표정·모션 보완

### Week 4 (뒷산 동굴 완성)

- [ ] Lead — 공장 폴리시·구역 확장·맵/노드·기계 정보 패널
- [ ] Dev1 — UI 폴리시·튜토 완성·세이브 안정화·엔딩
- [ ] Dev2 — 경제 수치 lock·뒷산 의뢰·클리어 라인·결산 폴리시
- [ ] Art — UI 프레임·뒷산 타일·기계 비주얼 폴리시

---

## 관련 문서

- [parallel-roadmap.md](./parallel-roadmap.md)
- [dev-contract.md](./dev-contract.md)
- [lead-plan.md](./lead-plan.md)
- [dev-gaps.md](./dev-gaps.md)
- [week4/week4.md](./week4/week4.md)
