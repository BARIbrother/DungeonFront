## Factory 정적 데이터 (SO·Database)

### 이 작업물

기계·아이템·레시피의 **기획 데이터**를 Unity ScriptableObject로 정의하고, id로 조회하는 **Database**를 만든다.  
[03-machine-prefabs](./03-machine-prefabs.md) Prefab을 `MachineDefinition.prefab`에 연결한다.

**스키마**: `02-data-structure.md`  
**코드**: `Assets/Scripts/` (Definition 클래스, Database 클래스)  
**에셋**: `Assets/Data/` 또는 `Assets/ScriptableObjects/` (팀 폴더 규칙 하나)

### ID 규칙 (Week 1 최소)

| 종류 | 예시 |
|------|------|
| itemId | `iron_ore`, `iron` (snake_case 영문) |
| machineDefId | `채굴기_1`, `용광로_1`, `제작기_1`, `창고_1` |
| machineTypeId | `채굴기`, `용광로`, `제작기`, `창고` |
| recipeId | `채굴기_iron_ore`, `용광로_iron`, `제작기_iron_rod` |

### C# 클래스 (필수 필드)

#### `ItemDefinition` : ScriptableObject

- `id`, `displayName`, `icon` (optional)

#### `MachineDefinition` : ScriptableObject

- `id`, `machineTypeId`, `tier`, `displayName`, `prefab`
- `recipeIds` (string[])
- `requiresManualWork` (bool — 제작기·창고 `true`)
- **Week 1 추가 여유**: `gridWidth`, `gridHeight` (점유 칸), 입·출력 포트 방향/오프셋

#### `RecipeDefinition` : ScriptableObject

- `id`, `machineTypeId`, `inputs`, `outputs` (`ItemGroup`)
- `durationTicks`, `manualClickCount` (둘 중 하나만 사용)

#### `ItemGroup` / `ItemGroupEntry`

- `itemId` + `count`

#### Database (각각 ScriptableObject 또는 MonoBehaviour 싱글톤)

- `ItemDatabase.Get(itemId)`
- `MachineDatabase.Get(machineDefId)`
- `RecipeDatabase.Get(recipeId)`

### Week 1 placeholder 에셋 (최소)

| SO | id | 비고 |
|----|-----|------|
| Item | `iron_ore` | 철 광석 |
| Item | `iron` | 철 (용광로 출력용 placeholder) |
| Machine | `채굴기_1` | prefab → 채굴기 Prefab |
| Machine | `용광로_1` | prefab → 용광로 Prefab |
| Machine | `제작기_1` | prefab → 제작기 Prefab, `requiresManualWork=true` |
| Machine | `창고_1` | prefab → 창고 Prefab, `requiresManualWork=true` |
| Recipe | `채굴기_iron_ore` | 입력 없음, 출력 iron_ore, durationTicks placeholder |
| Recipe | `용광로_iron` | 입력 iron_ore, 출력 iron (수치는 placeholder) |
| Recipe | `제작기_iron_rod` | manualClickCount placeholder |

- 레시피 **수치 밸런스**는 Week 1 미완이어도 됨. **필드가 채워져 조회되면** OK.

### MVP 대비 필드 (문서만 반영, 구현 여유)

- `machineTypeId`에 **`벨트`**, **`출력기`** type 추가 가능한 구조 (`00-vision.md`)
- 나중 세이브: `unlockedMachineDefIds` 등은 Dev 세션 쪽

### 완료 기준

- [ ] Definition 클래스 3종 + ItemGroup 컴파일
- [ ] Database 3종에서 `Get("채굴기_1")` 등이 null이 아님
- [ ] Machine SO 4개의 `prefab` 필드가 [03-machine-prefabs](./03-machine-prefabs.md)와 연결됨
- [ ] 에디터에서 SO 에셋을 열어 필드가 보인다
