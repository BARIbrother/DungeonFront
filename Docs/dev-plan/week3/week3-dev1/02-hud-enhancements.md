# HUD 보강

> **역할**: Dev1 · **Week**: 3 · **Issue**: 02  
> **선행**: W2 [02-hud-quest-summary](../../week2/week2-dev2/02-hud-quest-summary.md) (Dev1 HUD)  
> **계약**: [dev-contract.md](../../dev-contract.md) — 읽기 전용 Quest API

---

## 1. 이 작업물

글로벌 HUD에 **일차·페이즈·골드·명성·수락 의뢰 수**를 이벤트 기반으로 표시한다.  
(선택) 튜토 현재 단계 표시.

**코드**: `Assets/Scripts/GameFlow/` — 기존 HUD 컴포넌트 확장 또는 `GlobalHUD.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| `GameSessionState` 구독 | Dev2 의뢰·상점 패널 내용 |
| `QuestManager` 이벤트/카운트 | Lead 생산 요약 모달 |
| Factory + Settlement 양쪽 | 미니맵 로직 |

---

## 3. UI 바인딩 표

| UI 라벨 | 데이터 소스 | 갱신 시점 |
|---------|-------------|-----------|
| 일차 | `GameSessionState.day` | `OnPhaseChanged`, `AdvanceDay` |
| 페이즈 | `GameSessionState.phase` | `OnPhaseChanged` |
| 골드 | `GameSessionState.gold` | 보상 후 — **이벤트 필요 시** `OnGoldChanged` 추가 |
| 명성 | `GameSessionState.reputation` | 동일 |
| 생산 타이머 | `ProductionRemainingSeconds` | Production 중 매 프레임 또는 1초 간격 |
| 의뢰 | `ActiveQuests.Count` / max | `OnQuestAccepted`, `OnQuestsChanged` |
| (선택) 튜토 | `TutorialState.currentStep` | 튜토 Next |

### 의뢰 표시 형식

```
의뢰: {activeCount}/3
```

- max 3 — MVP 동시 수락 상한 (기획 미정 시 3 고정)
- Dev2 미완 Mock: `"의뢰: 0/3"` 고정

---

## 4. API (읽기만)

```csharp
// GameSessionState
Instance.day, .phase, .gold, .reputation, .ProductionRemainingSeconds
Instance.OnPhaseChanged += ...

// QuestManager — W3 권장 (dev-contract)
QuestManager.Instance.OnQuestAccepted += count => ...
// 또는 currentQuests.Count 폴링 (임시)
```

**금지**: `orderWindow.SetActive` — Dev2/세션 규칙은 Dev2가 `OnPhaseChanged`로 처리.

---

## 5. 구현 단계

- [ ] HUD 프리팹 — DontDestroyOnLoad 또는 씬 간 공유
- [ ] `OnPhaseChanged` 구독 — Prepare/Production/Settlement 라벨
- [ ] Quest 이벤트 — 수락 시 `의뢰: 1/3` 갱신
- [ ] (선택) `TutorialState` 바인딩
- [ ] Settlement 씬 로드 후에도 HUD 유지 — W2 씬 플로우 확인

---

## 6. 검증 시나리오

- [ ] NewGame → 일차 1, Prepare 표시
- [ ] `StartProduction` → 타이머 카운트다운, 페이즈 Production
- [ ] 의뢰 수락 → HUD `의뢰: 1/3`
- [ ] 결산 후 `AdvanceDay` → 일차 2
- [ ] Factory ↔ Settlement 전환 시 HUD 깜빡임 없음

---

## 7. 완료 기준

- [ ] §3 표의 필수 행(튜토 제외) 전부 표시
- [ ] 이벤트 구독 — 매 프레임 FindObject 없음
- [ ] [06-team-integration](./06-team-integration.md) HUD 항목 통과

---

## 8. 관련 문서

- [dev-contract.md](../../dev-contract.md)
- [01-tutorial-panel](./01-tutorial-panel.md)
