# 08 — 기계별 레시피 배정

> **정본**: 본 문서. 입출력 식은 [06-recipe.md](./06-recipe.md), 기계 역할은 [03-machine-plan.md](./03-machine-plan.md), 해금 시점은 `TechTreeCatalog`.  
> **범위**: 레시피를 고르는 생산 기계만. 벨트·출력기·창고·마나 저장소는 제외.  
> **상태**: RecipePool SO 연결됨 (`Assets/Recipe/RecipePool/`). `추가` 레시피 SO는 후속.

---

## 제외 (레시피 없음)

| 기계 | 이유 |
|------|------|
| 벨트 | 이송만 |
| 출력기 | 포트 중계만 |
| 창고 | 아이템 저장만 |
| 마나 저장소 | 마나 저장만 |

---

## RecipePool SO

상위 티어는 하위 레시피를 포함한다. `Miner_3`·`Assembler_3`은 직전 티어와 같은 풀이고 `workSpeed`만 더 빠르다 (T1=10, T2=15, T3=20). 시계는 1초 = 10틱이고, 진행도 단위만 10배다.

메뉴 `DungeonFront/Generate Week3 Machine Assets` 또는 `Bind Recipe Pools`가 Prefab `AvailableRecipes`에 꽂는다.

| 기계 | RecipePool | Prefab | workSpeed |
|------|------------|--------|-----------|
| 채굴기 1 | `RecipePool_Drill` | `Miner_machine` | 10 |
| 채굴기 2·3 | `RecipePool_Drill_2` | `Miner_2_machine`, `Miner_3_machine` | 15 / 20 |
| 용광로 1 | `RecipePool_Smelter` | `Smelter_machine` | 10 |
| 용광로 2 | `RecipePool_Smelter_2` | `Smelter_2_machine` | 15 |
| 용광로 3 | `RecipePool_Smelter_3` | `Smelter_3_machine` | 20 |
| 제작기 1 (수동·자동) | `RecipePool_Assembler` | `Handmade_Assembler_Machine`, `Assembler_machine` | 10 |
| 제작기 2·3 | `RecipePool_Assembler_2` | `Assembler_2_machine`, `Assembler_3_machine` | 15 / 20 |
| 마나 포집기 | `RecipePool_ManaExtractor` | `ManaExtractor_machine` | 10 |
| 마나 제작기 1 | `RecipePool_ManaAssembler` | `ManaHandmade_machine` | 10 |
| 마나 제작기 2 | `RecipePool_ManaAssembler_2` | `ManaAssembler_2_machine` | 15 |
| 마나 제작기 3 | `RecipePool_ManaAssembler_3` | `ManaAssembler_3_machine` | 20 |
| 마법 부여대 | `RecipePool_Enchanting` | `Enchanting_machine` | 10 |
| 주조소 | `RecipePool_Foundry` | `Foundry_machine` | 10 |
| 제단 | `RecipePool_Altar` | `Altar_machine` | 10 |

---

## recipeTime (틱)

기준은 철 주괴 제련 `smelt_iron_bar` = **100**. `workSpeed` 10이면 10틱(1초)에 1회 산출한다. 시계는 1초 = 10틱.

1:1 체인에서 하위 단계는 상위의 절반이 되도록 잡아, 균형 공장에서 대략 **하위 2 : 상위 1**이 맞는다. 아래 값은 임의 초안이며 SO `recipeTime`과 같다. `추가` 레시피는 SO가 생기면 이 표의 틱을 쓴다.

| 단계 | 틱 | 예시 |
|------|----|------|
| 채굴 (철·나무·돌) | 50 | `drill_iron_ore` |
| 채굴 (심층 광석) | 100 | `drill_mana_ore` **추가** |
| 제련·기초 가공 | 100 | `smelt_iron_bar`, `craft_wood_stick`, `craft_concrete` |
| 철 판·막대, 합금 주괴, 마력 코어 | 200 | `assemble_iron_plate`, `alloy_darksteel_ingot` |
| 철 장비, 합금 판·막대, 마나강 장비 | 400 | `craft_iron_sword`, `assemble_darksteel_plate` |
| 합금 장비, 진강 판·막대, 상급 스크롤 | 800 | `craft_darksteel_sword`, `craft_mage_robe` |
| 진강 장비, 구조물 지붕, 의식 조합 | 1600 | `craft_greysteel_sword`, `craft_sword_ritual` |
| 제단·전쟁 의식 | 3200 | `craft_altar`, `craft_war_ritual` |

