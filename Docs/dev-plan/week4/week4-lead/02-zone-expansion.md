# 구역 확장

> **역할**: Lead · **Week**: 4 · **Issue**: 02  
> **선행**: [01-mvp-factory-polish](./01-mvp-factory-polish.md) · GridManager  
> **연동**: Dev2 골드 차감  
> **기획**: [00-vision.md](../../../00-vision.md) · [01-core-loop.md](../../../01-core-loop.md) · [dev-gaps.md](../../dev-gaps.md) §5  
> **계약**: [dev-contract.md](../../dev-contract.md)  
> **던전**: 뒷산 동굴 맵만

---

## 1. 이 작업물

Prepare에서 **골드로 공장 구역을 확장**한다. 뒷산 시작 구역 → 인접 구역 해금.

**코드**: `ZoneManager` / `GridManager` 확장

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 인접 구역 구매 (최소 UI/키) | 던전별 구역 표 |
| 확장 후 배치 셀 증가 | 월드맵 |
| 구역별 자원 노드 ([03](./03-map-nodes-layout.md)과 연동) | 상점 UI (Dev2) |

---

## 3. 수치 placeholder

| zoneId | 비용 | 비고 |
|--------|------|------|
| `zone_start` | 0 | 시작 |
| `zone_east_1` | 100 | 1차 |
| `zone_north_1` | 150 | (선택) |

확정: Dev2 [01-economy-numbers-lock](../week4-dev2/01-economy-numbers-lock.md)

---

## 4. 구현 단계

- [ ] 잠긴/해금 셀 구분 · 해금 전 배치 거부
- [ ] Prepare 전용 구매 · 골드 차감
- [ ] 해금 `zoneId[]` → `IFactorySave` 포함

---

## 5. 완료 기준

- [ ] 인접 1구역 골드 구매 가능
- [ ] 확장 구역에 기계 배치·채굴 가능
- [ ] 세이브·로드 후 해금 유지

---

## 6. 관련 문서

- [03-map-nodes-layout](./03-map-nodes-layout.md)
- [week4-dev2/01-economy-numbers-lock](../week4-dev2/01-economy-numbers-lock.md)
