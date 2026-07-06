## HUD 보강

### 이 작업물

[week1 03-global-hud](../../week1/week1-dev/03-global-hud.md) 확장. 구 7주 **3주** HUD 항목.

### 추가 표시 (Week 3)

| UI | 데이터 |
|----|--------|
| 수락 의뢰 수 | `session.quests.Count` / 3 |
| (선택) 튜토 단계 | `TutorialState.currentStep` |
| (선택) 생산 남은 시간 | Lead 타이머와 **중복 최소화** — Dev HUD는 **의뢰·튜토** 위주 |

- [ ] `GameSessionState`·튜토 상태 구독
- [ ] Factory·Settlement 양쪽 유지

### 완료 기준

- [ ] 의뢰 수락 후 HUD `의뢰: 1/3` 등 표시
- [ ] 튜토 진행 시 단계 표시 (구현 시)
