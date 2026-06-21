# 데이터 구조

> `00-vision.md`·`01-core-loop.md`의 게임 규칙을 **코드·에셋에 옮길 때** 따르는 저장 방식.  
> 정적(기획) 데이터, 런타임 상태, 세이브를 구분한다.

## 저장 계층

| 계층 | 용도 | 형식 | 수정 주체 |
|------|------|------|-----------|
| **정적 데이터** | 아이템·기계·레시피·의뢰 정의 | Unity **ScriptableObject** | 기획·에디터 |
| **런타임 상태** | 현재 하루·맵·기계 WIP·틱 | C# 클래스 (메모리) | 게임 로직 |
| **세이브** | 일차·골드·인벤·진행도 영속 | **JSON** (`Application.persistentDataPath`) | 저장/불러오기 |

```
정적 SO ──참조(id)──→ 런타임 상태 ──직렬화──→ 세이브 JSON
```

- 정적 데이터는 **런타임에 생성·변경하지 않는다.** (밸런스 패치는 SO 에셋 교체)
- 런타임 상태는 **세이브에 필요한 필드만** 직렬화한다.
- MVP에서는 **던전 1개**만 가정. 멀티 던전 확장 시 `99-further_implementation.md`의 공유/비공유 규칙을 반영한다.

---

## ID 규칙

### 공통

- 코드·세이브·SO 간 참조는 **문자열 `id`** 로 통일한다.
- 표시용 이름(한글)은 `displayName` 등 **별도 필드**로 둔다. `id`에 한글을 쓰지 않는다.

### 아이템 `itemId`

- **snake_case** 영문 슬러그
- MVP 예:

| itemId | displayName (예) |
|--------|------------------|
| `iron_ore` | 철 광석 |
| `iron` | 철 |
| `iron_rod` | 철 막대 |
| `iron_plate` | 철 판 |

### 기계 type `machineTypeId`

- `01-core-loop.md`의 **type**과 동일한 문자열 (한글 허용 — 기획 문서와 1:1 대응)
- MVP: `채굴기`, `용광로`, `모루`

### 기계 정의 `machineDefId`

- **티어까지 포함**한 정적 정의 ID
- 형식: `{machineTypeId}_{tier}` (tier는 1부터)
- MVP 예: `채굴기_1`, `용광로_1`, `모루_1` (수동 모루 = tier 1)

### 기계 인스턴스 `instanceId`

- 맵에 놓이거나 인벤에 있는 **개별 기계** 구분용
- 런타임 생성 **GUID 문자열** (세이브에 보존)

### 레시피 `recipeId`

- 형식: `{machineTypeId}_{outputItemId}` 또는 출력이 겹치면 suffix 추가
- MVP 예: `채굴기_iron_ore`, `용광로_iron`, `모루_iron_rod`, `모루_iron_plate`

### 의뢰 `questId`

- `01-core-loop.md`와 동일: **8자리 십진수 문자열**
- `DDDQQQQQ` — 앞 3자리 던전, 뒤 5자리 의뢰
- 예: `00100001`

### 던전 `dungeonId`

- 3자리 zero-pad 문자열
- MVP: `001` (뒷산 동굴)

---

## 정적 데이터 (ScriptableObject)

에셋은 `Assets/` 아래 기획용 폴더에 둔다 (코드는 `Assets/Scripts/`).  
각 SO는 **`id` 필드 하나로** 런타임·세이브에서 조회한다.

### ItemDefinition

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | `itemId` |
| `displayName` | string | UI 표시명 |
| `icon` | Sprite | *(선택)* |

### MachineDefinition

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | `machineDefId` |
| `machineTypeId` | string | type (`채굴기` 등) |
| `tier` | int | 티어 (1 = 최저) |
| `displayName` | string | 티어 이름 (예: 수동 모루) |
| `prefab` | GameObject | 배치용 프리팹 참조 |
| `recipeIds` | string[] | 이 기계가 선택 가능한 레시피 |
| `requiresManualWork` | bool | 수작업(클릭) 여부 — 모루 true |

### RecipeDefinition

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | `recipeId` |
| `machineTypeId` | string | 처리 가능 type |
| `inputs` | ItemGroup | 입력 (항목 0~n) |
| `outputs` | ItemGroup | 출력 (항목 1~n) |
| `durationTicks` | int | 자동 공정 소요 (**0**이면 클릭 기반) |
| `manualClickCount` | int | 수작업 필요 클릭 수 (**0**이면 틱 기반) |

