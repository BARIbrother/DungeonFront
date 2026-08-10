# Quest itemcode 변환표

`questline.json`의 `itemcode`를 현재 아이템 체계(`itemId` + 레벨 + 인챈트)로 바꾼 전체 목록입니다.
검수용으로 **원본 추정 한글 → 변환 후 한글**을 같이 적었습니다. 틀린 매핑이 있으면 이 표를 고치면 됩니다.

런타임 구현: `Assets/Scripts/Quest/QuestItemCodeResolver.cs`

---

## 1. 코드 → 아이템 (이름/id 변환)

| 원본 itemcode | 원본 한글 (추정) | 변환 itemId | 변환 한글 | 비고 |
|---------------|------------------|-------------|---------|------|
| `iron_ore` | 철광석 | `iron_ore` | 철광석 | 동일 |
| `iron_ingot` | 철 주괴 | `iron_bar` | 철 주괴 | id만 정규화 |
| `iron_plate` | 철 판 | `iron_plate` | 철 판 | 동일 |
| `iron_bar` | 철 막대(기) | `iron_rod` | 철 막대기 | JSON에서 bar=막대, 우리 `iron_bar`는 주괴 |
| `iron_ingot_lv2` | 철 주괴 lv2 | `iron_bar` | 철 주괴 | **레벨 2** |
| `iron_sword` | 철 검 | `iron_sword` | 철 검 | 동일 |
| `iron_helmet` | 철 투구 | `iron_helmet` | 철 투구 | 동일 |
| `iron_chestplate` | 철 흉갑 | `iron_chestplate` | 철 흉갑 | 동일 |
| `iron_leggings` | 철 각반 | `iron_leggings` | 철 각반 | 동일 |
| `iron_boots` | 철 부츠 | `iron_boots` | 철 부츠 | 동일 |
| `iron_blade` | 철 대검 날 / 철 날 | `greatsword_blade` | 대검 날 | |
| `mana_wand` | 마나 완드 | `mana_wand` | 마나 완드 | 동일 |
| `manasteel_bar` | 마나강 주괴 | `mana_core` | 마력 코어 | **의심** — 우리 체계에 마나강 주괴 없음 |
| `Manasteel_ingot` | 마나강 주괴 | `mana_core` | 마력 코어 | 위와 동일 (대소문자만 다름) |
| `manasteel_sword` | 마나강 검 | `manasteel_sword` | 마나강 검 | 동일 |
| `manasteel_helmet` | 마나강 투구 | `manasteel_helmet` | 마나강 투구 | 동일 |
| `manasteel_chestplate` | 마나강 흉갑 | `manasteel_chestplate` | 마나강 흉갑 | 동일 |
| `manasteel_leggings` | 마나강 각반 | `manasteel_leggings` | 마나강 각반 | 동일 |
| `manasteel_boots` | 마나강 부츠 | `manasteel_boots` | 마나강 부츠 | 동일 |
| `element_scroll` | 원소 스크롤 | `element_scroll` | 원소 스크롤 | 동일 (중복 시 인챈트는 §3) |
| `darksteel_sword` | 흑강 검 | `darksteel_sword` | 흑강 검 | 동일 |
| `darksteel_helmet` | 흑강 투구 | `darksteel_helmet` | 흑강 투구 | 동일 |
| `darksteel_chestplate` | 흑강 흉갑 | `darksteel_chestplate` | 흑강 흉갑 | 동일 |
| `darksteel_leggings` | 흑강 각반 | `darksteel_leggings` | 흑강 각반 | 동일 |
| `darksteel_boots` | 흑강 부츠 | `darksteel_boots` | 흑강 부츠 | 동일 |
| `darksteel_ingot` | 흑강 주괴 | `darksteel_ingot` | 흑강 주괴 | 동일 |
| `brightsteel_sword` | 백강 검 | `brightsteel_sword` | 백강 검 | 동일 |
| `brightsteel_helmet` | 백강 투구 | `brightsteel_helmet` | 백강 투구 | 동일 |
| `brightsteel_chestplate` | 백강 흉갑 | `brightsteel_chestplate` | 백강 흉갑 | 동일 |
| `brightsteel_leggings` | 백강 각반 | `brightsteel_leggings` | 백강 각반 | 동일 |
| `brightsteel_boots` | 백강 부츠 | `brightsteel_boots` | 백강 부츠 | 동일 |
| `brightsteel_ingot` | 백강 주괴 | `brightsteel_ingot` | 백강 주괴 | 동일 |
| `greysteel_sword` | 진강 검 | `greysteel_sword` | 진강 검 | 동일 |
| `greysteel_helmet` | 진강 투구 | `greysteel_helmet` | 진강 투구 | 동일 |
| `greysteel_chestplate` | 진강 흉갑 | `greysteel_chestplate` | 진강 흉갑 | 동일 |
| `greysteel_leggings` | 진강 각반 | `greysteel_leggings` | 진강 각반 | 동일 |
| `greysteel_boots` | 진강 부츠 | `greysteel_boots` | 진강 부츠 | 동일 |
| `greysteel_ingot` | 진강 주괴 | `greysteel_ingot` | 진강 주괴 | 동일 |
| `greysteel_battlehammer` | 진강 전투망치 | `greysteel_warhammer` | 진강 전투 망치 | id 정규화 |
| `steel_column_framwork` | 철제 기둥 뼈대 (오타 framwork) | `iron_pillar_frame` | 철제 기둥 뼈대 | |
| `structural_column` | 구조물 기둥 | `structure_pillar` | 구조물 기둥 | |
| `structural_girder` | 구조물 대들보 | `structure_beam` | 구조물 대들보 | |
| `structural_roof` | 구조물 지붕 | `structure_roof` | 구조물 지붕 | |
| `warstained_executional_greatsword` | 전쟁에 물든 집행자 대검 | `war_stained_executor_greatsword` | 전쟁에 물든 집행자의 대검 | |
| `magicrobe` | 마법 로브 / 마술사 로브 | `mage_robe` | 마술사의 로브 | |
| `dark_mana_wand` | 흑마나 완드 / 흑마술 지팡이 | `dark_magic_staff` | 흑마술 지팡이 | |
| `bright_mana_wand` | 백마나 완드 / 백마술 지팡이 | `light_magic_staff` | 백마술 지팡이 | |
| `darkmana_core` | 흑마나 코어 / 흑마법 코어 | `dark_magic_core` | 흑마법 코어 | |
| `concrete` | 콘크리트 | `concrete` | 콘크리트 | 동일 |
| `gold` / `Gold` | 골드 | `gold` | 골드 | 동일 |
| `fame` / `Fame` | 명성 | `fame` | 명성 | 동일 |

