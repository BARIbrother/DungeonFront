## Week 2 마일스톤 — Dev2

> **담당**: 의뢰 (공장 외부) · `Assets/Scripts/Quest/`  
> 상위: [week2.md](../week2.md) · [dev-contract.md](../../dev-contract.md)

### 이 주에 만드는 것

**의뢰 SO**·**출현 풀**·**수락 UI**·**결산 목록**.  
Lead 배치·Dev1 HUD를 **기다리지 않고** Mock 인벤·세션으로 개발한다.

### 산출물 목록 (권장 작업 순서)

| 순서 | # | 작업물 | Issue |
|:----:|---|--------|-------|
| 1 | 01 | 의뢰 에셋 v1 | [01-quest-assets-v1](./01-quest-assets-v1.md) |
| 2 | 02 | 의뢰 출현 풀 | [02-quest-pool](./02-quest-pool.md) |
| 3 | 03 | 의뢰 수락 UI | [03-quest-accept-ui](./03-quest-accept-ui.md) |
| 4 | 04 | 결산 의뢰 목록 | [04-settlement-quest-list](./04-settlement-quest-list.md) |
| — | — | 금요일 통합 | [../team-integration.md](../team-integration.md) |

### 독립 개발 (Mock)

| 의존 대상 | Mock 방법 |
|-----------|-----------|
| `GameSessionState` (Dev1) | reputation·day 하드코딩 |
| `PlayerInventory` (Dev1) | Contracts Items로 `Add()` 테스트 |
| Lead Item SO | `Assets/Data/Contracts/Items/` placeholder SO |
| Dev1 HUD | `OnQuestAccepted`만 발행 — HUD는 금요일 |

### 금요일 데모 시나리오 (Dev2 담당 구간)

1. Prepare — 튜토 의뢰 **수락** (0/3 → 1/3)
2. 생산 1회전 후 결산 — **의뢰 목록** n/m

### 완료 기준

- [ ] 위 01~04 Issue 완료 기준 충족
- [ ] [team-integration.md](../team-integration.md) 데모 2·5 재현
