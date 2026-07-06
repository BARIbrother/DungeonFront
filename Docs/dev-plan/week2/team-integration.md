## Week 2 — 금요일 통합 게이트

> 상위: [week2.md](./week2.md) · [dev-contract.md](../dev-contract.md)

### 데모 체크리스트

1. [ ] NewGame → 인벤 UI · HUD `의뢰: 0/3`
2. [ ] 의뢰 풀 → 튜토 의뢰 **수락** → HUD `1/3`
3. [ ] Lead **배치** → 인벤·HUD 기계 수 갱신
4. [ ] **생산 시작** → 채굴기 틱 (Lead) · 5분 타이머 (Dev1)
5. [ ] 생산 1회전 → 결산 **의뢰 목록** n/m
6. [ ] **다음 날** → Factory 복귀
7. [ ] (Art) 수리·작업 모션 Lead 적용 확인

### 역할별 전달물

| 제공 | 수신 | 전달물 |
|------|------|--------|
| Dev1 | Lead, Dev2 | `GameSessionState`, `OnPhaseChanged`, `PlayerInventory` API |
| Dev2 | Dev1 | `OnQuestAccepted`, `ActiveQuests` |
| Lead | Dev1 | 배치 API, `GetPlacedMachines()` |
| Lead | Dev2 | `Assets/Data/Contracts/Items/` 4종 |
| Art | Lead | repair·work → `Assets/Art/Characters/Protagonist/` |
| Art | Dev2 | `eve_portrait.png` → `Assets/Art/UI/Portraits/` |

### 머지 순서

`dev1/w2-*` → `dev2/w2-*` → `lead/w2-*` → `art/w2-*` → `develop`

### 완료 기준

- [ ] 위 1~6 한 세션 동작
- [ ] [dev-contract.md](../dev-contract.md) 폴더·API 규칙 준수
