## Factory 생산 UI

### 이 작업물

**준비·생산** 단계에서 Factory 화면 위에 보이는 UI.  
「생산 시작」버튼과 **5분 타이머**를 Lead(공장) 쪽에 둔다. 페이즈 **상태**는 Dev `GameSessionState`가 소유한다.

**위치**: `Factory` 씬 Canvas (Screen Space 또는 World Space — 팀 선택)  
**코드**: `Assets/Scripts/` (예: `FactoryProductionUI.cs`)

### UI 요소

| 요소 | 표시 조건 | 동작 |
|------|-----------|------|
| **생산 시작** 버튼 | `GamePhase == Prepare` | 클릭 → Dev에 `StartProduction()` 요청 |
| **남은 시간** 텍스트 | `GamePhase == Production` | `MM:SS` 형식, Dev가 준 **종료 시각** 기준 카운트다운 |
| (Week 1) **디버그** 버튼 | 항상 또는 개발 빌드만 | Prepare / Production / Settlement / 다음 날 — Dev 페이즈 API 직접 호출 |

### 규격 (`01-core-loop.md`)

| 항목 | 값 |
|------|-----|
| 생산 길이 | **300초 (5분)** |
| 생산 시작 버튼 위치 | 화면 **중앙 상단** (비전 문서) |
| 조기 종료 | **없음** (Week 1은 타이머만) |
| 경고 UI | 미배치 기계·무의뢰 경고 — **Week 1 생략 가능** |

### Dev와의 계약 (인터페이스)

Lead UI는 아래 중 팀이 정한 방식으로 Dev와 연결:

- [ ] `IGamePhaseService.StartProduction()` — Production 전환 + `productionEndTime = Now + 300s`
- [ ] `IGamePhaseService.Phase` 구독 — UI 표시/숨김 갱신
- [ ] `IGamePhaseService.ProductionRemainingSeconds` — 타이머 표시용

(구체 클래스명은 자유. **양쪽 합의한 public API**만 문서화)

### 디버그 버튼 (Week 1 필수)

Dev 연동 전에도 테스트할 수 있게 Factory 씬에:

- [ ] `→ 준비` / `→ 생산` / `→ 결산` / `→ 다음 날` 중 최소 **생산 시작 + 결산** 트리거

### 완료 기준

- [ ] Prepare에서 **생산 시작** 버튼이 보이고, 클릭 시 Production으로 바뀐다 (Dev 연동 시)
- [ ] Production 중 **5:00 → 0:00** 카운트다운이 보인다
- [ ] Production 종료 시 Dev가 Settlement로 넘기면 버튼/타이머가 알맞게 숨겨진다
- [ ] 디버그로 페이즈를 수동 전환할 수 있다
