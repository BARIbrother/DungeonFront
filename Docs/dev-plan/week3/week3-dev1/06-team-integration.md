# Week 3 — 통합 데모 (팀)

> **역할**: 전원 · **Week**: 3 · **Issue**: Dev1 06 (통합 오너)  
> **시점**: 금요일  
> **상위**: [week3.md](../week3.md)

---

## 1. 목표

MVP **엔드투엔드 1회 플레이** — Title → 튜토 → 생산 → 결산 → 멀티데이 → 세이브/로드 → (스토리 only 시연 시 ~7일).

---

## 2. 통합 체크리스트

1. [ ] **Title** — 슬롯·새 게임·불러오기 ([04-save-load](./04-save-load.md))
2. [ ] **1일차 튜토** 2단계+ + **대화** 1회 ([01-tutorial-panel](./01-tutorial-panel.md), [05-dialogue-story](./05-dialogue-story.md))
3. [ ] **의뢰 수락** → 10종+ 배치 → 벨트/수동 **철 체인** 생산 (Lead)
4. [ ] 튜토/대화 **일시정지** 중 틱 정지
5. [ ] **생산 요약** → 결산 **납품·보상** (Dev2)
6. [ ] **멀티데이** 의뢰 · **게임오버** · **경제·해금** (Dev2)
7. [ ] **세이브 → 로드** — 일차·배치·의뢰 복원
8. [ ] **이브·레이 초상** · **캐릭터 표정** (Art)
9. [ ] 튜토~클리어 의뢰 라인 **1회 플레이** (크래시 0)

---

## 3. 역할별 전달물

| 제공 | 수신 | 전달물 | 확인 방법 |
|------|------|--------|-----------|
| Dev1 | Dev2 | `SaveData` + `IQuestSaveProvider` 훅 인터페이스 | Dev2 Import 호출 |
| Lead | Dev1 | `IFactorySave`, `StoryEventBus` | Save/Load factory 필드 |
| Dev2 | Dev1 | `quests[]` 직렬화 구현 | 슬롯 로드 후 의뢰 유지 |
| Art | Dev1 | 레이·표정 PNG | Dialogue 로드 |
| Art | Lead | 주인공 시트 | Factory 모션 |
| Dev2 | Lead | Quest `requiredItems` itemId | 생산 경로 검증 |

---

## 4. 머지 순서

```
dev1/w3-* → dev2/w3-* → lead/w3-* → art/w3-* → develop
```

충돌: [dev-contract.md](../../dev-contract.md) 폴더 소유 표 준수.

---

## 5. 시연 스크립트 (30분)

| 순서 | 행동 | 검증 Issue |
|------|------|------------|
| 1 | Title 새 게임 | 04-save-load |
| 2 | 오프닝 대화 + 튜토 1~2 | 01, 05, Lead 06 |
| 3 | O키 의뢰 → 00100001 수락 | Dev2 05 |
| 4 | B키 배치 채굴·용광로·제작 | Lead 01, 03 |
| 5 | 수동 운반 + 제작 클릭 | Lead 03 |
| 6 | 생산 시작 → (Skip 타이머) 요약 | Lead 04 |
| 7 | 결산 납품 n/m | Dev2 05 |
| 8 | 슬롯 저장 → Title → 로드 | 04, Dev2 04 |
| 9 | (선택) 멀티데이·게임오버 | Dev2 01, 03 |

---

## 6. 실패 시 에스컬레이션

| 증상 | 1차 담당 |
|------|----------|
| HUD·씬 | Dev1 |
| 납품·보상 | Dev2 |
| 배치·생산 | Lead |
| 초상·모션 | Art |

---

## 7. 완료 기준

- [ ] §2 체크리스트 **전항목** 체크 또는 known issue 목록
- [ ] develop에서 §5 시연 1회 성공
- [ ] [week3.md](../week3.md) 통합 데모 표와 일치

---

## 8. 관련 문서

- [week3-lead/07-mvp-integration](../week3-lead/07-mvp-integration.md)
- [parallel-roadmap.md](../../parallel-roadmap.md)
