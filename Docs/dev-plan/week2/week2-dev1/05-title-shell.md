## 타이틀 씬 껍데기

### 이 작업물

W3 MVP용 **Title** 씬·슬롯 UI **껍데기**. W3에 세이브 연동.

**코드**: `Assets/Scripts/GameFlow/TitleController.cs` (신규)  
**씬**: Title *(Dev1 소유)*

### UI (W2 최소)

- [ ] **새 게임** — `GameSessionState.NewGame()` + Factory 씬 로드
- [ ] **불러오기** — 버튼만 (W3 `SaveLoad` 연동, W2는 비활성 또는 토스트)
- [ ] 슬롯 1~3 placeholder

### 독립 개발

- [ ] W2는 **세이브 없이** 새 게임 → Factory 진입만 동작
- [ ] Lead·Dev2 완료 불필요

### 완료 기준

- [ ] Play → Title → 새 게임 → Factory
- [ ] W3 `SaveLoad` 연결 지점 주석·public 메서드 준비
