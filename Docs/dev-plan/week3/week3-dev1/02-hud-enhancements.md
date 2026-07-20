# HUD 보강

**역할**: Dev1 · **Week**: 3 · **Issue**: 02  
**선행**: W2 HUD

## 1. 이 작업물

글로벌 HUD에 일차·페이즈·골드·명성·수락 의뢰 수를 이벤트 기반으로 표시한다.  
(선택) 튜토 현재 단계 표시.

**코드**: `Assets/Scripts/GameFlow/` — 기존 HUD 확장 또는 `GlobalHUD.cs`

## 2. UI 바인딩

| UI 라벨 | 데이터 소스 | 갱신 |
|---------|-------------|------|
| 일차 | `GameSessionState.day` | `OnPhaseChanged`, `AdvanceDay` |
| 페이즈 | `GameSessionState.phase` | `OnPhaseChanged` |
| 골드 | `GameSessionState.gold` | `OnGoldChanged` 등 |
| 명성 | `GameSessionState.reputation` | 동일 |
| 생산 타이머 | `ProductionRemainingSeconds` | Production 중 |
| 의뢰 | `ActiveQuests.Count` / max | `OnQuestAccepted` / `OnQuestsChanged` |
| (선택) 튜토 | `TutorialState.currentStep` | 튜토 Next |

의뢰 표시: `의뢰: {activeCount}/3` (동시 수락 상한 3).  
미연동 시 Mock: `의뢰: 0/3`.

읽기만. `orderWindow.SetActive` 금지. 매 프레임 `Find` 금지.

## 3. 완료 기준

- [ ] 일차·페이즈·골드·명성·타이머·의뢰 n/3 표시
- [ ] 이벤트 구독으로 갱신 (매 프레임 Find 없음)
- [ ] NewGame → 일차 1 · Prepare
- [ ] 생산 시작 → 타이머 · Production 표시
- [ ] Factory ↔ Settlement 전환 시 HUD 유지
