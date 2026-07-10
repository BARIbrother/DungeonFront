## 튜토리얼 패널

### 이 작업물

**튜토리얼 패널** + **대상 강조**(화살표·하이라이트).  
문구: [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md) 1일차 이벤트.

**코드**: `Assets/Scripts/GameFlow/` (예: `TutorialPanelUI.cs`, `TutorialHighlighter.cs`)

### 범위 (2~3단계)

| 순서 | 트리거 | 내용 |
|------|--------|------|
| 1 | 1일차 Prepare 진입 | 오프닝 요약 또는 「의뢰를 확인하세요」 |
| 2 | 의뢰 UI 열림 | 수락 방법 안내 |
| 3 | (선택) 첫 배치 후 | 생산 시작 안내 |

### UI

- [ ] 패널 — 제목·본문·**다음** 버튼
- [ ] 재생 중 **일시정지** — `IFactoryProduction.StopTick()` 호출
- [ ] **스킵** 버튼 (ESC 확인은 polish)

### 강조

- [ ] UI Rect 또는 월드 오브젝트 **하이라이트** (테두리·화살표 placeholder)
- [ ] 대상: 의뢰 버튼, 생산 시작 버튼 등

### 독립 개발

- [ ] 의뢰 UI는 Dev2 소유 — **하이라이트 대상만** Rect 참조 또는 이름 태그
- [ ] 틱 정지는 `IFactoryProduction` 인터페이스 — Lead 미완 시 스텁

### 완료 기준

- [ ] 1일차 NewGame 후 튜토 1단계 표시
- [ ] 「다음」으로 2단계 진행
- [ ] 패널 열림 중 Production 틱 미진행
