# 키바인딩

같은 키가 두 곳에 묶여 있지 않게 맞춘 현재 입력이다.

## 플레이

| 키 | 동작 | 위치 |
|----|------|------|
| WASD / 화살표 | 이동 | `PlayerMovement` |
| **T** | 테크 트리만 열기/닫기 | `PlayerMovement` → `TechTreeUI` |
| **B** | 배치 모드만 토글 | `PlacementController` |
| **O** | 상점 | `EconomyHubUI` |
| **F** | 생산 즉시 종료 | `PlayerMovement` → `GameSessionState.ForceEndProduction` |
| **E** | 인벤토리 | `PlayerMovement` |
| **1** | 기계 제작 UI | `PlayerMovement` |
| Shift+1 | 기계 지급 치트 | `PlayerMovement` |
| **2** | 구역 해금 UI | `PlayerMovement` |
| **R** | 컨베이어 회전 (배치 중) | `PlacementController` |
| **Space** | 대화 다음 줄 / 튜토리얼 닫기(건너뛰기 확인). 대화·튜토 중에는 수리 모션 없음 | `DialogueUI`, `TutorialPanelUI` |
| **P** 홀드 1초 | 대화 스킵 | `DialogueUI` |
| **Enter** | 대화 다음 줄 | `DialogueUI` |
| **Esc** | 일시정지 | `PauseMenuController` |

생산 시작은 키가 없다. 일차 표시 바로 아래 **생산 시작** 버튼을 누른다.

의뢰 창도 단축키가 없고 HUD **퀘스트** 버튼만 있다.

## 개발

| 키 | 동작 | 위치 |
|----|------|------|
| F8 | Dev Mode 패널 | `QuestSystemDebugPanel` |
| F10 | GDC 테스트 패널 | `GdcFeatureTestPanel` |
| M / N | 기계 수량 디버그 | `MachineCountHUD` |
| G | 게임오버 화면에서 확인 | `GameOverController` |

## 관련 코드

- `Assets/Scripts/Player/PlayerMovement.cs`
- `Assets/Scripts/GameFlow/TickManager.cs`
- `Assets/Scripts/GameFlow/GameSessionState.cs`
- `Assets/Scripts/GameFlow/UIManager.cs`
- `Assets/Scripts/GameFlow/EconomyHubUI.cs`
- `Assets/Scripts/Placement/PlacementController.cs`
- `Assets/Scripts/GameFlow/DialogueUI.cs`
- `Assets/Scripts/GameFlow/TutorialPanelUI.cs`