---

## 2. 접미사 → 인챈트 (단일 itemcode에 속성 포함)

| 원본 itemcode | 원본 한글 (추정) | 변환 | 인챈트 |
|---------------|------------------|------|--------|
| `manasteel_sword_fire` | 마나강 검 (불/화염) | `manasteel_sword` | 불 (`Fire`) |
| `manasteel_sword_wind` | 마나강 검 (바람) | `manasteel_sword` | 바람 (`Wind`) |
| `manasteel_sword_earth` | 마나강 검 (대지/땅) | `manasteel_sword` | 땅 (`Earth`) |
| `manasteel_sword_water` | 마나강 검 (물) | `manasteel_sword` | 물 (`Water`) |
| `manasteel_chestplate_fire_proof` | 마나강 흉갑 (화염 내성/방화) | `manasteel_chestplate` | 불 (`Fire`) + 방어 (`Defense`) |
| `scroll_explosion` | 폭발 스크롤 (화염+바람) | `tier2_element_scroll` | 불+바람 (`Fire`+`Wind`) |
| `scroll_lava` | 용암 스크롤 (화염+대지) | `tier2_element_scroll` | 불+땅 (`Fire`+`Earth`) |
| `scroll_poison` | 독 스크롤 (화염+물) | `tier2_element_scroll` | 불+물 (`Fire`+`Water`) — **독(`Poison`) enum과 다름, 확인 필요** |
| `scroll_lightning` | 번개 스크롤 (바람+물) | `tier2_element_scroll` | 바람+물 (`Wind`+`Water`) — **전기(`Electric`) 미사용** |
| `scroll_nature` | 자연 스크롤 (바람+대지) | `tier2_element_scroll` | 바람+땅 (`Wind`+`Earth`) |
| `scroll_ice` | 얼음 스크롤 (대지+물) | `tier2_element_scroll` | 땅+물 (`Earth`+`Water`) — **얼음(`Ice`) enum과 다름** |

구 레시피 주석의 2단계 조합(폭발/용암/독/자연/번개/얼음)을 그대로 두 슬롯 인챈트로 옮긴 것입니다.

---

## 3. 같은 itemcode 중복 → 인챈트 분기 (퀘스트별)

