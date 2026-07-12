# 기계 10종+ Prefab·SO

> **역할**: Lead · **Week**: 3 · **Issue**: 01  
> **선행**: W2 [04-machines-6plus](../../week2/week2-lead/04-machines-6plus.md) · [06-outputter](../../week2/week2-lead/06-outputter.md)  
> **기획 정본**: [03-machine-plan.md](../../../03-machine-plan.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) — Contracts Machines

---

## 1. 이 작업물

MVP **10종 이상** `machineTypeId`에 대해 **tier 1 Prefab + ItemDef_Machine SO**를 완성하고, 배치·가동이 가능하게 한다.  
벨트·(출력기) 포함 여부는 기획 표에 맞춘다 — **현재 코드에 `ConveyerBelt` 존재**, 10종에 벨트 포함 권장.

**코드**: `Assets/Scripts/Machine/`, `Assets/Scripts/Item/ItemDef_Machine.cs`  
**에셋**: `Assets/Prefabs/Machines/`, `Assets/ItemDefinition/MachineDef/`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 03-machine-plan 표 10행 Prefab·SO | tier 2+ |
| `MachineDatabase` (또는 ItemManager) id 조회 | Dev2 상점 UI |
| 배치·틱·레시피 연결 가능 상태 | 레시피 수치 lock → [05-recipe-data-lock](./05-recipe-data-lock.md) |

---

## 3. 기계 목록 (tier 1 — 확정 표)

[03-machine-plan.md](../../../03-machine-plan.md)에서 복사. **각 행 = Prefab 1개 + SO 1개**.

| # | machineTypeId | machineDefId | 표시명 | 수작업 | 해금 (초안) | 스크립트 |
|---|---------------|--------------|--------|--------|-------------|----------|
| 1 | `채굴기` | `채굴기_1` | 채굴기 | X | 시작 | `MinerMachine` |
| 2 | `용광로` | `용광로_1` | 용광로 | X | 시작 | `SmelterMachine` |
| 3 | `제작기` | `제작기_1` | 수동 제작대 | **O** | 시작 | `AssemblerMachine` + 수작업 |
| 4 | `마나 추출기` | `마나 추출기_1` | 마나 추출기 | X | 2번째 스토리 의뢰 | 신규 또는 Placeholder |
| 5 | `마법 부여대` | `마법 부여대_1` | 마법 부여대 | X | 2번째 스토리 의뢰 | 신규 |
| 6 | `마나 제작기` | `마나 제작기_1` | 수동 마나 제작대 | **O** | 2번째 스토리 의뢰 | Assembler 패턴 |
| 7 | `제단` | `제단_1` | 제단 | X | 명성 350 | 신규 |
| 8 | `주조소` | `주조소_1` | 주조소 | X | 명성 500 | 신규 |
| 9 | `창고` | `창고_1` | 창고 | O | 시작 | 저장 로직 |
| 10 | `마나 저장소` | `마나 저장소_1` | 마나 저장소 | X | 2번째 스토리 의뢰 | 신규 |
| + | `컨베이어` | `컨베이어_1` | 컨베이어 벨트 | X | 시작/해금 | `ConveyerBelt` (기존) |

**W2 완료분**: 채굴기, 용광로, 제작기(Assembler), 창고, 벨트 — **나머지 6종+ 신규**.

---

## 4. 산출물

| 산출물 | 경로 예 |
|--------|---------|
| Machine Prefab | `Assets/Prefabs/Machines/{Type}_machine.prefab` |
| ItemDef_Machine SO | `Assets/ItemDefinition/MachineDef/{machineDefId}.asset` |
| Database | `MachineDatabase.asset` 또는 `ItemManager` 등록 — **MVP id 전부 non-null** |
| Contracts (시작 4종) | `Assets/Data/Contracts/Machines/` — Dev1 NewGame용 |

### SO 필수 필드 (`ItemDef_Machine`)

| 필드 | 예 |
|------|-----|
| `id` | `채굴기_1` |
| `machineTypeId` | `채굴기` |
| `displayName` | 채굴기 |
| `prefab` | 해당 Prefab 참조 |
| `requiresManualWork` | 제작기·마나 제작기·창고 = true |

---

## 5. Prefab 구성 체크리스트 (기계당)

- [ ] `Machine` 서브클래스 컴포넌트
- [ ] `inputPort` / `outputPort` (또는 서브클래스에서 초기화)
- [ ] `RecipePool` — [05-recipe-data-lock](./05-recipe-data-lock.md)에서 레시피 연결
- [ ] `size` / `GetFootprintSize()` — GridManager 배치
- [ ] SpriteRenderer + (선택) Animator
- [ ] `MachineRecipeUI` — 우클릭 레시피 (기존 패턴)

---

## 6. 구현 단계

### 6-1. 표 대조

- [ ] [03-machine-plan.md](../../../03-machine-plan.md) 10행 + 벨트 포함 여부 팀 확인
- [ ] 미구현 type 목록 작성

### 6-2. 신규 기계

- [ ] W2 `Machine_placeholder` 또는 기존 Prefab **복제** 후 이름·스크립트 교체
- [ ] 마법 라인 4종 + 제단 + 주조소 — 최소 **틱 생산 스텁** (`IFactoryProduction`)으로 시작 가능
- [ ] 수작업 기계: `requiresManualWork=true`, `manualClickCount`는 Recipe SO에서

### 6-3. Database

- [ ] `machineDefId` → Prefab/SO 조회 API
- [ ] `PlacementController` / `PlayerInventory.AddMachine`이 id로 스폰 가능

### 6-4. 검증

- [ ] NewGame 인벤 4종 배치 가능
- [ ] 해금된 기계 10종+ **각 1대** 배치 성공 (치트키 또는 Dev2 해금 후)
- [ ] null 참조 Prefab 0건

---

## 7. 독립 개발

Dev2·Dev1 없이:

```csharp
// PlayerMovement 디버그 키 또는 인스펙터에 SO 할당
playerInventory.AddMachine(machineDef);
PlacementController → B키 배치
```

---

## 8. 완료 기준

- [ ] `machineTypeId` **10종 이상** tier 1 Prefab 존재
- [ ] `MachineDatabase`(또는 동등)에서 MVP `machineDefId` **전부 조회 가능**
- [ ] Prepare 단계에서 각 type **1대 이상** 배치·회수 가능
- [ ] W2 기계(채굴·용광로·제작·벨트) 회귀 없음

---

## 9. 관련 문서

- [03-machine-plan.md](../../../03-machine-plan.md)
- [05-recipe-data-lock](./05-recipe-data-lock.md)
- [07-mvp-integration](./07-mvp-integration.md)
