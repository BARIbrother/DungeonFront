## Dev·Art 팀 연동

### 이 작업물

Lead 산출물(Factory 씬·SO·생산 UI)과 **Dev**(세션·결산·씬 전환), **Art**(주인공 모션)를 붙여 **금요일 데모** 1회전을 만든다.

### Lead가 제공할 것

| 대상 | 전달물 |
|------|--------|
| **Dev** | `Factory` 씬 (Build Settings 등록됨), 맵 중앙 **스폰 월드 좌표**, Prefab 4종(시작 지급) 경로 |
| **Art** | `Player`의 SpriteRenderer/Animator 슬롯, 픽셀당 **1칸 높이** (칸 크기) |

### Lead가 수령·연결할 것

| 대상 | 수령물 | Lead 작업 |
|------|--------|-----------|
| **Dev** | `NewGame` 이벤트 / API | [02-player-movement](./02-player-movement.md) 스폰 연결 |
| **Dev** | `GamePhase` 변경 | [05-factory-production-ui](./05-factory-production-ui.md) 버튼·타이머 연동 |
| **Art** | idle/walk 스프라이트 시트 | Player Animator 또는 Sprite 교체 |

### 데모 체크리스트 (순서대로 실행)

1. [ ] Unity Play → **Factory** 로드
2. [ ] (Dev) **새 게임** → 플레이어 맵 **중앙**, 인벤 기계 4종 (HUD 확인)
3. [ ] **WASD** 이동
4. [ ] **생산 시작** → 타이머 **5:00** 표시
5. [ ] (디버그 또는 타이머 만료) **결산** 화면
6. [ ] **다음 날** → 일차 2, **Prepare**, Factory 복귀
7. [ ] (Art) 주인공 **walk** 모션 적용 시 이동 중 애니 재생
8. [ ] 기계 **10종+ v0.1** 표 팀 공유 — [03-machine-plan.md](../../planning/03-machine-plan.md)

### 완료 기준

- [ ] 위 1~6이 한 Play 세션에서 동작 (7·8은 가능 시)
- [ ] `MachineDefinition.prefab` 4종(시작 지급) 연결 확인 ([04-factory-data](./04-factory-data.md))
