## Dev1·Dev2·Art 연동 (Week 2)

> 통합 상세: [../team-integration.md](../team-integration.md)

### Lead 데모 체크리스트 (Lead 담당)

1. [ ] NewGame → Factory, 인벤 4종
2. [ ] **배치** — 채굴기 노드 위, 용광로·제작기
3. [ ] **생산 시작** → 5:00, 채굴기 틱
4. [ ] 철 체인 3종 (수동 투입 허용)
5. [ ] 6종+ 중 1종 추가 배치
6. [ ] 고장 → 수리

### Lead 제공

| 대상 | 전달물 |
|------|--------|
| Dev1 | 배치 후 `PlayerInventory` 갱신, `GetPlacedMachines()`, `PlacementController` API |
| Dev2 | (W2 무관) |
| Art | 수리·작업 Animator 트리거명 |

### Lead 소비 (Dev1 계약)

| 계약 | 용도 |
|------|------|
| `GameSessionState.OnPhaseChanged` | Prepare에서만 배치 허용 |
| `GameSessionState.StartProduction()` | Production 틱 시작 (W3 Dev1 연동) |

### 완료 기준

- [ ] 위 1~6 Lead 구간 동작
- [ ] [team-integration.md](../team-integration.md) 전체 데모 통과
