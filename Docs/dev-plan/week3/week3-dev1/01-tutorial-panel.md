# 튜토리얼 패널

**역할**: Dev1 · **Week**: 3 · **Issue**: 01  
**선행**: W2 HUD·씬 플로우 · `GameSessionState`

## 1. 이 작업물

첫 플레이 튜토리얼 패널 + 대상 강조(화살표·하이라이트).  
재생 중 게임·생산 틱 일시정지. ESC 스킵(확인 팝업).

**코드**: `TutorialPanelUI.cs`, `TutorialHighlighter.cs`, `TutorialState.cs` (`Assets/Scripts/GameFlow/`)

## 2. 확정 규칙

- 강조: 화살표 · 하이라이트 · 카메라 이동 전부
- 스킵: ESC → 「튜토리얼을 건너뛸까요?」 확인 → `TutorialState.skipped=true`
- 패널 표시 중: `Time.timeScale=0` 또는 생산 틱 정지
- W3 범위: **최소 2~3단계** (전 일차 가이드 아님)

## 3. 튜토 단계 (최소)

| step | 트리거 | 제목 (예) | 본문 (예) | 강조 |
|------|--------|-----------|-----------|------|
| 1 | 1일차 Prepare (`day==1`, NewGame 후) | 환영 | 「의뢰를 확인하세요」 | 의뢰 버튼 / O키 |
| 2 | 의뢰 UI 열림 (`orderWindow.active`) | 의뢰 수락 | 「의뢰를 선택하고 수락하세요」 | 수락 버튼 |
| 3 | (선택) 첫 배치 성공 | 생산 준비 | 「생산 시작 버튼을 누르세요」 | 생산 시작 UI |

권장 순서: `001E00001` 대화 종료 후 step 1.  
`orderWindow`는 직접 `SetActive`하지 않음 — 플레이어가 O키로 열게 유도.

확정 문구가 필요하면 `01-tutorial-panel_missingReq.md`를 채운다.

## 4. UI 스펙

| 요소 | 설명 |
|------|------|
| 패널 | 제목 · 본문 · 다음 버튼 |
| 하이라이트 | 대상 `RectTransform` 오버레이 링 또는 화살표 |
| 월드 강조 | (선택) 첫 채굴기 배치 위치 |

```csharp
public class TutorialState : MonoBehaviour {
    public int currentStep;
    public bool skipped;
    public bool IsPaused => IsShowingPanel;
}
```

## 5. 완료 기준

- [ ] 최소 2단계 패널 + 다음/스킵 동작
- [ ] 하이라이트 1곳 이상
- [ ] 패널 중 틱/게임 일시정지
- [ ] ESC 스킵 확인 후 게임 재개
- [ ] Factory·Settlement에서 HUD와 겹치지 않음
