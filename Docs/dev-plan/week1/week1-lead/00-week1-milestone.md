## Week 1 마일스톤 — Lead

### 이 주에 만드는 것

**공장 안(Factory)** 전부: 탑다운 맵, 플레이어 이동, 기계·노드 Prefab, 정적 데이터(SO), 생산 UI, 기획 표 2종.  
Dev는 세션·결산·씬 전환을 담당하고, Lead는 **Factory 씬**과 그 안에서 보이는 것·데이터를 담당한다.

### 산출물 목록

| # | 작업물 | Issue |
|---|--------|-------|
| 1 | 그리드 맵 + `Factory` 씬 + 철광석 노드 placeholder | [01-grid-map](./01-grid-map.md) |
| 2 | WASD 이동 + `NewGame` 스폰 | [02-player-movement](./02-player-movement.md) |
| 3 | 기계 4종(시작 지급) + 철광석 노드 Prefab | [03-machine-prefabs](./03-machine-prefabs.md) |
| 4 | Item/Machine/Recipe SO + Database + Prefab 연결 | [04-factory-data](./04-factory-data.md) |
| 5 | 생산 시작 버튼 + 5분 타이머 UI | [05-factory-production-ui](./05-factory-production-ui.md) |
| 6 | MVP 기계 10종+ 목록 v0.1 | [03-machine-plan](../../planning/03-machine-plan.md) · Issue [06-planning-machine-list](./06-planning-machine-list.md) |
| 7 | 튜토 의뢰 수치 + 1일차 스토리 초안 | [07-planning-quest-story](./07-planning-quest-story.md) |
| 8 | Dev·Art 연동 데모 | [08-team-integration](./08-team-integration.md) |

### 금요일 데모 시나리오

1. Play → **Factory** 씬, 탑다운 맵·철광석 노드 보임  
2. **WASD**로 맵 중앙에서 이동  
3. **생산 시작** 클릭 → 타이머 5:00 카운트다운  
4. (Dev 연동) 타이머 종료 또는 디버그 → **결산** 화면 → **다음 날** → 다시 Factory  
5. 기계 **10종+ v0.1** 표 팀 공유  

### 완료 기준

- [ ] 위 1~7 산출물 각 Issue의 완료 기준 충족
- [ ] 데모 시나리오 1~4 재현 가능 (Dev·Art 연동 포함)
