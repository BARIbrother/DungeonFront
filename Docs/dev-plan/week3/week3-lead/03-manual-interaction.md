# 수작업·수동 운반·회수

> **역할**: Lead · **Week**: 3 · **Issue**: 03  
> **선행**: W2 WIP·배치 · `Machine.cs` · `PlacementController.cs`  
> **Art 연동**: [week3-art/04-protagonist-motions-polish](../week3-art/04-protagonist-motions-polish.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) — PlayerInventory

---

## 1. 이 작업물

Factorio형 **수작업 클릭 가공**, **플레이어 수동 운반**(들기/놓기), **출력 버퍼→인벤 즉시 회수**를 구현한다.  
튜토 2단계: 채굴기–용광로 **수동 운반** ([01-core-loop.md](../../../01-core-loop.md)).

**코드**: `Assets/Scripts/Machine/`, `Assets/Scripts/Player/PlayerMovement.cs`, `Assets/Scripts/Placement/PlacementController.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| `requiresManualWork` + `manualClickCount` | 자동 벨트 완전 대체 (벨트는 별도) |
| 버퍼·벨트 끝 아이템 들기/놓기 | 인벤 UI 드래그앤드롭 (선택) |
| 출력 버퍼 → 인벤 클릭 회수 | Prepare 외 배치/회수 (기존 Placement) |
| 회수 시 인벤 반환 | `GameFlowController` (레거시) |

---

## 3. 현재 코드 상태

| 구현됨 | 미구현/갭 |
|--------|-----------|
| `Machine` inputPort/outputPort, WIP 틱 | `requiresManualWork` 필드·클릭 카운트 |
| `PlacementController` 배치·회수, `ReturnAllContentsToPlayerInventory` | 플레이어 **손에 든 아이템** 상태 |
| `ConveyerBelt` heldItem | 벨트 끝에서 플레이어 pick/drop |
| `PlayerMovement` Animator (이동) | repair/work 트리거 연동 |

### 생산 중단 vs 회수 (동작 정의)

| 동작 | WIP·레시피 | 포트 아이템 |
|------|------------|-------------|
| T키/결산 진입 | `currentRecipe`, `progressTicks`, `hasActiveWip` **유지** | 포트 유지 |
| Prepare **회수** | WIP 입력 환원 + 포트 → 인벤 | `ReturnAllContentsToPlayerInventory()` |

---

## 4. 데이터 스펙

### 수작업 레시피 ([02-data-structure.md](../../../02-data-structure.md))

| recipeId | machineTypeId | manualClickCount | durationTicks |
|----------|---------------|------------------|---------------|
| `제작기_iron_rod` | `제작기` | **5** | 0 |
| `제작기_iron_plate` | `제작기` | **5** | 0 |

- `manualClickCount > 0` → 틱 대신 클릭 진행
- 클릭 완료 시 **출력 버퍼**에 산출 (틱 완료와 동일 경로)

### 수작업 기계 type

`03-machine-plan`: `제작기`, `마나 제작기`, `창고` — `requiresManualWork = O`

---

## 5. 기능 상세

### 5-1. 수작업 클릭

| 단계 | 동작 |
|------|------|
| 전제 | Production 페이즈, 플레이어가 기계 **근접** (1칸 이내 등) |
| 입력 | 기계 **클릭** (UI 위 클릭은 EventSystem으로 무시) |
| 진행 | `manualClickCount` 1 감소 또는 누적 +1 → 목표 도달 시 출력 |
| 입력 소비 | 첫 클릭 또는 완료 시 `inputPort`에서 레시피 입력 차감 (기획: 완료 시 1회) |
| 연출 | Animator `Work` 트리거 — Art 시트 |

### 5-2. 수동 운반

| 동작 | 설명 |
|------|------|
| **들기** | 출력 버퍼·벨트 끝에 아이템 있을 때 상호작용 → `PlayerCarry` 상태에 `ItemEntry` 1스택 |
| **놓기** | 인접 기계 **입력 포트** 또는 빈 벨트 칸에 `PutintoInputPort` |
| 제한 | 손에 **1종류·1스택**만 (MVP) |

**신규 상태** (예시):

```csharp
// PlayerInventory 또는 PlayerCarry.cs
public bool HasCarriedItem { get; }
public ItemEntry CarriedItem { get; }
public bool TryPickUp(ItemEntry from);
public bool TryDropTo(Machine target);
```

### 5-3. 출력 버퍼 → 인벤

| 단계 | 동작 |
|------|------|
| 전제 | Prepare 또는 Production, 출력 포트에 아이템 |
| 입력 | 출력 포트/기계 **상호작용** (우클릭과 구분 — 키 또는 좌클릭) |
| 결과 | `PlayerInventory.Add` + 포트에서 제거 |

튜토: 창고 대신 **인벤 직접 회수**로 동일 효과.

---

## 6. 구현 단계

- [ ] `ItemDef_Machine` 또는 `Machine`에 `requiresManualWork` bool
- [ ] `Recipe`에 `manualClickCount` (또는 기존 필드명 확인)
- [ ] `AssemblerMachine`(제작기) — 클릭 핸들러 + 진행 카운터
- [ ] `PlayerCarry` — pick/drop API
- [ ] `ConveyerBelt` 끝 칸 pick/drop 연동
- [ ] 출력 → 인벤 상호작용
- [ ] `PlayerMovement` — 근접 판정 + `Work`/`Repair` 트리거
- [ ] 고장 수리 클릭 시 `Repair` 트리거 (W2 malfunction 연동)

---

## 7. 검증 시나리오

- [ ] **제작기**: 레시피 철 막대 선택 → 클릭 5회 → outputPort에 iron_rod
- [ ] **수동 운반**: 채굴기 output → 들기 → 용광로 input 놓기 → 용광로 가동
- [ ] **인벤 회수**: 제작기 output 클릭 → 인벤 count 증가
- [ ] **회수**: 배치된 기계 회수 시 포트·WIP 인벤 반환 (기존 `ReturnAllContentsToPlayerInventory`)
- [ ] 튜토 순서 2번 ([01-core-loop.md](../../../01-core-loop.md)) 재현 가능

---

## 8. Art 연동

| 모션 | 트리거 | Art 산출물 |
|------|--------|------------|
| work | 수작업 클릭 | `protagonist_work.png` |
| repair | 고장 수리 | `protagonist_repair.png` |

파라미터: `PlayerMovement` — `IsMoving`, `MoveX`, `MoveY` + `Work`/`Repair` trigger

---

## 9. 완료 기준

- [ ] §7 검증 시나리오 전부 통과
- [ ] Production 중 튜토 일시정지 시 클릭·운반도 정지 (Dev1 연동)
- [ ] Lead 단독 테스트 씬에서 벨트 없이 철 체인 완성 가능

---

## 10. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md) — 튜토·수동 운반
- [week3-art/04-protagonist-motions-polish](../week3-art/04-protagonist-motions-polish.md)