---

## 배정 규칙

1. **한 레시피는 한 type만** 가진다. 같은 식을 용광로와 제작기에 동시에 넣지 않는다.
2. **상위 티어는 하위 레시피를 포함**한다. `용광로_3`은 철 주괴도 제련한다.
3. **용광로_n은 주괴 레벨을 올린다.** 이 용광로가 다루는 주괴를 `lv(n-1) ×5 → lv n`으로 재련한다. `용광로_1`은 광석 제련만 하고, 레벨 합성은 `용광로_2`부터다.
4. **수동·자동은 같은 풀**. `수동 제작대`와 `자동 제작기`는 물리 제작 풀을 공유하고, `수동 마나 제작대`와 상위 마나 제작기는 마법 제작 풀을 공유한다. 차이는 클릭 vs 틱이다.
5. **채굴기는 레시피 UI가 없다.** 설치한 자원 노드와 출력이 맞는 레시피를 자동 선택한다. 티어는 **캘 수 있는 노드 종류**만 늘린다. 돌은 모든 채굴기가 캔다.
6. 아래 `recipeId`는 현재 SO id다. `추가`로 표시한 채굴·부여·레벨 합성 레시피는 SO가 아직 없다.
7. **마법 부여대 출력은 itemId가 바뀌지 않는다.** 같은 아이템에 `Enchantment`(속성+형태)만 붙인다. 스크롤 각인 입력은 **무인챈트** 스크롤만 받는다.

판·막대는 테크트리 `m_furnace_1` 설명과 달리 **제작기**에 둔다. (`assemble_iron_plate` / `assemble_iron_rod`, 역할 = 물리적 형태 변화)

---

## 한눈에

| 기계 | 하는 일 | 레시피 수 (기존 SO) |
|------|---------|---------------------|
| 채굴기 | 노드에서 원석·원목·돌 | 1 + 추가 5 |
| 용광로 | 광석 제련, 합금, 주괴 레벨 합성 | 7 + 추가 |
| 제작기 (수동·자동) | 판·막대·장비 | 34 |
| 마나 포집기 | 공기에서 마나 정수 생성 | 4 |
| 마나 제작기 (수동·자동) | 마법 재료·스크롤·마나강·로브 | 21 |
| 마법 부여대 | 스크롤 각인, 장비에 스크롤 적용 | 추가 21 |
| 주조소 | 콘크리트·뼈대·구조물·거대 금속 | 10 |
| 제단 | 의식 카테고리 | 4 |

---

## 채굴기 (`Miner`)

풀: `RecipePool_Drill`. 노드 위 1대. 우클릭으로 레시피를 고르지 않는다.

| 해금 | 캘 수 있는 노드 | recipeId | 출력 | 틱 |
|------|-----------------|----------|------|----|
| 모든 채굴기 | 철광석 | `drill_iron_ore` | 철광석 | 50 |
| 모든 채굴기 | 나무 | `drill_wood_log` **추가** | 나무 원목 | 50 |
| 모든 채굴기 | 돌 | `drill_stone` **추가** | 돌 | 50 |
| 심층 굴착 (`Miner_2`) | 마력석 | `drill_mana_ore` **추가** | 마력석 광석 | 100 |
| 심층 굴착 (`Miner_2`) | 칠흑석 | `drill_blackstone_ore` **추가** | 칠흑석 광석 | 100 |
| 심층 굴착 (`Miner_2`) | 순백석 | `drill_whitestone_ore` **추가** | 순백석 광석 | 100 |

돌은 1티어부터 캘 수 있다. `Miner_2`는 그 위에 마력석·칠흑석·순백석을 더한다. `Miner_3`는 같은 목록을 더 빠르게 캔다.

---

## 용광로 (`Smelter`)

풀: `RecipePool_Smelter`. 광석을 녹이거나 합금하고, 주괴 레벨을 올린다. 판·막대·장비는 넣지 않는다.

