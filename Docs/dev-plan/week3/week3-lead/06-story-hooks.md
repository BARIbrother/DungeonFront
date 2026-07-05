## 스토리 이벤트 트리거·전 구간 연동

### 이 작업물

Factory **이벤트 훅** + Dev 스토리·튜토 **전 구간** 연동. 구 7주 **5~6주**.

### 발행 이벤트

| 이벤트 | 시점 |
|--------|------|
| `OnPrepareEntered` | Prepare (일차 포함) |
| `OnProductionStarted` | 생산 시작 후 |
| `OnProductionEnded` | 요약 직전/직후 |
| `OnMachinePlaced` | 배치 (type, grid) |

### 연동

- [ ] `FactoryStoryHooks` — Dev `StoryEventRunner` 구독 API
- [ ] [week1 07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md) 1일차 트리거 id 매핑
- [ ] 튜토~클리어 Factory 쪽 트리거 **전 구간** (기획 표 기준)

### 범위 밖 (Dev)

대화 UI, 튜토 패널, 카메라 연출

### 완료 기준

- [ ] 1일차 Prepare 진입 이벤트 Dev 수신 가능
- [ ] 기획 표 트리거 id와 이벤트 일치
