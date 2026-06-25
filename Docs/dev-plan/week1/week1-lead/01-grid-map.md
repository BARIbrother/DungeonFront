## 그리드 맵·타일 배치

### 이 작업물

던전 앞 **대장간 공장**의 탑다운 맵 뼈대. 준비·생산 단계 동안 플레이어가 돌아다니는 **Factory** 씬을 만든다.  
이후 기계 배치·세이브의 `gridX/gridY`와 같은 좌표 체계를 쓴다.

**코드 위치**: `Assets/Scripts/` (예: `GridMap`, `GridCoordinates` 등)  
**씬 이름**: `Factory` (또는 `Game` — 팀 내 하나로 통일)

### 맵 규격

| 항목 | 값 |
|------|-----|
| 던전 | 뒷산 동굴 앞 — **시작 구역 1개**만 (확장은 후속) |
| 뷰 | **탑다운** orthographic (`00-vision.md`) |
| 플레이어 스폰 | 맵 **기하학적 중앙** (Dev `NewGame`과 동일 좌표 합의) |
| 자원 노드 | **철광석** placeholder **1~2칸** (정확 위치·개수는 미정 가능) |
| 그리드 | 1 Unity unit = 1칸 권장. 맵 크기 예: **16×16** (팀에서 확정) |

### 구현할 것

#### 1. 좌표 체계

- [ ] 정수 그리드 `(gridX, gridY)` ↔ 월드 `Vector2/Vector3` **양방향** 변환
- [ ] `WorldToGrid(worldPos)` → `(x, y)`
- [ ] `GridToWorld(x, y)` → 타일 **중심** 또는 코너 (팀 규칙 하나로 고정)
- [ ] 나중 세이브 `MachineInstanceState.gridX/gridY`와 **동일 규칙** (`02-data-structure.md`)

#### 2. 바닥·타일

- [ ] Unity **Tilemap** 또는 타일 배열
- [ ] placeholder(단색 Sprite·Tile)로 충분
- [ ] 맵 경계 밖은 걸을 수 없게 (플레이어 이동 Issue와 연동)

#### 3. 씬·카메라

- [ ] `Factory` 씬 생성·Build Settings 등록
- [ ] **Orthographic** 카메라, 탑다운 (회전 고정)
- [ ] **Sorting Order**: 바닥(0) → 오브젝트·노드(1) → 플레이어(2)

#### 4. 철광석 노드 placeholder

- [ ] 맵 위 1~2곳에 **시각만** 있는 노드 오브젝트 (채굴 로직 없음)
- [ ] 그리드 칸에 맞게 정렬

### 완료 기준

- [ ] Play Mode에서 탑다운 맵이 보인다
- [ ] 코드로 `GridToWorld(3, 5)` 같은 호출이 예측 가능한 위치를 반환한다
- [ ] 철광석 노드 placeholder가 맵에 1곳 이상 있다
- [ ] Dev가 페이즈 매니저를 붙일 **빈 GameObject** 자리를 씬에 둘 수 있다 (이름 예: `GameSystems`)