**레벨 합성** — `용광로_n`은 자신이 다루는 주괴를 `lv(n-1) ×5 → lv n`으로 재련한다. 대상: 철·칠흑석·순백석·흑강·백강·진강 주괴. (진강은 `용광로_3`만)

### 시작 — `Smelter_1`

| recipeId | 식 | 틱 |
|----------|-----|----|
| `smelt_iron_bar` | 철광석 → 철 주괴 | 100 |
| `smelt_mana_crystal` | 마력석 광석 → 마력석 결정 | 100 |

마력석 제련은 용광로 1에도 있다. 재료는 `Miner_2` 이후에 들어온다. 레벨 합성은 없다.

### 더 좋은 용광로 — `Smelter_2` (위에 더함)

| recipeId | 식 | 틱 |
|----------|-----|----|
| `smelt_blackstone_ingot` | 칠흑석 광석 → 칠흑석 주괴 | 100 |
| `smelt_whitestone_ingot` | 순백석 광석 → 순백석 주괴 | 100 |
| `alloy_darksteel_ingot` | 철 주괴 + 칠흑석 주괴 → 흑강 주괴 | 200 |
| `alloy_brightsteel_ingot` | 철 주괴 + 순백석 주괴 → 백강 주괴 | 200 |
| `refine_*_lv2` **추가** | 철·칠흑석·순백석·흑강·백강 주괴 lv1 ×5 → 같은 주괴 lv2 | 200 |

### 고열 용광로 — `Smelter_3` (위에 더함)

| recipeId | 식 | 틱 |
|----------|-----|----|
| `alloy_greysteel_ingot` | 철 주괴 lv2 + 칠흑석 주괴 lv2 + 순백석 주괴 lv2 → 진강 주괴 | 400 |
| `refine_*_lv3` **추가** | 철·칠흑석·순백석·흑강·백강·진강 주괴 lv2 ×5 → 같은 주괴 lv3 | 400 |

---

## 제작기 (`HandmadeAssembler` / `Assembler`)

풀: `RecipePool_Assembler`. 물리적 형태 변화. 나무 가공, 판·막대, 금속 장비.

수동 제작대와 자동 제작기는 **같은 목록**이다. `Assembler_3`은 고속 제작기와 같은 레시피고, 더 빠르다. 대형 구조물 부품은 주조소.

### 시작 — `HandmadeAssembler_1` / `Assembler_1`

| recipeId | 식 | 틱 |
|----------|-----|----|
| `craft_wood_stick` | 나무 원목 → 나무 막대기 ×8 | 100 |
| `craft_paper` | 나무 원목 → 종이 ×4 | 100 |
| `assemble_iron_plate` | 철 주괴 → 철 판 | 200 |
| `assemble_iron_rod` | 철 주괴 → 철 막대기 ×2 | 200 |
| `craft_iron_helmet` | 철 판 ×2 + 철 주괴 ×4 → 철 투구 | 400 |
| `craft_iron_chestplate` | 철 판 ×4 + 철 주괴 ×6 → 철 흉갑 | 400 |
| `craft_iron_leggings` | 철 판 ×2 + 철 주괴 ×5 → 철 각반 | 400 |
| `craft_iron_boots` | 철 판 ×2 + 철 주괴 ×2 → 철 부츠 | 400 |
| `craft_iron_sword` | 철 막대기 ×2 + 나무 막대기 ×2 → 철 검 | 400 |
| `craft_iron_warhammer` | 철 판 ×4 + 철 막대기 ×2 + 나무 막대기 ×2 → 철 전투 망치 | 400 |

### 고속 제작기 — `Assembler_2` (위에 더함)

흑강·백강·진강 판·막대·장비. 진강 **주괴**는 고열 용광로.

