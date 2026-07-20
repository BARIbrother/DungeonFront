# 구역 확장

**역할**: Lead · **Week**: 4 · **Issue**: 02  
**선행**: 공장 폴리시 · `GridManager`

## 1. 이 작업물

Prepare에서 골드로 공장 구역을 확장한다. 뒷산 시작 구역 → 인접 구역 해금.

**코드**: `ZoneManager` / `GridManager` 확장 · 해금 `zoneId[]` → `IFactorySave`

## 2. 동작

- 잠긴/해금 셀 구분 · 해금 전 배치 거부
- Prepare 전용 구매 · 골드 차감
- 확장 후 배치 셀 증가
- 구역별 자원 노드는 맵·자원 노드 Issue와 맞춤

## 3. 수치 (초안)

| zoneId | 비용 | 비고 |
|--------|------|------|
| `zone_start` | 0 | 시작 |
| `zone_east_1` | 100 | 1차 |
| `zone_north_1` | 150 | (선택) |

확정 비용은 경제 수치 lock과 동일해야 한다. 비용·순서가 바뀌면 `02-zone-expansion_missingReq.md`를 채운다.

## 4. 구현 요약

잠긴/해금 셀 구분 · 해금 전 배치 거부 · Prepare 전용 구매·골드 차감 · 해금 `zoneId[]` → `IFactorySave`.

## 5. 완료 기준

- [ ] 잠긴 셀에 배치 거부 · 해금 셀 구분
- [ ] Prepare에서 인접 1구역 골드 구매 가능
- [ ] 확장 구역에 기계 배치·채굴 가능
- [ ] 해금 `zoneId[]`가 세이브에 포함
- [ ] 세이브·로드 후 해금 유지
