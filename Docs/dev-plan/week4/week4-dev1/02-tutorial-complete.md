# 튜토리얼 완성

> **역할**: Dev1 · **Week**: 4 · **Issue**: 02  
> **선행**: W3 [01-tutorial-panel](../../week3/week3-dev1/01-tutorial-panel.md) · [01-mvp-ui-polish](./01-mvp-ui-polish.md)  
> **기획**: [01-core-loop.md](../../../01-core-loop.md) · [04-story.md](../../../04-story.md)  
> **연동**: Lead `StoryEventBus` · Dev2 첫 의뢰  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

뒷산 **첫 플레이 튜토리얼**을 W3 최소 2~3단계에서, 기획 순서대로 **완주 가능한 세트**로 채운다.  
강조·일시정지·스킵이 실제 클리어 플로우와 맞아야 한다.

**코드**: `TutorialPanelUI`, `TutorialHighlighter`, `TutorialState`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 1일차 Prepare·Production·Settlement 튜토 단계 | 전 일차 40일 가이드 |
| 하이라이트·틱 정지·ESC 스킵 | Lead 맵 편집 |
| 이브 안내와 순서 합의 | 다른 던전 튜토 |

---

## 3. 단계 (기획 정렬)

[01-core-loop.md](../../../01-core-loop.md) 「첫 플레이 튜토리얼 순서」 기준으로 채움. 예:

| step | 페이즈 | 내용 |
|------|--------|------|
| 1 | Prepare | 의뢰 확인 |
| 2 | Prepare | 의뢰 수락 |
| 3 | Prepare | 배치·생산 시작 |
| 4 | Production | 수작업/운반 안내 (선택) |
| 5 | Settlement | 납품 안내 |

문구: [04-story](../../../04-story.md) / Story 폴더 — 없으면 placeholder 후 기획 갱신.

---

## 4. 완료 기준

- [ ] NewGame → 튜토 단계가 끊기지 않고 첫 납품까지 유도
- [ ] 패널 중 틱 정지 · ESC 스킵 확인
- [ ] 스킵 후에도 게임 진행 가능

---

## 5. 관련 문서

- [04-ending-story-polish](./04-ending-story-polish.md)
- [week3-dev1/01-tutorial-panel](../../week3/week3-dev1/01-tutorial-panel.md)