| recipeId | 식 | 틱 |
|----------|-----|----|
| `assemble_darksteel_plate` | 흑강 주괴 → 흑강 판 | 400 |
| `assemble_darksteel_rod` | 흑강 주괴 → 흑강 막대기 ×2 | 400 |
| `craft_darksteel_helmet` | 철 투구 + 흑강 판 ×3 → 흑강 투구 | 800 |
| `craft_darksteel_chestplate` | 철 흉갑 + 흑강 판 ×6 → 흑강 흉갑 | 800 |
| `craft_darksteel_leggings` | 철 각반 + 흑강 판 ×5 → 흑강 각반 | 800 |
| `craft_darksteel_boots` | 철 부츠 + 흑강 판 ×2 → 흑강 부츠 | 800 |
| `craft_darksteel_sword` | 철 검 + 흑강 주괴 ×2 + 흑강 막대기 ×2 → 흑강 검 | 800 |
| `craft_darksteel_warhammer` | 철 전투 망치 + 흑강 판 ×4 + 흑강 막대기 ×2 → 흑강 전투 망치 | 800 |
| `assemble_brightsteel_plate` | 백강 주괴 → 백강 판 | 400 |
| `assemble_brightsteel_rod` | 백강 주괴 → 백강 막대기 ×2 | 400 |
| `craft_brightsteel_helmet` | 철 투구 + 백강 판 ×3 → 백강 투구 | 800 |
| `craft_brightsteel_chestplate` | 철 흉갑 + 백강 판 ×6 → 백강 흉갑 | 800 |
| `craft_brightsteel_leggings` | 철 각반 + 백강 판 ×5 → 백강 각반 | 800 |
| `craft_brightsteel_boots` | 철 부츠 + 백강 판 ×2 → 백강 부츠 | 800 |
| `craft_brightsteel_sword` | 철 검 + 백강 주괴 ×2 + 백강 막대기 ×2 → 백강 검 | 800 |
| `craft_brightsteel_warhammer` | 철 전투 망치 + 백강 판 ×4 + 백강 막대기 ×2 → 백강 전투 망치 | 800 |
| `assemble_greysteel_plate` | 진강 주괴 → 진강 판 | 800 |
| `assemble_greysteel_rod` | 진강 주괴 → 진강 막대기 ×2 | 800 |
| `craft_greysteel_helmet` | 흑강 투구 + 백강 투구 + 진강 판 ×1 → 진강 투구 | 1600 |
| `craft_greysteel_chestplate` | 흑강 흉갑 + 백강 흉갑 + 진강 판 ×2 → 진강 흉갑 | 1600 |
| `craft_greysteel_leggings` | 흑강 각반 + 백강 각반 + 진강 판 ×2 → 진강 각반 | 1600 |
| `craft_greysteel_boots` | 흑강 부츠 + 백강 부츠 + 진강 판 ×1 → 진강 부츠 | 1600 |
| `craft_greysteel_sword` | 흑강 검 + 백강 검 + 진강 막대기 ×2 → 진강 검 | 1600 |
| `craft_greysteel_warhammer` | 흑강 전투 망치 + 백강 전투 망치 + 진강 막대기 ×2 → 진강 전투 망치 | 1600 |

---

## 마나 포집기 (`ManaExtractor`)

풀: `RecipePool_ManaExtractor`. 공기에서 마나 정수를 만든다. 입력 없음. 채굴기와 달리 노드가 필요 없고, 우클릭으로 정수 등급을 고른다. 티어 1만 있다.

느슨한 `마나` 아이템은 쓰지 않는다. 정수가 곧 마나 묶음이다.

| 정수 | itemId | 마나 함량 |
|------|--------|-----------|
| 하급 마나 정수 | `low_monster_mana_essence` | 10 |
| 중급 마나 정수 | `mid_monster_mana_essence` | 50 |
| 상급 마나 정수 | `high_monster_mana_essence` | 100 |
| 던전의 주인의 정수 | `dungeon_master_essence` | 500 |

| recipeId | 식 | 틱 |
|----------|-----|----|
| `extract_mana_low` | (없음) → 하급 마나 정수 | 100 |
| `extract_mana_mid` | (없음) → 중급 마나 정수 | 200 |
| `extract_mana_high` | (없음) → 상급 마나 정수 | 400 |
| `extract_mana_dungeon_master` | (없음) → 던전의 주인의 정수 | 800 |

가공 레시피의 옛 `마나 ×N`은 함량 합이 N이 되도록 정수로 바꾼다. 큰 단위를 우선한다.

