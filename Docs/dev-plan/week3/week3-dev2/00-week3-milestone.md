## Week 3 마일스톤 — Dev2

> **담당**: 의뢰·경제 · `Assets/Scripts/Quest/`  
> 상위: [week3.md](../week3.md) · [dev-contract.md](../../dev-contract.md)

### 이 주에 만드는 것

구 7주 4~7주 Dev2 항목 압축 — **멀티데이**·**경제·해금**·**게임오버**·의뢰 lock.  
W2 [납품·보상](../../week2/week2-dev2/05-delivery-rewards.md) 위에 확장.

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 멀티데이 납기 추적 | [01-multi-day-deadline](./01-multi-day-deadline.md) |
| 2 | 경제·명성 해금 UI | [02-economy-unlock](./02-economy-unlock.md) |
| 3 | 게임오버·미납 페널티 | [03-gameover-penalty](./03-gameover-penalty.md) |
| 4 | 의뢰 직렬화 (세이브) | [04-quest-serialize](./04-quest-serialize.md) |
| 5 | 의뢰 lock·결산 polish | [05-quest-lock-polish](./05-quest-lock-polish.md) |

### 독립 개발 (Mock)

| 의존 대상 | Mock |
|-----------|------|
| Lead 생산·해금 기계 | Contracts Items·기계 id 하드코딩 |
| Dev1 `SaveData` | W2 DTO — `quests[]` 필드만 구현 |
| 명성 threshold | reputation 임의값으로 풀 테스트 |

### 완료 기준

- [ ] 01~05 Issue 충족
- [ ] [week3-dev1/06-team-integration.md](../week3-dev1/06-team-integration.md) 데모 5·6·7 재현
