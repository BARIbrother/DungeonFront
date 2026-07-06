## Week 2 마일스톤 — Dev

### 이 주에 만드는 것

**인벤·의뢰 UI**·**결산 의뢰 목록**·**HUD**·**의뢰 풀**·**의뢰 에셋 v1**. Dev **2인** 분담.  
구 7주 **2주** 개발 (배치·노드는 Lead).

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 인벤토리 UI | [01-inventory-ui](./01-inventory-ui.md) |
| 2 | 의뢰 목록·수락 UI | [02-quest-accept-ui](./02-quest-accept-ui.md) |
| 3 | 결산 의뢰 목록 (n/m) | [03-settlement-quest-list](./03-settlement-quest-list.md) |
| 4 | HUD 의뢰·인벤 요약 | [04-hud-quest-summary](./04-hud-quest-summary.md) |
| 5 | 의뢰 출현 풀 (명성) | [05-quest-pool](./05-quest-pool.md) |
| 6 | 의뢰 에셋 v1 (기존 `Quest` SO) | [06-quest-assets-v1](./06-quest-assets-v1.md) |
| 7 | Lead·Art 연동 | [07-team-integration](./07-team-integration.md) |

### 담당 분리 (권장)

| Dev | Issue |
|-----|-------|
| **A** | 01, 04, 06 |
| **B** | 02, 03, 05 |

### 금요일 데모 시나리오

1. NewGame → 인벤 UI · HUD `의뢰: 0/3`  
2. 의뢰 풀에서 튜토 의뢰 **수락**  
3. Lead **배치** (세션은 Lead 갱신) → 인벤·HUD 반영  
4. 생산 1회전 → 결산 **의뢰 목록** n/m  
5. 다음 날  

### 완료 기준

- [ ] 위 1~6 Issue 완료 기준 충족
- [ ] 데모 시나리오 1~4 재현
