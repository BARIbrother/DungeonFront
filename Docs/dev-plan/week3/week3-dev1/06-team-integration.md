## Week 3 — MVP 통합 데모

> 전원 참여. Dev1 정리.

### 데모 체크리스트 (MVP 7항)

1. [ ] **Title** — 새 게임 / 불러오기
2. [ ] 1일차 **튜토** 2단계 + **대화** 1회
3. [ ] 의뢰 수락 → **10종+** 배치 → **벨트** 체인 생산
4. [ ] 튜토 **일시정지** 중 틱 정지 (`TickManager` 연동)
5. [ ] 결산 **납품·보상** · **멀티데이** · **게임오버**
6. [ ] **세이브 → 로드** — 일차·배치·의뢰 복원
7. [ ] 튜토~클리어 의뢰 라인 **1회 플레이**

### 역할별 전달물

| 제공 | 수신 | 전달물 |
|------|------|--------|
| Dev1 | Dev2 | `SaveData` + `IQuestSaveProvider` 훅 |
| Lead | Dev1 | `IFactorySave`, `StoryEventBus` |
| Dev2 | Dev1 | `quests[]` 직렬화 |
| Art | Dev1, Dev2 | 레이·네메시스 PNG |

### 머지 순서

`dev1/w3-*` → `dev2/w3-*` → `lead/w3-*` → `art/w3-*` → `develop`

### 완료 기준

- [ ] [dev-plan.md](../../dev-plan.md) MVP 완료 기준 7항 충족