- `durationTicks`와 `manualClickCount`는 **둘 중 하나만** 사용 (`01-core-loop.md` 틱 vs 클릭)
- 채굴기 레시피: 입력 없음, 맵 **자원 노드**와 별도로 `ResourceNodeDefinition`과 연결

### ResourceNodeDefinition

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | 노드 종류 ID |
| `itemId` | string | 채취 출력 (`iron_ore` 등) |
| `displayName` | string | |

### QuestDefinition

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | `questId` (8자리) |
| `dungeonId` | string | |
| `displayName` | string | UI용 |
| `isMandatory` | bool | 필수(실패 시 데이 리트라이) |
| `requirements` | ItemGroup | 납품 요구 |
| `rewardsGold` | int | *(MVP 수치 미정)* |
| `rewardsReputation` | int | |
| `rewardItems` | ItemGroup | 아이템 보상 |
| `rewardMachines` | string[] | `machineDefId` — 기계 보상 |
| `deadlineDays` | int | 납기 (1 = 당일 결산) |

### Database SO (조회용)

- `ItemDatabase`, `RecipeDatabase`, `MachineDatabase`, `QuestDatabase` 등
- `id → Definition` 딕셔너리로 **Awake/Init 시 한 번** 인덱싱
- 런타임에서 SO를 직접 순회하지 않고 **Database 경유**로 조회

---

## 공통 값 타입

아이템과 개수 묶음은 **`ItemGroupEntry`** · **`ItemGroup`** 으로 통일한다.

```csharp
[System.Serializable]
public struct ItemGroupEntry
{
    public string itemId;
    public int count;
}

[System.Serializable]
public class ItemGroup
{
    public List<ItemGroupEntry> entries = new();
}
```

- **레시피 입·출력**, **의뢰 요구·아이템 보상**, **WIP 투입 재료** 등 “(아이템, 개수) n종” 표현에 `ItemGroup` 사용
- MVP 레시피·의뢰는 보통 항목 1~4개이나, 구조상 **n개** 허용
- `count`는 항상 **양의 정수**
- 같은 `itemId`가 한 `ItemGroup`에 중복되지 않도록 SO 작성 시 정리 (런타임에서 merge하지 않음)

---

## 런타임 상태

세션마다 메모리에만 존재. 아래는 **MVP 최소 필드**.

### GameSessionState

| 필드 | 타입 | 설명 |
|------|------|------|
| `day` | int | 현재 일차 (1부터) |
| `phase` | enum | `Prepare` / `Production` / `Settlement` |
| `gold` | int | |
| `reputation` | int | 명성 |
| `inventory` | InventoryState | |
| `factory` | FactoryState | 맵·배치 |
| `quests` | QuestRuntimeState | 수락·진행 |
| `unlockedMachineDefIds` | HashSet\<string\> | 해금된 기계 정의 |
| `productionTick` | int | 생산 단계 경과 틱 (0~3000) |

### InventoryState

- `01-core-loop.md`: **용량 무제한**, 모든 창고·인벤 **공유**
- 구현: `Dictionary<string, int>` — `itemId → count` (조회·차감 편의)
- 의뢰 납품·세이브·레시피 비교 시 `Dictionary` ↔ **`ItemGroup`** 변환
- 기계 보관: **별도 목록** `List<MachineInstanceState>` (아이템 dict에 넣지 않음)

### MachineInstanceState

| 필드 | 타입 | 설명 |
|------|------|------|
| `instanceId` | string | GUID |
| `machineDefId` | string | |
| `selectedRecipeId` | string | 현재 레시피 |
| `placement` | MachinePlacement | 아래 enum |
| `gridX`, `gridY` | int | `PlacedOnMap`일 때만 |
| `wip` | MachineWipState | 진행 중 작업 |

```csharp
public enum MachinePlacement
{
    InInventory,
    PlacedOnMap
}
```

### MachineWipState

| 필드 | 타입 | 설명 |
|------|------|------|
| `recipeId` | string | 진행 중 레시피 |
| `consumedInputs` | ItemGroup | 이미 투입된 재료 (환원용) |
| `progressTicks` | int | 자동 공정 진행 |
| `progressClicks` | int | 수작업 클릭 진행 |
| `outputPending` | bool | 완성품 회수 전 (수작업·버퍼) |

**생산 단계 종료 시** (`01-core-loop.md`):

1. `wip.consumedInputs` → 인벤으로 환원
2. `wip` 초기화
3. 완성품(이미 인벤에 들어간 것)은 유지

### FactoryState

| 필드 | 타입 | 설명 |
|------|------|------|
| `placedMachines` | MachineInstanceState[] | `PlacedOnMap`인 것만 (또는 전체에서 필터) |
| `resourceNodes` | ResourceNodePlacement[] | 구역 확장으로 배치된 노드 |
| `unlockedZones` | string[] | 해금 구역 ID |

