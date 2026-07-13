## Week 2 마일스톤 — Dev1

> **담당**: 게임 플로우 · `Assets/Scripts/GameFlow/` · `Player/PlayerInventory.cs` (API)  
> 상위: [dev-contract.md](../../dev-contract.md)

### 이 주에 만드는 것

**인벤토리 UI**·**HUD**·**세션 단일화** (`GameFlowController` 제거).  
Dev2·Lead 완료를 **기다리지 않고** Mock으로 개발.

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 인벤토리 UI | [01-inventory-ui](./01-inventory-ui.md) |
| 2 | HUD 의뢰·인벤 요약 | [02-hud-quest-summary](./02-hud-quest-summary.md) |
| 3 | 씬·페이즈·세션 단일화 | [03-scene-flow-enhancement](./03-scene-flow-enhancement.md) |
| 4 | SaveData DTO 동결 | [04-save-dto-freeze](./04-save-dto-freeze.md) |
| 5 | 타이틀 씬 껍데기 | [05-title-shell](./05-title-shell.md) |
| 6 | 금요일 통합 | [../team-integration.md](../team-integration.md) |

### Dev1이 추가하는 계약 API (PlayerInventory)

- [ ] `OnItemsChanged` 이벤트
- [ ] `GetInInventoryMachineCount()` (또는 `Machines` 필터 헬퍼)

### 독립 개발 (Mock)

| 의존 대상 | Mock |
|-----------|------|
| `QuestManager` | HUD `0/3` 하드코딩 |
| Lead 배치 | 기계 4종 고정 표시 |

### 완료 기준

- [ ] 01~03 Issue 충족
- [ ] [team-integration.md](../team-integration.md) 데모 1·3·4·5·6
