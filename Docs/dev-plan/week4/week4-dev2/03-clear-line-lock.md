# 클리어 라인 lock

> **역할**: Dev2 · **Week**: 4 · **Issue**: 03  
> **선행**: [02-backcave-quest-fill](./02-backcave-quest-fill.md)  
> **기획**: [04-story.md](../../../04-story.md) · [01-core-loop.md](../../../01-core-loop.md) — 면허 취득  
> **연동**: Dev1 [04-ending-story-polish](../week4-dev1/04-ending-story-polish.md)  
> **비범위**: 다음 던전 해금

---

## 1. 이 작업물

뒷산 **필수(스토리) 의뢰 순서·해금 연동**을 lock하고, **전부 클리어 시 엔딩 이벤트**를 발행한다.

**코드**: `QuestManager` — 필수 완료 체크 · `OnBackCaveCleared` / `StoryEventBus` 연동

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 필수 의뢰 목록·순서·선행 조건 | 타 던전 메인 임무 |
| 클리어 시 기계/레시피 해금 트리거 | 엔딩 UI (Dev1) |
| `allMandatoryComplete` → 엔딩 훅 | 멀티 엔딩 |

---

## 3. 클리어 라인 (표 — 기획 확정본으로 교체)

| # | questId | 클리어 시 | 해금·비고 |
|---|---------|-----------|-----------|
| 1 | `00100001` | 튜토 | 시작 기계 |
| 2 | `00100002` | 마법 라인 | 마나 계열 |
| … | … | … | … |
| N | `00100NNN` | **엔딩** | 면허 |

- [ ] [04-story.md](../../../04-story.md)와 동일 표 유지
- [ ] 실패(필수 미납) → 게임오버 회귀 (W3)

---

## 4. 구현 단계

- [ ] 필수 목록 SO/코드 상수
- [ ] 전부 `Completed`일 때 Dev1 엔딩 이벤트 1회
- [ ] 스토리 only 압축 일차 모드와 호환 (있으면)

---

## 5. 완료 기준

- [ ] 필수 N개 클리어 시에만 엔딩 트리거
- [ ] 중간 필수 스킵 불가 (또는 기획대로)
- [ ] Dev1이 이벤트 1회로 엔딩 재생 가능

---

## 6. 관련 문서

- [04-story.md](../../../04-story.md)
- [week4-dev1/04-ending-story-polish](../week4-dev1/04-ending-story-polish.md)
