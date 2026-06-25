## Week 1 마일스톤 — Dev

### 이 주에 만드는 것

**공장 밖** 게임 뼈대: 하루 **페이즈 상태**, **새 게임** 초기값, **글로벌 HUD**, **결산 화면 껍데기**, **Factory ↔ Settlement 씬 전환**, **의뢰 SO** 골격.  
Factory 맵·Prefab·생산 UI·기계 SO **콘텐츠**는 Lead 담당.

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | `GameSessionState` + 페이즈 + 5분 타이머 상태 | [01-session-phase](./01-session-phase.md) |
| 2 | `NewGame()` + 인벤토리 (기계 3종 인벤) | [02-new-game-inventory](./02-new-game-inventory.md) |
| 3 | 글로벌 HUD (일차·페이즈·골드·명성) | [03-global-hud](./03-global-hud.md) |
| 4 | Settlement 씬/UI 스텁 + 다음 날 | [04-settlement-stub](./04-settlement-stub.md) |
| 5 | 씬 로드 + Lead API 연동 | [05-scene-flow](./05-scene-flow.md) |
| 6 | `QuestDefinition` + Database | [06-quest-data](./06-quest-data.md) |

### 금요일 데모 시나리오

1. 새 게임 → 일차 1, 골드 0, 명성 0, 페이즈 Prepare  
2. **Factory** 표시, Lead 맵·이동  
3. 생산 시작 → Production, 5분 타이머  
4. 종료 → **Settlement** 스텁  
5. 다음 날 → 일차 2, Prepare, Factory 복귀  

### 완료 기준

- [ ] 위 1~6 Issue 완료 기준 충족
- [ ] 데모 시나리오 1~5 재현
