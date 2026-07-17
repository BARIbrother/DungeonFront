# 엔딩·스토리 폴리시

> **역할**: Dev1 · **Week**: 4 · **Issue**: 04  
> **선행**: W3 [05-dialogue-story](../../week3/week3-dev1/05-dialogue-story.md) · [02-tutorial-complete](./02-tutorial-complete.md)  
> **기획**: [04-story.md](../../../04-story.md) — **대장장이 면허 · 스토리 엔딩**  
> **연동**: Lead `StoryEventBus` · Dev2 필수 의뢰 전부 클리어  
> **비범위**: 「다음 던전」 안내 · 던전 복수화

---

## 1. 이 작업물

뒷산 **MVP 스토리 엔딩**(면허 취득) 연출을 완성한다.  
필수 의뢰 라인 클리어 → 엔딩 대화 → Title/크레딧.

**코드**: `DialogueUI`, `EndingFlow` (신규 가능)

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 엔딩 대화 재생·스킵 | 전 스토리 대사 작성 (Docs/Story/) |
| 클리어 플래그 · Title 복귀 | 멀티 엔딩 · 다음 던전 훅 |
| 이브·레이 주요 비트 폴리시 | 컷신 카메라 |

---

## 3. 흐름

```
뒷산 필수 의뢰 전부 클리어 (Dev2)
  → StoryEventBus Ending
  → Dialogue (면허 취득)
  → Title 또는 Continue(자유 플레이)
```

---

## 4. 완료 기준

- [ ] 엔딩 트리거 시 대화 → 종료/타이틀
- [ ] ESC 스킵 가능
- [ ] 엔딩 후 NewGame/로드 정상

---

## 5. 관련 문서

- [04-story.md](../../../04-story.md)
- [week4-dev2/03-clear-line-lock](../week4-dev2/03-clear-line-lock.md)
