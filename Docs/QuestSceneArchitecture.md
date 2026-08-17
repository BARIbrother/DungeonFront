# 퀘스트·씬 구조 (현재 기준)

## 실제 플레이 씬

`Assets/Scenes/Factory.unity`가 현재 게임의 단일 메인 씬이다.

하루의 세 단계는 씬 전환이 아니라 `GameSessionState.Phase`로 전환한다.

```
Prepare(준비)
  ├ 의뢰 수락: QuestAcceptUI
  ├ 기계 배치 / 상점·해금
  └ 생산 시작
        ↓
Production(생산)
  └ 생산 종료 요약
        ↓
Settlement(결산)
  ├ SettlementQuestListUI: 진행 의뢰 전량 납품
  ├ 의뢰 카드: 보유/요구 수량 표시
  └ 다음 일차
```

`QuestSystemRoot`는 Factory 안에 존재하며 `QuestManager`, `QuestPool`, 납기, 경제·해금, 진행도 서비스를 유지한다. `QuestManager`는 `DontDestroyOnLoad`이지만 현재 정상 플레이에서는 Factory를 벗어나지 않는다.

## Build Settings

- **Factory**: 유일하게 활성화된 시작 씬
- `DungeonFront`, `ProductionScene`, `Settlement`: 이전/개별 테스트 씬이므로 Build에서 비활성화

`Settlement.unity`에는 별도 `QuestManager`와 `QuestPool`가 있어 독립 실행하면 Factory의 런타임 퀘스트 상태와 다른 시스템이 만들어질 수 있다. 정식 결산은 이 씬이 아니라 Factory의 `Settlement` 페이즈에서 진행한다.

## 퀘스트 표시 원칙

- 준비: 수락 가능한 일반·스토리 의뢰
- 생산: 재고만 축적, 퀘스트는 직접 생산을 강제하지 않음
- 결산: 수락한 의뢰를 전량 납품. 부분 납품 없음
- 상시 의뢰: 수락 없이 결산에서 반복 납품하는 별도 흐름
- HUD: 실제 `QuestManager.currentQuests.Count / 3`만 표시. K키 Mock은 제거됨.

## 스토리 시작

Day 1 Prepare 진입 → `001E00001` 오프닝 → 닫기 → `001E00002` 첫 의뢰 안내 → 닫기 → 튜토리얼 → 의뢰 수락 순서다.

대사는 `StoryEventBus` 이벤트를 `DialogueUI`가 받아 표시한다. 이벤트 발행은 `FactoryStoryHooks`가 맡는다.
