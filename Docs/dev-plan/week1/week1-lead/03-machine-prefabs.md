## 기계·자원 노드 Prefab (placeholder)

### 이 작업물

시작 시 인벤에 들어가는 **기계 3종**과 맵에 놓을 **철광석 노드**의 **시각 Prefab**.  
Week 1은 맵에 **수동 배치**로 모양만 확인한다. 배치 모드·채굴 로직은 없음.

**SO 연결**은 [04-factory-data](./04-factory-data.md)에서 `MachineDefinition.prefab`으로 한다.

### 새 게임 시 기계 위치

| machineDefId | type | Week 1 위치 |
|--------------|------|-------------|
| `채굴기_1` | 채굴기 | **인벤토리만** (맵에 없음) |
| `용광로_1` | 용광로 | **인벤토리만** |
| `모루_1` | 수동 모루 | **인벤토리만** |

→ Prefab은 **에셋으로 존재**하고, 런타임 인스턴스는 Dev `NewGame`이 인벤에만 만든다.

### 만들 Prefab 4종

#### 기계 3종

- [ ] `채굴기_1` — placeholder Sprite + TextMesh/라벨 `채굴기`
- [ ] `용광로_1` — placeholder + `용광로`
- [ ] `모루_1` — placeholder + `수동 모루`
- [ ] 각 Prefab 루트에 **점유 크기** 메타 보관 (예: `Vector2Int size = (1,1)` 컴포넌트) — 종류별로 나중에 `(2,1)` 등 변경 가능
- [ ] **입력 포트**·**출력 포트** 자식 오브젝트 placeholder (작은 사각형 + 화살표). **방향은 배치 시 자유** (`00-vision.md`)

#### 자원 노드 1종

- [ ] `철광석_노드` (이름 예시) — 회색/갈색 placeholder
- [ ] 출력 자원: `iron_ore` (철 광석) — 로직 없이 라벨만 달아도 됨

### 폴더·이름

```
Assets/Prefabs/Machines/채굴기_tier1.prefab
Assets/Prefabs/Machines/용광로_tier1.prefab
Assets/Prefabs/Machines/모루_tier1.prefab
Assets/Prefabs/ResourceNodes/철광석_노드.prefab
```

- `machineDefId` = `채굴기_1` 형식과 티어 번호 규칙을 [06-planning-machine-list](./06-planning-machine-list.md)와 맞출 것

### 씬 확인

- [ ] `Factory` 씬에 Prefab·노드를 **임시로** 올려 그리드에 맞게 보이는지 확인 후 제거해도 됨

### 완료 기준

- [ ] Prefab 에셋 4개가 프로젝트에 존재한다
- [ ] 각 기계 Prefab에 점유 크기·포트 placeholder가 있다
- [ ] Factory 씬에 드래그 배치 시 그리드 칸에 맞게 보인다
