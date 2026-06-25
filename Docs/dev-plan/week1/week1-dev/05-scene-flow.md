## 씬·페이즈 흐름

### 이 작업물

**페이즈**에 따라 **Factory** vs **Settlement** 를 보여 주는 오케스트레이터.  
[01-session-phase](./01-session-phase.md)의 상태를 읽고, Lead·결산 UI를 연결한다.

**코드**: `Assets/Scripts/` (예: `GameFlowController.cs`)  
**씬**: `Factory`, `Settlement` — Build Settings에 **둘 다** 등록

### 페이즈 ↔ 화면

| GamePhase | 활성 화면 | 비고 |
|-----------|-----------|------|
| Prepare | Factory | Lead 맵·이동·생산 시작 버튼 |
| Production | Factory | Lead 타이머 UI |
| Settlement | Settlement 스텁 | [04-settlement-stub](./04-settlement-stub.md) |

### 구현 체크리스트

#### 씬 로드

- [ ] 앱 시작 또는 `NewGame()` → **Factory** 씬 로드 (Single)
- [ ] `phase == Settlement` → **Settlement** 씬 로드 (또는 Factory 언로드 후 Settlement)
- [ ] `AdvanceDay()` 후 → 다시 **Factory**

#### Lead 연동

- [ ] Lead `StartProduction()` 호출 경로 → `GamePhaseManager.StartProduction()`
- [ ] `OnNewGame` → Lead `PlayerSpawner` (또는 동등 API) 호출
- [ ] `ProductionRemainingSeconds`를 Lead 타이머 UI가 읽음

#### 루프 (Update 또는 코루틴)

- [ ] Production 중 매 프레임/초: 남은 시간 ≤ 0 → `SetPhase(Settlement)` + Settlement 표시
- [ ] Settlement 「다음 날」→ `AdvanceDay()` + Factory 표시

### 부트스트랩 (Week 1)

- [ ] Play 시 자동 `NewGame()` **또는** 디버그 「새 게임」버튼 하나 (팀 선택, 문서에 명시)

### 완료 기준

- [ ] Play → Factory → (생산 시작) → Production → (만료/디버그) → Settlement → (다음 날) → Factory, **일차 2**
- [ ] 전환 중 크래시·씬 중복 로드 없음
- [ ] [03-global-hud](./03-global-hud.md)가 전 구간에서 갱신됨
