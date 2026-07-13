# 튜토리얼 패널

> **역할**: Dev1 · **Week**: 3 · **Issue**: 01  
> **선행**: W2 HUD·씬 플로우 · `GameSessionState`  
> **문구 출처**: [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md) · [01-core-loop.md](../../../01-core-loop.md) 튜토 순서  
> **연동**: Lead `StoryEventBus` · `IFactoryProduction`  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

첫 플레이 **튜토리얼 패널** + **대상 강조**(화살표·하이라이트).  
재생 중 **게임·생산 틱 일시정지**, ESC 스킵(확인 팝업) — [01-core-loop.md](../../../01-core-loop.md).

**코드**: `Assets/Scripts/GameFlow/TutorialPanelUI.cs`, `TutorialHighlighter.cs`, `TutorialState.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 2~3단계 패널 + 다음/스킵 | 전체 40일 튜토 |
| UI Rect·월드 오브젝트 하이라이트 | Lead Factory 맵 편집 |
| 틱 정지 (`IFactoryProduction` / Time.timeScale) | Dev2 의뢰 로직 |

---

## 3. 튜토 단계 (최소 3단계)

[01-core-loop.md](../../../01-core-loop.md) 「첫 플레이 튜토리얼 순서」와 정렬:

| step | 트리거 | 제목 (예) | 본문 (예) | 강조 대상 |
|------|--------|-----------|-----------|-----------|
| 1 | 1일차 Prepare 진입 (`day==1`, `StoryEventBus` 또는 NewGame 후) | 환영 | 「의뢰를 확인하세요」 | 의뢰 버튼 / O키 안내 |
| 2 | 의뢰 UI 열림 (`orderWindow.active`) | 의뢰 수락 | 「의뢰를 선택하고 수락하세요」 | 수락 버튼 |
| 3 | (선택) 첫 기계 배치 성공 | 생산 준비 | 「생산 시작 버튼을 누르세요」 | 생산 시작 UI |

**문구 확정**: [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md) 1일차 표 — 없으면 위 예시로 placeholder 후 기획 갱신.

---

## 4. UI 스펙

| 요소 | 설명 |
|------|------|
| 패널 | 화면 하단 또는 중앙 — **제목**·**본문**·**다음** 버튼 |
| 스킵 | ESC → 「튜토리얼을 건너뛸까요?」 확인 → `TutorialState.skipped=true` |
| 하이라이트 | 대상 UI의 RectTransform에 **오버레이 링** 또는 화살표 |
| 월드 강조 | (선택) 첫 채굴기 배치 위치 — `TutorialHighlighter` 월드 좌표 |
| 일시정지 | 패널 표시 중 `Time.timeScale=0` **또는** Lead `IFactoryProduction.StopTick()` |

### Dev1이 토글하지 않는 것

- `orderWindow` 직접 `SetActive` — 플레이어가 O키로 열게 유도만
- Dev2 상점·결산 패널

---

## 5. 상태 관리

```csharp
public class TutorialState : MonoBehaviour {
    public int currentStep;
    public bool skipped;
    public bool IsPaused => IsShowingPanel;
}
```

- `currentStep` — HUD 표시용 ([02-hud-enhancements](./02-hud-enhancements.md))
- 세이브: W3 선택 — `SaveData.tutorialStep` (Dev1 DTO에 필드 추가 시 contract 공지)

---

## 6. 구현 단계

- [ ] `TutorialPanelUI` — Show(step), Hide, OnNext, OnSkip
- [ ] `GameSessionState.OnPhaseChanged` — 1일차 Prepare → step 1
- [ ] 의뢰 UI 열림 감지 — `orderWindow.activeSelf` 폴링 또는 Dev2 이벤트 (읽기만)
- [ ] `TutorialHighlighter` — `RectTransform` 또는 `GameObject` 참조
- [ ] 일시정지 — Production 중이면 `TickManager` 정지 확인
- [ ] 스킵 시 남은 step 무시, `OnStoryEvent`와 중복 방지 합의

---

## 7. Lead 연동

| API | 용도 |
|-----|------|
| `IFactoryProduction` / `TickManager` | 튜토 중 틱 정지 |
| `StoryEventBus` | 오프닝 대화와 순서 — 대화 먼저 vs 패널 먼저 팀 합의 |

권장: `001E00001` 대화 종료 후 step 1 패널.

---

## 8. 검증 시나리오

- [ ] NewGame → 1일차 Prepare → 튜토 step 1 표시
- [ ] 「다음」→ step 2, 강조 대상 변경
- [ ] 패널 열림 중 플레이어 이동·틱 정지
- [ ] ESC 스킵 → 패널 닫힘·게임 재개
- [ ] [06-team-integration](./06-team-integration.md) 통합 데모 2단계

---

## 9. 완료 기준

- [ ] §3 최소 2단계 동작 (3단계 권장)
- [ ] 하이라이트 1곳 이상 동작
- [ ] 일시정지 검증 통과
- [ ] Factory·Settlement 씬 모두에서 HUD와 겹치지 않음

---

## 10. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md)
- [05-dialogue-story](./05-dialogue-story.md)
- [week3-lead/06-story-hooks](../week3-lead/06-story-hooks.md)
