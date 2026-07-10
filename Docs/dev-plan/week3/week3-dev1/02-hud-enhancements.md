## HUD 보강

### 이 작업물

`GameSessionState`·`QuestManager` 이벤트로 일차·페이즈·골드·명성·의뢰 수 표시.

### 추가 표시

| UI | 데이터 |
|----|--------|
| 수락 의뢰 수 | `QuestManager.OnQuestAccepted` / `ActiveQuests.Count` |
| (선택) 튜토 단계 | `TutorialState.currentStep` |
| 생산 남은 시간 | Lead 타이머와 **중복 최소화** — Dev1 HUD는 **의뢰·튜토** 위주 |

- [ ] `GameSessionState`·튜토·`QuestManager` 이벤트 구독
- [ ] Factory·Settlement 양쪽 유지

### 독립 개발

- [ ] Dev2 미완 시 W2와 동일하게 `0/3` Mock → 금요일 `OnQuestAccepted` 연결

### 완료 기준

- [ ] 의뢰 수락 후 HUD `의뢰: 1/3` 등 표시
- [ ] 튜토 진행 시 단계 표시
