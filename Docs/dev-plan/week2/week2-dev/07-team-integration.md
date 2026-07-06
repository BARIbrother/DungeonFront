## Lead·Art 연동 (Week 2)

### 데모 체크리스트

1. [ ] NewGame → 인벤 UI · HUD 의뢰 0/3
2. [ ] 의뢰 풀 → 튜토 의뢰 수락 → HUD 1/3
3. [ ] Lead **배치** → 인벤·HUD 기계 수 갱신
4. [ ] 생산 1회전 → **결산 의뢰 목록** n/m 표시
5. [ ] 다음 날
6. [ ] (Art) 수리·작업 모션 Lead 적용 확인

### Dev 제공

| 대상 | 전달물 |
|------|--------|
| Lead | `GameSessionState` 접근, `OnPhaseChanged` (Prepare 여부) |
| Art | 초상화 import 경로 `Assets/Art/UI/` |

### 담당 분리 (2인 권장)

| Dev | Issue |
|-----|-------|
| A | [01-inventory-ui](./01-inventory-ui.md), [04-hud-quest-summary](./04-hud-quest-summary.md), [06-quest-assets-v1](./06-quest-assets-v1.md) |
| B | [02-quest-accept-ui](./02-quest-accept-ui.md), [03-settlement-quest-list](./03-settlement-quest-list.md), [05-quest-pool](./05-quest-pool.md) |

### 완료 기준

- [ ] 위 1~5 한 세션 동작