| 옛 마나 | 정수 |
|---------|------|
| ×100 | 상급 ×1 |
| ×250 | 상급 ×2 + 중급 ×1 |
| ×300 | 상급 ×3 |
| ×500 | 던전의 주인의 정수 ×1 |
| ×1000 | 던전의 주인의 정수 ×2 |
| ×1500 | 던전의 주인의 정수 ×3 |

---

## 마나 제작기 (`ManaHandmade` / `ManaAssembler`)

풀: `RecipePool_ManaAssembler`. 마법 재료·스크롤·마나강·로브·지팡이. 금속 판·막대·일반 장비는 넣지 않는다.

### 마나 가공 — `ManaHandmade_1`

| recipeId | 식 | 틱 |
|----------|-----|----|
| `craft_mana_core` | 마력석 결정 + 철 주괴 → 마력 코어 | 200 |
| `craft_mana_wand` | 마력 코어 + 나무 막대기 ×2 → 마나 완드 | 400 |
| `craft_manasteel_sword` | 철 검 + 마력 코어 ×2 → 마나강 검 | 400 |
| `craft_manasteel_helmet` | 철 투구 + 마력 코어 ×2 → 마나강 투구 | 400 |
| `craft_manasteel_chestplate` | 철 흉갑 + 마력 코어 ×4 → 마나강 흉갑 | 400 |
| `craft_manasteel_leggings` | 철 각반 + 마력 코어 ×3 → 마나강 각반 | 400 |
| `craft_manasteel_boots` | 철 부츠 + 마력 코어 ×2 → 마나강 부츠 | 400 |
| `craft_blank_magic_scroll` | 종이 + 상급 마나 정수 → 빈 마법 스크롤 | 200 |
| `craft_element_scroll` | 빈 마법 스크롤 + 상급 마나 정수 ×3 → 원소 스크롤 | 400 |
| `craft_element_form_scroll` | 원소 스크롤 + 상급 마나 정수 → 원소 형태 스크롤 | 800 |

여기서는 스크롤 **실물**만 만든다. 속성·형태 각인과 장비 적용은 마법 부여대.

### 정교한 마나제작 — `ManaAssembler_2` (위에 더함)

| recipeId | 식 | 틱 |
|----------|-----|----|
| `craft_blank_tier2_magic_scroll` | 빈 마법 스크롤 + 마력 코어 + 상급 마나 정수 → 빈 2단계 마법 스크롤 | 400 |
| `craft_tier2_element_scroll` | 빈 2단계 마법 스크롤 + 던전의 주인의 정수 → 2단계 원소 스크롤 | 800 |
| `craft_tier2_element_form_scroll` | 2단계 원소 스크롤 + 상급 마나 정수 ×2 + 중급 마나 정수 → 2단계 원소 형태 스크롤 | 1600 |
| `craft_mage_robe` | 마나강 흉갑 + 마나강 각반 + 던전의 주인의 정수 ×3 → 마술사의 로브 | 800 |
| `craft_dark_magic_core` | 마력 코어 lv2 + 칠흑석 주괴 lv2 → 흑마법 코어 | 400 |
| `craft_dark_mage_robe` | 마술사의 로브 + 흑마법 코어 → 흑마술사의 로브 | 800 |
| `craft_dark_magic_staff` | 마나 완드 + 흑마법 코어 → 흑마술 지팡이 | 800 |
| `craft_light_magic_core` | 마력 코어 lv2 + 순백석 주괴 lv2 → 백마법 코어 | 400 |
| `craft_light_mage_robe` | 마술사의 로브 + 백마법 코어 → 백마술사의 로브 | 800 |
| `craft_light_magic_staff` | 마나 완드 + 백마법 코어 → 백마술 지팡이 | 800 |

### 의식 세공 — `ManaAssembler_3` (위에 더함)

| recipeId | 식 | 틱 |
|----------|-----|----|
| `craft_ritual_scroll` | 빈 2단계 마법 스크롤 + 던전의 주인의 정수 ×2 → 의식 스크롤 | 800 |

대검 날·의식용 대검은 거대 금속이라 주조소. 테크트리 `m_manacraft_3` 문구(대검 날)와 여기가 다르다.

---

## 마법 부여대 (`Enchanting`)

