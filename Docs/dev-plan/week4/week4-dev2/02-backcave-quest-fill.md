# 뒷산 의뢰 채우기

> **역할**: Dev2 · **Week**: 4 · **Issue**: 02  
> **선행**: W3 Quest SO · [01-economy-numbers-lock](./01-economy-numbers-lock.md)  
> **기획**: [05-quest.md](../../../05-quest.md) · [04-story.md](../../../04-story.md) · [dev-gaps.md](../../dev-gaps.md) §9  
> **비범위**: 혹한·다른 던전 의뢰

---

## 1. 이 작업물

뒷산 동굴용 **의뢰 SO**의 빈 칸(표시명·요구·납기·보상·필수 여부)을 채운다.  
스토리 필수와 일반 풀을 구분해 둔다 — 클리어 순서 lock은 [03](./03-clear-line-lock.md).

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| `001xxxxx` 뒷산 의뢰 SO 필드 완성 | `002xxxxx` 등 타 던전 |
| 요구 itemId = Lead 생산 가능 목록 | 스토리 대사 전문 |
| 골드·명성 보상 = [01](./01-economy-numbers-lock.md) | 해석형·특수 기믹 의뢰 |

---

## 3. 체크리스트

- [ ] [04-story.md](../../../04-story.md) 필수 의뢰 표 — questId·표시명·납기·보상 기입
- [ ] 일반 풀 최소 N종 (팀 합의, 예: 8+)
- [ ] 멀티데이 샘플 유지·보강
- [ ] Lead와 `requiredItems` 생산 경로 대조

---

## 4. 완료 기준

- [ ] 필수·일반 SO null/미정 필드 0 (의도적 TBD 문서화만 허용)
- [ ] Prepare 풀에서 수락·결산 납품 가능

---

## 5. 관련 문서

- [03-clear-line-lock](./03-clear-line-lock.md)
- [04-quest-content-fill](./04-quest-content-fill.md)
