## GameSessionState·페이즈 전환

### 이 작업물

게임 전체 **하루 루프의 상태**를 들고 있고, **페이즈를 바꾸는 규칙**을 구현한다.  
UI·씬은 다른 Issue; 여기서는 **데이터 + 전환 API**만.

**코드**: `Assets/Scripts/` (예: `GameSessionState.cs`, `GamePhaseManager.cs`)

### 페이즈 정의

```csharp
enum GamePhase {
    Prepare,      // 준비 — Factory
    Production,   // 생산 5분 — Factory
    Settlement    // 결산 — Settlement 화면
}
```

순환: `Prepare → Production → Settlement → (day++) → Prepare`

### `GameSessionState` 필드 (Week 1)

| 필드 | 타입 | 초기값 (NewGame) |
|------|------|------------------|
| `day` | int | 1 |
| `phase` | GamePhase | Prepare |
| `gold` | int | 0 |
| `reputation` | int | 0 |
| `inventory` | InventoryState | [02-new-game-inventory](./02-new-game-inventory.md) |
| `factory` | FactoryState | 빈 맵 (배치 목록 empty) |
| `quests` | List\<AcceptedQuestState\> | 빈 목록 |
| `productionEndTime` | float 또는 DateTime | Production 아닐 때 null/0 |

### 전환 API (public)

- [ ] `void SetPhase(GamePhase next)` — 유효한 전환만 허용 (아래 표)
- [ ] `void StartProduction()` — Prepare → Production, `productionEndTime = Now + 300f` 초
- [ ] `void TickProduction()` 또는 Update에서 — `Now >= productionEndTime` → Settlement
- [ ] `void AdvanceDay()` — Settlement에서만 호출, `day++`, phase → Prepare

| From | To | 트리거 |
|------|-----|--------|
| Prepare | Production | `StartProduction()` (Lead 생산 시작 버튼) |
| Production | Settlement | 타이머 만료 |
| Settlement | Prepare | `AdvanceDay()` (결산 스텁 「다음 날」) |

### Lead가 읽을 값

- [ ] `GamePhase Phase { get; }`
- [ ] `float ProductionRemainingSeconds { get; }` — Production일 때만 > 0
- [ ] (선택) `event Action OnPhaseChanged`

### 완료 기준

- [ ] `NewGame` 후 phase = Prepare, day = 1
- [ ] `StartProduction()` 후 phase = Production, 남은 시간 ≈ 300초
- [ ] 시간 만료(또는 테스트용 `ForceEndProduction()`) 후 phase = Settlement
- [ ] `AdvanceDay()` 후 day = 2, phase = Prepare
- [ ] 잘못된 전환(예: Prepare → Settlement 직접)은 거부되거나 로그 경고
