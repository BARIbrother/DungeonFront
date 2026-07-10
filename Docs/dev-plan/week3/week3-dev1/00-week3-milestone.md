## Week 3 마일스톤 — Dev1

> **담당**: 게임 플로우 · `Assets/Scripts/GameFlow/`  
> 상위: [week3.md](../week3.md) · [dev-contract.md](../../dev-contract.md)

### 이 주에 만드는 것

**튜토리얼**·**HUD**·**세이브**·**대화 UI**·스토리 연출.  
*(페이즈·틱 연동은 `TickManager`에 구현됨)*

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 튜토리얼 패널 | [01-tutorial-panel](./01-tutorial-panel.md) |
| 2 | HUD 보강 | [02-hud-enhancements](./02-hud-enhancements.md) |
| 3 | JSON 세이브·로드 | [04-save-load](./04-save-load.md) |
| 4 | 대화 UI·스토리 연출 | [05-dialogue-story](./05-dialogue-story.md) |
| 5 | MVP 통합 데모 | [06-team-integration](./06-team-integration.md) |

### 독립 개발 (Mock)

| 의존 대상 | Mock |
|-----------|------|
| Lead `IFactorySave` | `factory: null` |
| `StoryEventBus` | `RaiseMock(eventId)` |
| Dev2 `quests[]` | 빈 배열 저장 |

### 완료 기준

- [ ] 01~04 Issue 충족
- [ ] [06-team-integration.md](./06-team-integration.md) MVP 데모 재현
