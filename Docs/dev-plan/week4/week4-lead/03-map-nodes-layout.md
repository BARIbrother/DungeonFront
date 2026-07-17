# 맵·자원 노드

> **역할**: Lead · **Week**: 4 · **Issue**: 03  
> **선행**: [02-zone-expansion](./02-zone-expansion.md) · `MinerMachine`  
> **기획**: [dev-gaps.md](../../dev-gaps.md) §2 · [01-core-loop.md](../../../01-core-loop.md)  
> **Art**: [02-backcave-tiles](../week4-art/02-backcave-tiles.md)  
> **던전**: 뒷산 동굴만

---

## 1. 이 작업물

뒷산 Factory 맵의 **시작 레이아웃·철광석(및 MVP 자원) 노드 위치·개수**를 확정하고 배치한다.  
튜토 첫날과 클리어까지 **채굴 병목**이 나지 않게 한다.

**코드/씬**: Factory 씬 · 자원 노드 Prefab · (선택) `ResourceNode` 컴포넌트

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 1일차 시작 구역 철광석 노드 위치·개수 | 혹한·다른 던전 노드 |
| 확장 구역 추가 노드 (종류 placeholder OK) | 무한 맵 생성 |
| Art 타일 적용 슬롯 | Art 제작 |

---

## 3. 배치 초안 (팀 확정)

| 구역 | 노드 | 개수 (초안) | 비고 |
|------|------|-------------|------|
| `zone_start` | 철광석 | 2~3 | 튜토·00100001 |
| `zone_east_1` | 철광석 또는 상위 | 1+ | 확장 보상감 |
| (선택) | 마나 원석 등 | 스토리 해금 후 | 마법 라인 |

---

## 4. 구현 단계

- [ ] 노드 Prefab · 채굴기 footprint와 정렬
- [ ] 시작 맵에 노드 배치 · 플레이 테스트
- [ ] 확장 구역 해금 시 노드 활성/스폰
- [ ] Docs에 최종 좌표·개수 기록 (또는 씬이 정본)

---

## 5. 완료 기준

- [ ] NewGame 직후 철광석 채굴 가능 (치트 없이)
- [ ] 튜토 의뢰량에 대해 5분×수일 내 생산 가능 (밸런스 대략)
- [ ] Art 타일과 충돌·구멍 없음

---

## 6. 관련 문서

- [01-mvp-factory-polish](./01-mvp-factory-polish.md)
- [week4-art/02-backcave-tiles](../week4-art/02-backcave-tiles.md)
