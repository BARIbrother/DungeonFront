# 경제 수치 lock

> **역할**: Dev2 · **Week**: 4 · **Issue**: 01  
> **선행**: W3 [02-economy-unlock](../../week3/week3-dev2/02-economy-unlock.md)  
> **연동**: Lead 구역 비용  
> **기획**: [01-core-loop.md](../../../01-core-loop.md) · [dev-gaps.md](../../dev-gaps.md) §5  
> **계약**: [dev-contract.md](../../dev-contract.md)  
> **던전**: 뒷산 (단일 골드·명성)

---

## 1. 이 작업물

뒷산에서 쓰는 **골드 단가·구역 비용·명성 해금**을 표로 고정하고 SO/테이블에 반영한다.

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 재료·기계 구매가 (뒷산 MVP 세트) | 던전별 명성·PTW |
| 구역 확장 비용 | 동적 시세 |
| 명성 해금 임계값 | UI 전면 리디자인 |

---

## 3. lock 표 (초안 — 팀 확정 후 교체)

### 구역

| zoneId | gold |
|--------|------|
| `zone_east_1` | 100 |
| `zone_north_1` | 150 |

### 재료 단가 (예)

| itemId | buyGold |
|--------|---------|
| `iron_ore` | 2 |
| `iron_ingot` | 8 |
| `iron_plate` | 20 |

명성 해금: W3 + [03-machine-plan](../../../03-machine-plan.md) 동기화.

---

## 4. 완료 기준

- [ ] 가격 단일 소스
- [ ] Lead 구역·Dev2 상점 금액 일치
- [ ] 표 문서화

---

## 5. 관련 문서

- [week4-lead/02-zone-expansion](../week4-lead/02-zone-expansion.md)