풀: `RecipePool_Enchanting` (지금은 비어 있음. `추가` SO가 생기면 바인더가 채운다).

스크롤에 속성·형태를 각인하고, 각인된 스크롤을 장비에 붙인다. 티어 1만 있다. 2단계 재료는 마나 제작기 2 이후에 들어온다.

공격은 `EnchantmentForm.None`, 방어는 `EnchantmentForm.Defense`. 원소 스크롤을 장비에 붙이면 공격, 원소 형태 스크롤은 방어다.

2단계 이름은 컨셉트다. 데이터는 기본 원소 두 개다. (`Electric` / `Ice` / `Poison` enum을 쓰지 않는다.)

| 이름 | 속성 |
|------|------|
| 폭발 | 불 + 바람 |
| 용암 | 불 + 땅 |
| 독 | 불 + 물 |
| 자연 | 바람 + 땅 |
| 번개 | 바람 + 물 |
| 얼음 | 땅 + 물 |

### 1단계 스크롤 각인 **추가**

| recipeId | 식 | 틱 |
|----------|-----|----|
| `enchant_element_fire` | 원소 스크롤 → 원소 스크롤 [불] | 100 |
| `enchant_element_water` | 원소 스크롤 → 원소 스크롤 [물] | 100 |
| `enchant_element_wind` | 원소 스크롤 → 원소 스크롤 [바람] | 100 |
| `enchant_element_earth` | 원소 스크롤 → 원소 스크롤 [땅] | 100 |
| `enchant_form_fire` | 원소 형태 스크롤 → 원소 형태 스크롤 [불, 방어] | 200 |
| `enchant_form_water` | 원소 형태 스크롤 → 원소 형태 스크롤 [물, 방어] | 200 |
| `enchant_form_wind` | 원소 형태 스크롤 → 원소 형태 스크롤 [바람, 방어] | 200 |
| `enchant_form_earth` | 원소 형태 스크롤 → 원소 형태 스크롤 [땅, 방어] | 200 |

### 2단계 스크롤 각인 **추가**

| recipeId | 식 | 틱 |
|----------|-----|----|
| `enchant_tier2_explosion` | 2단계 원소 스크롤 → 2단계 원소 스크롤 [불, 바람] | 400 |
| `enchant_tier2_lava` | 2단계 원소 스크롤 → 2단계 원소 스크롤 [불, 땅] | 400 |
| `enchant_tier2_poison` | 2단계 원소 스크롤 → 2단계 원소 스크롤 [불, 물] | 400 |
| `enchant_tier2_nature` | 2단계 원소 스크롤 → 2단계 원소 스크롤 [바람, 땅] | 400 |
| `enchant_tier2_lightning` | 2단계 원소 스크롤 → 2단계 원소 스크롤 [바람, 물] | 400 |
| `enchant_tier2_ice` | 2단계 원소 스크롤 → 2단계 원소 스크롤 [땅, 물] | 400 |
| `enchant_tier2_form_explosion` | 2단계 원소 형태 스크롤 → 2단계 원소 형태 스크롤 [불·바람, 방어] | 800 |
| `enchant_tier2_form_lava` | 2단계 원소 형태 스크롤 → 2단계 원소 형태 스크롤 [불·땅, 방어] | 800 |
| `enchant_tier2_form_poison` | 2단계 원소 형태 스크롤 → 2단계 원소 형태 스크롤 [불·물, 방어] | 800 |
| `enchant_tier2_form_nature` | 2단계 원소 형태 스크롤 → 2단계 원소 형태 스크롤 [바람·땅, 방어] | 800 |
| `enchant_tier2_form_lightning` | 2단계 원소 형태 스크롤 → 2단계 원소 형태 스크롤 [바람·물, 방어] | 800 |
| `enchant_tier2_form_ice` | 2단계 원소 형태 스크롤 → 2단계 원소 형태 스크롤 [땅·물, 방어] | 800 |

### 장비에 스크롤 적용 **추가**

장비마다 식을 두지 않는다. 스크롤의 인챈트를 대상에 복사하고 스크롤을 소모한다.

