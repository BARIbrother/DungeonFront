## 의뢰 에셋 v1

> **담당**: Dev2 · `Assets/Scripts/Quest/`  
> **권장 순서**: Dev2 **1번째** (풀·UI보다 먼저)

### 이 작업물

**기존** `Quest` SO·`QuestManager`에 **1~3일차 의뢰 에셋**을 채운다.  
클래스·SO 스키마 정의는 **하지 않음** — 이미 `Assets/Scripts/Quest/`에 있음.

**기획**: Lead [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md)

### 계약 에셋 (Lead 의존 제거)

`requiredItems`·`rewards`의 Item 참조는 **반드시** 계약 경로만 사용한다.

| 경로 | itemId |
|------|--------|
| `Assets/Data/Contracts/Items/` | `iron_ore`, `iron`, `iron_rod`, `iron_plate` |

- [ ] Lead `Assets/Data/Contracts/Items/` 에셋 **없을 때**: Dev2 브랜치에 **동일 id** placeholder Item SO 임시 생성 (금요일 전 Lead 에셋으로 교체)
- [ ] Lead 전용 `Assets/Data/Items/` 등 **직접 참조 금지** ([dev-contract.md](../../dev-contract.md))

### Week 2 에셋 (최소)

| 에셋 | 비고 |
|------|------|
| 튜토 의뢰 1개 | `00100001` — `title`·`requiredItems`·`rewards`·`deadlineDays` |
| (선택) 2~3일차 1~2개 | 기획 v1 있으면 추가 |

### 작업

- [ ] `Assets/Data/Quests/`에 Quest SO 생성
- [ ] `deadlineDays` — 당일 = 1
- [ ] 풀·UI는 **이번 Issue 완료 후** 연결 ([02-quest-pool](./02-quest-pool.md))

### 범위 밖

- 납품·보상 실행 (Week 3~)

### 완료 기준

- [ ] 튜토 Quest SO 1개, Contracts Items만 참조
- [ ] 에셋 단독으로 Inspector에서 필드 검증 가능 (UI 없이)
