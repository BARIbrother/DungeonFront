# 레시피·기계 SO lock

> **역할**: Lead · **Week**: 3 · **Issue**: 05  
> **선행**: [01-machines-10plus](./01-machines-10plus.md)  
> **입력**: [03-machine-plan.md](../../../03-machine-plan.md) · [01-core-loop.md](../../../01-core-loop.md) · [07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md)  
> **연동**: Dev2 의뢰 납품품 생산 가능 여부

---

## 1. 이 작업물

기획 표 확정 후 **Recipe·Item·Machine SO** 수치를 lock하고, `RecipeDatabase`에서 MVP `recipeId` **전부 조회 가능**하게 한다.  
튜토 의뢰 `00100001` 납품품이 **최소 1개 생산 경로**로 만들어지는지 검증.

**에셋**: `Assets/Recipe/`, `Assets/ItemDefinition/`, `Assets/ItemDefinition/MachineDef/`  
**코드**: `Assets/Scripts/Item/Recipe.cs`, `RecipePool.cs`, `RecipeManager.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| MVP 레시피 전체 inputs/outputs/틱/클릭 | tier 2 레시피 |
| Contracts Items + Lead 확장 Item | Dev2 Quest SO 작성 |
| 생산 체인 문서 1장 | 밸런스 시뮬 전체 |

---

## 3. MVP 레시피 표 (lock 대상)

[02-data-structure.md](../../../02-data-structure.md) · [01-core-loop.md](../../../01-core-loop.md)

| recipeId | machineTypeId | 입력 | 출력 | durationTicks | manualClickCount |
|----------|---------------|------|------|---------------|------------------|
| `채굴기_iron_ore` | `채굴기` | — | iron_ore ×1 | **30** | 0 |
| `용광로_iron` | `용광로` | iron_ore ×1 | iron ×1 | **15** | 0 |
| `제작기_iron_rod` | `제작기` | iron ×1 | iron_rod ×1 | 0 | **5** |
| `제작기_iron_plate` | `제작기` | iron ×1 | iron_plate ×1 | 0 | **5** |

**틱 환산**: 1초 = 10틱 → 철광석 3.0초/개, 철 1.5초/개

### 튜토 의뢰 `00100001` 요구 vs 생산

| 요구 itemId | 수량 | 생산 경로 |
|-------------|------|-----------|
| iron_ore | 5 | 채굴기 ×5회 (또는 버퍼 누적) |
| iron | 5 | 용광로 ×5회 |
| iron_plate | 1 | 제작기 레시피 전환 + 클릭 5 |
| iron_rod | 1 | 제작기 레시피 전환 + 클릭 5 |

**5분(3000틱) 내 가능성**: 채굴 30틱×5 = 150틱, 용광 15×5 = 75틱 — **수동 운반·제작 클릭** 포함해 튜토 가능하도록 lock.

### 마법 라인 (2번째 스토리 의뢰 이후)

[03-machine-plan.md](../../../03-machine-plan.md) 마법 체인 — 최소 레시피 id placeholder라도 SO 생성:

- `마나 추출기_*`, `마법 부여대_*`, `마나 제작기_*`, `제단_*`, `주조소_*`, `마나 저장소_*`
- 수치는 2차 lock 가능 — **id·연결만** W3 금요일 전 non-null

---

## 4. Item SO (튜토~클리어)

### Contracts (고정 id)

`Assets/Data/Contracts/Items/`: `iron_ore`, `iron`, `iron_rod`, `iron_plate`

### Lead 확장

튜토 이후 의뢰·마법 라인용 Item — `Assets/ItemDefinition/` 또는 `Assets/Data/Items/`  
**id 변경 금지** ([dev-contract.md](../../dev-contract.md))

---

## 5. RecipePool · Database

각 Machine Prefab:

- [ ] `RecipePool` — 해당 type이 처리 가능한 Recipe SO 목록
- [ ] `MachineRecipeUI` — 우클릭 시 목록 표시·`currentRecipe` 변경

**RecipeDatabase** (또는 `RecipeManager`):

- [ ] `GetRecipe(recipeId)` — MVP 표 전부 non-null
- [ ] 10종+ 기계 각각 `recipeIds` 연결 ([01-machines-10plus](./01-machines-10plus.md))

---

## 6. 생산 체인 문서 (산출물)

팀 공유용 **1장** — `Docs/` 또는 채널. 최소 포함:

```
철광석 노드 ─[채굴기 30틱]─→ iron_ore ─[수동/벨트]─→ 용광로 15틱 ─→ iron
                                                              └→ 제작기 5클릭 → iron_rod / iron_plate
```

마법 라인 요약 링크: [03-machine-plan.md](../../../03-machine-plan.md)

---

## 7. 구현 단계

- [ ] Item SO — Contracts 4종 + 확장 id 생성·icon 연결 (Art icon 경로)
- [ ] Recipe SO 4종+ — 필드 `inputEntryList`, `outputEntryList`, `durationByTick`, manual 클릭
- [ ] 각 Machine `RecipePool` 할당
- [ ] Database 등록 — 누락 id 검사 스크립트 또는 에디터 체크리스트
- [ ] PlayMode: 채굴→용광로→제작기 **00100001 요구품** 1세트 생산
- [ ] lock 후 SO 수치 변경 시 **팀 공지** (version 메모)

---

## 8. 완료 기준

- [ ] 10종+ 기계 `recipeIds` 전부 연결
- [ ] MVP `recipeId` 전부 Database non-null
- [ ] 튜토 의뢰 납품품 **1경로 이상** 생산 검증 통과
- [ ] 생산 체인 문서 1장 팀 공유
- [ ] Dev2 Quest `requiredItems`의 itemId가 전부 Item SO에 존재

---

## 9. 관련 문서

- [05-quest-lock-polish](../week3-dev2/05-quest-lock-polish.md)
- [01-machines-10plus](./01-machines-10plus.md)