| recipeId | 식 | 틱 |
|----------|-----|----|
| `enchant_apply_scroll` | 인챈트 가능 장비 + 각인된 스크롤 → 같은 장비(스크롤의 인챈트) | 200 |

- 대상의 **빈 슬롯 수**가 스크롤 인챈트 수 이상이어야 한다.
- 1슬롯: 마나강 검·투구·흉갑·각반·부츠. 1단계 스크롤.
- 2슬롯: 마술사·흑·백 로브, 흑·백 지팡이. 2단계 스크롤. (1단계 스크롤은 슬롯 1개만 채운다.)

---

## 주조소 (`Foundry`)

풀: `RecipePool_Foundry`. 거대 카테고리. 콘크리트, 뼈대, 구조물, 의식용 대검. 테크트리 `m_crafter_3` 문구(뼈대·제단을 제작기)와 여기가 다르다.

| recipeId | 식 | 틱 |
|----------|-----|----|
| `craft_concrete` | 돌 → 콘크리트 | 100 |
| `craft_iron_pillar_frame` | 철 주괴 ×10 + 철 막대기 ×20 → 철제 기둥 뼈대 | 400 |
| `craft_iron_beam_frame` | 철 주괴 ×10 + 철 막대기 ×20 → 철제 대들보 뼈대 | 400 |
| `craft_iron_roof_frame` | 철 주괴 ×20 + 철 막대기 ×50 → 철제 지붕 뼈대 | 800 |
| `craft_structure_pillar` | 철제 기둥 뼈대 + 콘크리트 ×50 → 구조물 기둥 | 800 |
| `craft_structure_beam` | 철제 대들보 뼈대 + 콘크리트 ×50 → 구조물 대들보 | 800 |
| `craft_structure_roof` | 철제 지붕 뼈대 + 콘크리트 ×150 → 구조물 지붕 | 1600 |
| `craft_altar` | 구조물 기둥 ×8 + 구조물 대들보 ×2 + 구조물 지붕 → 제단 | 3200 |
| `craft_greatsword_blade` | 철 판 lv3 ×20 + 철 막대기 lv3 ×10 → 대검 날 | 800 |
| `craft_ritual_iron_greatsword` | 대검 날 + 철 막대기 lv3 ×10 + 철 주괴 lv3 ×10 → 의식용 철제 대검 | 1600 |

---

## 제단 (`Altar`)

풀: `RecipePool_Altar`. 의식 카테고리. 스크롤·대검을 **조합**해 새 아이템을 만든다. 장비에 속성·형태를 붙이는 건 마법 부여대.

| recipeId | 식 | 틱 |
|----------|-----|----|
| `craft_sword_ritual` | 의식 스크롤 + 철 검 ×10 + 흑강 검 ×10 + 백강 검 ×10 + 진강 검 ×10 → 검의 의식 | 1600 |
| `craft_war_ritual` | 검의 의식 ×10 → 전쟁의 의식 | 3200 |
| `craft_executor_greatsword` | 의식용 철제 대검 + 검의 의식 → 집행자의 대검 | 1600 |
| `craft_war_stained_executor_greatsword` | 집행자의 대검 + 전쟁의 의식 → 전쟁에 물든 집행자의 대검 | 3200 |

---

## 후속 (이 문서에서 안 나눔)

- **채굴 `추가` 5종** — `drill_wood_log`, `drill_stone`, `drill_mana_ore`, `drill_blackstone_ore`, `drill_whitestone_ore` SO 생성. 틱은 위 표.
- **주괴 레벨 합성 `추가`** — `refine_*_lv2` (200틱) / `refine_*_lv3` (400틱). 출력 `Item.level`을 담는 Recipe 필드가 필요할 수 있다.
- **부여 `추가` 21종** — 스크롤 각인 20 + `enchant_apply_scroll`. 출력에 Enchantment를 담는 Recipe 필드가 필요할 수 있다. SO가 생기면 `RecipePoolBinder`가 `RecipePool_Enchanting`에 넣는다.

---

## 관련 문서

- [06-recipe.md](./06-recipe.md) — 식·레벨·인챈트 구분
- [03-machine-plan.md](./03-machine-plan.md) — 기계 역할
- [item-english-names.md](./item-english-names.md) — itemId · recipeId