### QuestRuntimeState

| 필드 | 타입 | 설명 |
|------|------|------|
| `acceptedQuestIds` | List\<AcceptedQuest\> | 하루 최대 3 (`00-vision.md`) |

**AcceptedQuest**

| 필드 | 타입 | 설명 |
|------|------|------|
| `questId` | string | |
| `acceptedDay` | int | 수락 일차 |
| `dueDay` | int | 납기 일차 |
| `submitted` | bool | 납품 완료 여부 |

- 요구 물품 충족 여부는 **결산 시** 인벤 snapshot으로 판정 (별도 진행 카운터 불필요)

---

## 세이브 데이터

- 파일명 예: `save_slot_0.json`
- `GameSessionState`에서 직렬화 가능한 필드만 `[Serializable]` DTO로 복사
- Unity `JsonUtility`는 `Dictionary` 미지원 → 인벤 등은 **`ItemGroup`** (`List<ItemGroupEntry>`)으로 변환

```csharp
[System.Serializable]
public class SaveData
{
    public int version = 1;
    public int day;
    public string phase;           // enum.ToString()
    public int gold;
    public int reputation;
    public ItemGroup inventoryItems;
    public List<MachineInstanceSave> machines;
    public List<AcceptedQuestSave> quests;
    public List<string> unlockedMachineDefIds;
    public FactorySave factory;
    // productionTick: 생산 중 저장 허용 시 포함
}
```

- **저장 시점**: 결산 종료(다음 일차 진입), 준비 단계, 명시적 저장 메뉴 *(구현 시 결정)*
- **로드**: 타이틀·슬롯 선택 시 `SaveData` → `GameSessionState` 복원

---

## 시간·틱

| 상수 | 값 | 저장 |
|------|-----|------|
| `TicksPerSecond` | 10 | 코드 상수 |
| `ProductionPhaseTicks` | 3000 | 코드 상수 |
| `productionTick` | 0~3000 | 런타임·(선택) 세이브 |

- 레시피 `durationTicks`는 **정적 SO**에만 둔다.

---

## MVP 데이터 목록 (기준)

`01-core-loop.md`와 동기화. SO·Database 초기 데이터 작성 시 참고.

### 시작 지급

| machineDefId | 수량 |
|--------------|------|
| `채굴기_1` | 1 |
| `용광로_1` | 1 |
| `모루_1` | 1 |

### 레시피 (요약)

| recipeId | machineTypeId | 입력 | 출력 | durationTicks | manualClickCount |
|----------|---------------|------|------|---------------|------------------|
| `채굴기_iron_ore` | `채굴기` | — | iron_ore ×1 | 30 | 0 |
| `용광로_iron` | `용광로` | iron_ore ×1 | iron ×1 | 15 | 0 |
| `모루_iron_rod` | `모루` | iron ×1 | iron_rod ×1 | 0 | 5 |
| `모루_iron_plate` | `모루` | iron ×1 | iron_plate ×1 | 0 | 5 |

### 의뢰

| questId | isMandatory | requirements (요약) |
|---------|-------------|---------------------|
| `00100001` | true | iron_ore×5, iron×5, iron_plate×1, iron_rod×1 |

---

## 코드 배치 (예정)

| 역할 | 위치 (예) |
|------|-----------|
| SO 정의 클래스 | `Assets/Scripts/Data/` |
| Database·조회 | `Assets/Scripts/Data/` |
| 런타임 상태 | `Assets/Scripts/Runtime/` |
| 세이브 DTO·IO | `Assets/Scripts/Save/` |

- 새 데이터 타입 추가 시: **SO 필드 → Database id 등록 → MVP 표 갱신** 순서

---

## 추후 확장 (MVP 밖)

`99-further_implementation.md` 반영 시 변경 예정:

| 항목 | MVP | 확장 |
|------|-----|------|
| 던전 | `001` 고정 | `dungeonId`별 FactoryState·Quest 분리 |
| 인벤·일차 | 단일 세션 | **전 던전 공유** (세이브 최상위) |
| 명성 | 단일 | 던전별 + (던전 4 예외) |
| 의뢰 | 단일 목록 | **던전별** 수락 목록 |

확장 시에도 **`id` 규칙·SO/DTO/JSON 3계층**은 유지한다.

---

## 관련 문서

- `00-vision.md` — 인벤·의뢰·경제 규칙
- `01-core-loop.md` — MVP 수치·레시피·의뢰 ID
- `99-further_implementation.md` — 멀티 던전 공유 규칙
