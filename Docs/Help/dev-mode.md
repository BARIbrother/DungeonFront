# Dev Mode 사용법

플레이 전체를 돌리지 않고, 세션·퀘스트·인벤·스토리를 점프하며 검증하기 위한 개발용 패널입니다.

| | |
|--|--|
| 토글 | **F8** |
| 위치 | Game 뷰 왼쪽 OnGUI 창 |
| 코드 | [`QuestSystemDebugPanel.cs`](../Assets/Scripts/Quest/Week4/QuestSystemDebugPanel.cs), [`DevModeCommands.cs`](../Assets/Scripts/Dev/DevModeCommands.cs) |
| 프리팹 | `QuestSystemRoot` → `enableDevMode` |

---

## 켜는 조건

다음을 **모두** 만족해야 합니다.

1. **Unity Editor**이거나 **Development Build**
2. 씬에 `QuestSystemDebugPanel`이 있고 `enableDevMode`가 켜져 있음  
   (`Assets/Prefabs/Quest/QuestSystemRoot.prefab` 기본값 ON)
3. Play 모드에서 **F8**

릴리스 플레이어 빌드에서는 패널·명령이 컴파일되지 않습니다.

상단 상태줄 예:

```text
Day 1 / Prepare / Gold 100 / Rep 10 / Active 0 / TestMode=ON
```

---

## 탭 안내

### 1. Session — 일차·페이즈·재화

| 버튼 / 입력 | 동작 |
|-------------|------|
| **NewGame** | Day1·Prepare·골드/명성 초기화, 퀘스트 상태 비움 |
| **Prepare / Production / Settlement** | 페이즈 강제 (Production은 가능하면 `StartProduction` 경로) |
| **Day** + Set / ±1 | 일차 절대값 변경 |
| **Gold / Rep** + Set | 재화 절대값 |
| **G+500 / R+500** | 증감 |
| 상점 버튼 | 철광석 구매·제단 해금/구매 (기존 QA) |

**이런 때 쓰기**

- Settlement·기한·게임오버만 보고 싶을 때 Day/Phase만 점프
- 명성 해금 구간을 바로 맞추고 싶을 때 Rep Set

---

### 2. Quests — 의뢰 파이프라인

#### 진행도

| 동작 | 설명 |
|------|------|
| 완료 목록 | `QuestProgressionService`에 기록된 id |
| **Restore** | 입력한 메인 id까지 선행 메인을 일괄 완료로 맞춤 |
| **Reset** | 진행도 비움 |

메인 id 예 (SO `Quest.id`):

| 문서 | id |
|------|-----|
| Q001 | `00100001` |
| Q002 | `00100002` |
| Q003 | `00100003` |
| Q005 | `00100005` |
| Q010 | `00100010` |
| … | … |

사이드(예: Q012)를 바로 열려면, 그 열이 묶인 **직전 메인까지** Restore한 뒤 풀을 새로고침하세요.  
예: Q005 열 사이드 → `00100003`(Q003)까지 Restore.

#### SO 카탈로그

`QuestDatabase` / `Assets/Quest/**` 목록.

| 버튼 | 동작 |
|------|------|
| **오퍼** | 오늘 수락 가능 목록에 강제 추가 |
| **수락** | 오퍼 후 즉시 수락 |
| **원클릭 납품** | 수락 → 요구 아이템 지급 → 납품 |

보상 미리보기는 각 카드에 표시됩니다.

#### 오늘 목록 / 진행 중 / 상시

기존 QA와 동일합니다.

- 수락 · 요구 지급 · 납품 · 원클릭
- 상시: 재료 ×2 · ×1 납품
- D-day −1 · D-0 미납 판정
- 메모리 Export / ClearActive / Import

---

### 3. Inventory — 아이템

| 동작 | 설명 |
|------|------|
| **itemId / count / level** + 지급 | 임의 아이템 추가 (`iron_ore`, `Gold` 등) |
| **활성 의뢰 요구 일괄 지급** | 진행 중 의뢰 요구분을 모두 넣음 |
| **아이템 비우기** | 기계 제외, 아이템만 Clear |

하단에 현재 보유 스택이 표시됩니다.

---

### 4. Story — 이벤트 버스

| 동작 | 설명 |
|------|------|
| **Raise** | `StoryEventBus.Raise(id)` |
| 목록 버튼 | `001E00001` 등 알려진 id 즉시 Raise |
| **Reset fired** | `FactoryStoryHooks`의 1회성 발화 기록 초기 |

스토리 id 매핑은 [`Docs/04-story.md`](./04-story.md) · `FactoryStoryHooks` 주석을 참고하세요.

---

### 5. Smoke — 자동 한 바퀴

**Run Q001 Smoke** 한 번으로:

1. NewGame  
2. Q001(`00100001`) 오퍼·수락  
3. 요구 지급 → 납품  
4. Gold/Rep 증가 확인  
5. 진행도 기록 확인  
6. 메인 선행 Restore(Q001~Q002) 확인  

결과는 패널과 Console에 `OK` / `FAIL` / `SMOKE PASSED|FAILED`로 남습니다.

---

## 추천 워크플로

### A. 새 퀘스트 SO가 맞는지

1. F8 → **Quests** → 카탈로그 새로고침  
2. 해당 의뢰 **수락** → 보상 미리보기 확인  
3. **원클릭 납품** → Gold/Rep·진행도 확인  

### B. 중반 사이드만 검증

1. **Session** NewGame (선택)  
2. **Quests**에서 직전 메인 id **Restore** → 풀 새로고침  
3. 사이드 **수락** → Inventory로 요구 맞추거나 원클릭  

### C. 기한·결산만

1. 의뢰 수락  
2. **D-day -1** 반복 또는 Day 점프  
3. **Settlement** 강제 → **D-0 미납**  

### D. 회귀 스모크

1. **Smoke** → Run Q001 Smoke  
2. FAIL이면 Console의 첫 FAIL 줄부터 추적  

---

## 같이 쓰는 기존 핫키 (Dev Mode 밖)

아직 패널에 안 묶인 단축키입니다.

| 키 | 용도 |
|----|------|
| **T** | 틱/생산 시작 |
| **F** | Production 강제 종료 |
| **1** | 기계 지급 UI |
| **G** | 게임오버 강제 |

제작 포트 원클릭·레시피 전체 해금은 Dev Mode 2차 범위입니다.

---

## 관련 경로

| 내용 | 경로 |
|------|------|
| 퀘스트 SO | `Assets/Quest/{메인id}/Q###.asset` |
| Quest DB | `Assets/Resources/Data/QuestDatabase.asset` |
| questline JSON | `Assets/Data/Quest/questline.json` |
| 퀘스트 문서 | [`Docs/quest/README.md`](./quest/README.md) |
| SO 재생성 | Unity 메뉴 `DungeonFront/Generate Quest Assets From Questline` |

패널이 안 보이면: Play 중인지, `QuestSystemRoot`가 씬에 있는지, Inspector의 **Enable Dev Mode**가 켜져 있는지 확인하세요.
