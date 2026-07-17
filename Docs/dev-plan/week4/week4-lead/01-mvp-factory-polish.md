# 공장 폴리시

> **역할**: Lead · **Week**: 4 · **Issue**: 01  
> **선행**: W3 [07-mvp-integration](../../week3/week3-lead/07-mvp-integration.md)  
> **기획**: [01-core-loop.md](../../../01-core-loop.md) · [dev-gaps.md](../../dev-gaps.md) §1  
> **계약**: [dev-contract.md](../../dev-contract.md)  
> **비범위**: 다른 던전·복수 맵

---

## 1. 이 작업물

뒷산 동굴 Factory의 **물류·생산·고장** 버그와 갭을 막아, 튜토~엔딩 완주에 지장 없게 한다.

**코드**: `Assets/Scripts/Machine/`, `Assets/Scripts/Placement/`, Factory 씬

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 벨트·출력기·수동 회수 우선순위·버그 | 수동 운반 전면 재설계 (선택만) |
| 입·출력 포트·WIP 회귀 | 던전 복수화 |
| 고장 tint·수리 클릭 수 조정 | 기계별 정밀 밸런스 전량 |
| W3 이슈 triage | 신규 기계 type |

---

## 3. 폴리시 체크리스트

### 3-1. 물류

- [ ] 벨트 → 출력기 → 수동 회수 **우선순위** 문서·코드 일치
- [ ] 채굴기 산출 → 출력 포트/버퍼 경로
- [ ] 용광로·제작기 입력 흡수 회귀
- [ ] 회수 시 포트·WIP → 인벤 ([03-manual-interaction](../../week3/week3-lead/03-manual-interaction.md) 회귀)

### 3-2. 고장·수작업

- [ ] 고장 빈도 placeholder를 플레이 가능 수준으로
- [ ] 수작업 제작기·마나 제작기 E키 회귀

### 3-3. 안정성

- [ ] 10종+ 배치·회수·세이브 복원
- [ ] Console Error 0 (문서화된 경고만)

---

## 4. 완료 기준

- [ ] §3 통과 또는 티켓화
- [ ] 튜토 의뢰 물품 1회 생산·납품 가능 (Dev2 SO 연동 전 Mock OK)
- [ ] 신규 크래시 0

---

## 5. 관련 문서

- [02-zone-expansion](./02-zone-expansion.md)
- [05-team-integration](./05-team-integration.md)