같은 코드가 여러 칸인 경우, 등장 순서대로 인챈트를 나눠 붙였습니다.

### Q022 (세트 맞추기) — `manasteel_leggings` ×2

| 순서 | 원본 | 변환 | 인챈트 |
|------|------|------|--------|
| 1 | 마나강 각반 | `manasteel_leggings` | 불 |
| 2 | 마나강 각반 | `manasteel_leggings` | 물 |

### Q027 (롤링 페이퍼) — `element_scroll` ×4 (각 10장)

| 순서 | 원본 | 변환 | 인챈트 |
|------|------|------|--------|
| 1 | 원소 스크롤 | `element_scroll` ×10 | 불 |
| 2 | 원소 스크롤 | `element_scroll` ×10 | 물 |
| 3 | 원소 스크롤 | `element_scroll` ×10 | 바람 |
| 4 | 원소 스크롤 | `element_scroll` ×10 | 땅 |

### Q036 (상시 - 기본 스크롤 납품) — `element_scroll` ×4 (각 1장)

| 순서 | 원본 | 변환 | 인챈트 |
|------|------|------|--------|
| 1 | 원소 스크롤 | `element_scroll` ×1 | 불 |
| 2 | 원소 스크롤 | `element_scroll` ×1 | 물 |
| 3 | 원소 스크롤 | `element_scroll` ×1 | 바람 |
| 4 | 원소 스크롤 | `element_scroll` ×1 | 땅 |

### Q047 (대가족) — `manasteel_chestplate` ×6 (각 3개, 2슬롯)

본문에 “2단계 마법 인챈트”라고 해서 2단계 조합 6종을 흉갑에 붙였습니다.

| 순서 | 원본 | 변환 | 인챈트 (2개) | 구 조합명 |
|------|------|------|----------------|-----------|
| 1 | 마나강 흉갑 | `manasteel_chestplate` ×3 | 불+바람 | 폭발 |
| 2 | 마나강 흉갑 | `manasteel_chestplate` ×3 | 불+땅 | 용암 |
| 3 | 마나강 흉갑 | `manasteel_chestplate` ×3 | 불+물 | 독 |
| 4 | 마나강 흉갑 | `manasteel_chestplate` ×3 | 바람+물 | 번개 |
| 5 | 마나강 흉갑 | `manasteel_chestplate` ×3 | 바람+땅 | 자연 |
| 6 | 마나강 흉갑 | `manasteel_chestplate` ×3 | 땅+물 | 얼음 |

---

## 4. 특히 확인이 필요한 항목

1. **`manasteel_bar` / `Manasteel_ingot` → `mana_core`(마력 코어)**  
   원본은 “마나강 주괴”로 보이나, 현재 레시피/아이템에 마나강 주괴가 없어 마력 코어로 치환함.
2. **`iron_bar`(JSON) → `iron_rod`(철 막대기)**  
   같은 퀘스트에 `iron_ingot`+`iron_plate`+`iron_bar`가 같이 나와 bar=막대로 해석. 주괴로 보면 중복이 됨.
3. ~~바람 → 풀~~ → **`Wind`(바람)** 으로 통일.
4. **`scroll_poison` / `scroll_ice` / `scroll_lightning`**  
   조합식(화염+물 등)으로 넣었고, enum의 `Poison` / `Ice` / `Electric` 단일값은 쓰지 않음.
5. **`manasteel_chestplate_fire_proof`**  
   속성 불 + 형태 방어로 매핑.
6. **Q022 각반 불/물, Q047 흉갑 2속성 조합**  
   JSON에 속성이 없어 추정 할당. 형태는 `None`.

---

## 5. 인챈트 구조

한 슬롯 = **속성** (`EnchantmentId`) + **형태** (`EnchantmentForm`).
같은 속성은 형태가 달라도 한 아이템에 중복 불가.

### 속성

| enum | 한글 |
|------|------|
| `Fire` | 불 |
| `Water` | 물 |
| `Wind` | 바람 |
| `Electric` | 전기 |
| `Ice` | 얼음 |
| `Poison` | 독 |
| `Earth` | 땅 |
| `Pure` | 순수 |

### 형태

| enum | 한글 |
|------|------|
| `None` | 없음 |
| `Defense` | 방어 |
| `Ritual` | 의식 |

예: `manasteel_chestplate_fire_proof` → 속성 불 + 형태 방어.
일반 원소 접미사·2단계 스크롤 조합은 형태 `None`.
