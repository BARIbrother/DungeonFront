## 글로벌 HUD

### 이 작업물

**Factory**와 **Settlement** 양쪽에서 보이는 **메타 정보** UI. 일차·페이즈·골드·명성만 표시한다.  
생산 타이머·생산 시작 버튼은 Lead [05-factory-production-ui](../week1-lead/05-factory-production-ui.md).

**코드**: `Assets/Scripts/` (예: `GlobalHUD.cs`)  
**씬**: DontDestroyOnLoad Canvas **또는** Factory·Settlement 각 씬에 동일 Prefab

### 표시 항목

| UI 라벨 | 데이터 소스 | 예시 |
|---------|-------------|------|
| 일차 | `session.day` | `1일차` |
| 페이즈 | `session.phase` | `준비` / `생산` / `결산` (한글 표시) |
| 골드 | `session.gold` | `골드: 0` |
| 명성 | `session.reputation` | `명성: 0` |

- [ ] `GameSessionState` 변경 시 **자동 갱신** (이벤트 구독 또는 Update)
- [ ] 페이즈 한글 매핑 테이블을 코드 한곳에 정의

### 레이아웃 (Week 1)

- [ ] 화면 **한쪽 모서리** (예: 좌상단)에 4줄 텍스트
- [ ] 폰트·색 — placeholder OK (Art UI 가이드 후속)

### 씬 전환 시

- [ ] Factory → Settlement 이동해도 HUD **유지** (같은 Canvas 또는 씬마다 재바인딩)

### 완료 기준

- [ ] Play 후 HUD에 일차 1, 준비, 골드 0, 명성 0
- [ ] `StartProduction()` 후 페이즈가 **생산**으로 바뀜
- [ ] Settlement 진입 후 **결산** 표시
- [ ] `AdvanceDay()` 후 일차 2, **준비**
