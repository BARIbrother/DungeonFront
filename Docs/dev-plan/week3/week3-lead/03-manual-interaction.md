# 수작업·회수

> **역할**: Lead · **Week**: 3 · **Issue**: 03  
> **선행**: W2 WIP·배치 · `Machine.cs` · `PlacementController.cs`  
> **Art 연동**: [week3-art/04-protagonist-motions-polish](../week3-art/04-protagonist-motions-polish.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) — PlayerInventory  
> **비범위**: 수동 운반(들기/놓기) — 이번 이슈에서 **제외**

---

## 1. 이 작업물

Factorio형 **수작업 가공**, **기계 회수 시 포트·WIP → 인벤 반환**, (선택) **출력 버퍼→인벤 즉시 회수**를 구현한다.

**코드**: `Assets/Scripts/Machine/`, `Assets/Scripts/Player/PlayerMovement.cs`, `Assets/Scripts/Placement/PlacementController.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 수작업 기계 (`HandmadeMachine`) + E키 진도 | **수동 운반** (들기/놓기, `PlayerCarry`) |
| `recipeTime` + `workSpeed` 통일 진행 | 벨트 끝 pick/drop |
| 회수 시 input/output/WIP → 인벤 | 인벤 UI 드래그앤드롭 |
| E키 근접 수리 + 고장 붉은 틴트 | `GameFlowController` (레거시) |
| (선택) 출력 → 인벤 즉시 회수 | |

---

## 3. 현재 코드 상태

| 구현됨 | 미구현/갭 |
|--------|-----------|
| `recipeTime` + `workSpeed` 통일 진행도 | 출력 버퍼 → 인벤 즉시 회수 (선택) |
| `HandmadeMachine` + E키 근접 수작업 | Animator `Work`/`Repair` 파라미터·클립 (Art) |
| 회수: `ReturnAllContentsToPlayerInventory` (포트 + WIP 환원) | |
| E키 근접 수리 (고장 기계 우선) + 붉은 틴트 | |
| `PlayerMovement` `Work`/`Repair` 트리거(파라미터 있을 때만) | |

### 생산 중 재료(WIP) 모델

1. **시작** (`StartProductionTick`): `inputPort`에서 레시피 입력 **즉시 소비** → `hasActiveWip = true`, `progressTicks = 0`
2. **진행**: 틱 또는 E키마다 `progress += workSpeed` (재료 아이템은 포트에 없음)
3. **완료**: `outputPort`에 산출, WIP 플래그 해제
4. **회수 중 WIP**: 소비됐던 입력 수량을 `currentRecipe.inputEntryList` 기준으로 **인벤 복구**

### 생산 중단 vs 회수

| 동작 | WIP·레시피 | 포트 아이템 |
|------|------------|-------------|
| T키/결산 진입 | `currentRecipe`, `progressTicks`, `hasActiveWip` **유지** | 포트 유지 |
| Prepare **회수** | WIP 입력 환원 + 포트 → 인벤 | `ReturnAllContentsToPlayerInventory()` |

---

## 4. 데이터 스펙

- 수작업: `HandmadeMachine` + `SupportsManualWorkClick`
- 진행: `Recipe.recipeTime`, `Machine.workSpeed` (E키마다 `workSpeed` 누적)

---

## 5. 기능 상세

### 5-1. 수작업·수리 (E키 공통)

| 단계 | 동작 |
|------|------|
| 전제 | 기계 **근접** (1칸 이내) |
| 좌클릭 | 레시피 선택 UI |
| E키 우선순위 | ① 고장난 기계 수리 → ② 수작업 진도 |
| 수작업 진도 | `progress += workSpeed` → `recipeTime` 이상이면 출력 |
| 입력 소비 | WIP **시작 시** `inputPort`에서 차감 |
| 고장 비주얼 | `SetBroken(true)` 시 SpriteRenderer **붉은 틴트** |
| 연출 | Animator `Work` / `Repair` (파라미터 있으면) |

### 5-2. 기계 회수

Prepare 배치 모드에서 회수 시:

1. WIP 중이면 소비 입력 복구
2. `inputPort` · `outputPort` 전부 인벤 지급 후 비움
3. 기계 인스턴스 인벤 복귀

### 5-3. 출력 버퍼 → 인벤 (선택·미구현)

출력 포트만 클릭/키로 즉시 인벤 회수. 튜토·편의용.

---

## 6. 구현 단계

- [x] 수작업 기계 식별 → `HandmadeMachine` / `SupportsManualWorkClick`
- [x] `recipeTime` + `workSpeed`
- [x] 수작업 진도 (좌클릭=레시피 UI, **E키**=진도)
- [x] 회수 시 포트·WIP → 인벤 (`ReturnAllContentsToPlayerInventory`)
- [ ] 출력 → 인벤 즉시 회수 (선택)
- [x] `PlayerMovement` 근접 + `Work` 트리거
- [x] 고장 수리: E키 근접 + 붉은 틴트 + `Repair` 트리거(파라미터 있으면)

---

## 7. 검증 시나리오

- [x] **수작업**: 레시피 선택 → E키로 진행 → outputPort 산출
- [x] **회수**: input/output + WIP 중 소비 재료 인벤 반환
- [x] **수리**: 고장 시 붉은 틴트 → 근접 E키로 수리·틴트 해제
- [ ] **인벤 즉시 회수**: 출력만 클릭/키로 인벤 (선택)
- [ ] Animator `Work`/`Repair` 클립 연동 (Art)

---

## 8. Art 연동

| 모션 | 트리거 | 상태 |
|------|--------|------|
| work | 수작업 E키 | 코드 준비, Animator 파라미터 Art 대기 |
| repair | 고장 수리 E키 | 코드 준비, Animator 파라미터 Art 대기 |

---

## 9. 완료 기준

- [ ] §7 남은 항목 (선택 회수·Art) 결정·통과
- [ ] Production 중 튜토 일시정지 시 수작업·수리도 정지 (Dev1 연동)

---

## 10. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md)
- [week3-art/04-protagonist-motions-polish](../week3-art/04-protagonist-motions-polish.md)
