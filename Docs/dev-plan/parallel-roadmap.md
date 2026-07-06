# 병렬 개발 로드맵

> **기준 문서**: [dev-plan.md](./dev-plan.md) · [lead-plan.md](./lead-plan.md) · [02-data-structure.md](../02-data-structure.md) · **[dev-contract.md](./dev-contract.md)**  
> **목적**: 팀원이 **서로의 미완 작업에 막히지 않고** 각자 브랜치에서 개발한 뒤, **금요일 통합 게이트**에서 합칠 수 있도록 재구성한 일정.

---

## 팀 역할

| 역할 | 담당 | 코드·에셋 소유 |
|------|------|----------------|
| **Lead** | 공장 **내부** — 맵·배치·생산 틱·물류·기계 SO·공장 UI | `Assets/Scripts/Placement/`, `Production/`, `Item/`(기계·레시피), Factory 씬 |
| **Dev1** | 공장 **외부** — **게임 플로우** (세션·페이즈·씬·HUD·튜토·세이브·타이틀) | `Assets/Scripts/GameFlow/` |
| **Dev2** | 공장 **외부** — **의뢰** (풀·수락·납기·결산 납품·보상·게임오버) | `Assets/Scripts/Quest/` |
| **Art** | 초상화·주인공 모션 (코드 없음) | `Assets/Art/` |

**Dev1 ↔ Dev2 분리 원칙**: Dev1은 의뢰 **상태를 읽고 이벤트만 구독**한다. Dev2는 `GameSessionState` **읽기** + `PlayerInventory` **계약 API**만 호출한다. **서로의 UI·Manager 내부를 수정하지 않는다.** Dev1은 Dev2 UI(`orderWindow` 등)를 토글하지 않는다.

---

## 독립 작업을 위한 계약 (Contract)

통합 전에도 각 트랙이 **목(Mock)·스텁**으로 동작할 수 있도록, 아래 인터페이스를 **Week 1 종료 시점에 고정**한다.  
**상세 시그니처·쓰기 권한**: [dev-contract.md](./dev-contract.md)

| 계약 | 소유 | 제공 (다른 역할이 쓰는 것) | 소비자 | Mock 방법 |
|------|------|---------------------------|--------|-----------|
| `GameSessionState` | Dev1 | `Phase`, `Day`, `Gold`, `Reputation`, `OnPhaseChanged`, `StartProduction()`, `AdvanceDay()` | Lead, Dev2 | Lead: 페이즈만 수동 토글하는 테스트 씬 |
| `PlayerInventory` | Dev1 | `GetCount`, `OnMachinesChanged`, `OnItemsChanged`, 배치 API | Lead, Dev2 | Dev2: Contracts Items로 `Add()` |
| `QuestManager` | Dev2 | `ActiveQuests`, `TryAccept`, `OnQuestAccepted` | Dev1 HUD | Dev1: `0/3` 하드코딩 |
| `IFactoryProduction` | Lead | `StartTick()`, `StopTick()`, `IsRunning` | Dev1 | Dev1: `NullFactoryProduction` 스텁 |
| **Contracts Items** | Lead | `Assets/Data/Contracts/Items/` | Dev2 | Dev2: 동일 id placeholder SO |
| Art drop zone | Art | `Assets/Art/UI/Portraits/`, `Assets/Art/Characters/Protagonist/` | Lead, Dev1 | Placeholder 색상 PNG |

```mermaid
flowchart LR
    subgraph Dev1["Dev1 — GameFlow"]
        GSS[GameSessionState]
        HUD[GlobalHUD]
        SCN[SceneFlow]
        SAV[SaveLoad]
    end

    subgraph Dev2["Dev2 — Quest"]
        QM[QuestManager]
        QUI[Quest UI]
        SET[SettlementDelivery]
    end

    subgraph Lead["Lead — Factory"]
        PLC[Placement]
        PTK[ProductionTick]
        LOG[Logistics]
    end

    subgraph Art["Art"]
        PRT[Portraits]
        MOT[Motions]
    end

    GSS -->|OnPhaseChanged| PTK
    GSS -->|StartProduction| PTK
    HUD -->|read ActiveQuests| QM
    QM -->|read Phase, Gold| GSS
    QM -->|read inventory| INV[PlayerInventory]
    SET -->|TryRemoveItem| INV
    MOT -->|sprites| PLC
    PRT -->|PNG| QUI
```

---

## 전체 타임라인 (병렬 레인)

```mermaid
gantt
    title MVP 병렬 로드맵 (Week 1 완료 · Week 2~5 잔여)
    dateFormat YYYY-MM-DD
    axisFormat %m/%d

    section Lead
    W2 배치·틱·6종·고장·출력기     :lead2, 2026-07-07, 7d
    W3 10종·벨트·수작업·레시피UI   :lead3, after lead2, 7d
    W4 레시피 lock·스토리훅        :lead4, after lead3, 7d
    W5 MVP 통합·버그               :lead5, after lead4, 7d

    section Dev1
    W2 인벤UI·HUD·씬흐름          :d1_2, 2026-07-07, 7d
    W3 튜토패널·HUD보강·틱연동     :d1_3, after d1_2, 7d
    W4 세이브·타이틀·대화UI껍데기   :d1_4, after d1_3, 7d
    W5 스토리연출·튜토전구간        :d1_5, after d1_4, 7d

    section Dev2
    W2 의뢰수락·풀·결산목록·SO     :d2_2, 2026-07-07, 7d
    W3 납품판정·보상·멀티데이       :d2_3, after d2_2, 7d
    W4 경제연동·게임오버·필수의뢰   :d2_4, after d2_3, 7d
    W5 의뢰 lock·결산 polish       :d2_5, after d2_4, 7d

    section Art
    W2 수리·작업모션·이브초상       :art2, 2026-07-07, 7d
    W3 레이초상·모션보완            :art3, after art2, 7d
    W4 네메시스·α초상 lock         :art4, after art3, 7d
    W5 폴리시·placeholder교체      :art5, after art4, 7d

    section 통합
    W2 금요일 통합 데모             :milestone, 2026-07-11, 1d
    W3 금요일 통합 데모             :milestone, 2026-07-18, 1d
    W4 금요일 통합 데모             :milestone, 2026-07-25, 1d
    W5 MVP 빌드                    :milestone, 2026-08-01, 1d
```

---

## 주차별 병렬 작업 그래프

### Week 1 — 완료 ✅

```mermaid
flowchart TB
    subgraph W1_Lead["Lead ✅"]
        L1[그리드맵·Factory씬]
        L2[이동·Prefab 4종]
        L3[SO·Database·생산UI]
    end

    subgraph W1_Dev1["Dev1 ✅"]
        D1[GameSessionState·페이즈]
        D2[NewGame·인벤초기화]
        D3[HUD·결산스텁·씬흐름]
    end

    subgraph W1_Dev2["Dev2 ✅"]
        Q1[QuestDefinition·Database 골격]
    end

    subgraph W1_Art["Art ✅"]
        A1[스타일가이드]
        A2[주인공 초상·idle/walk]
    end

    CONTRACT{{"계약 고정<br/>GameSessionState<br/>QuestDefinition schema"}}
    L3 --> CONTRACT
    D1 --> CONTRACT
    Q1 --> CONTRACT
```

---

### Week 2 — 배치·의뢰 UI (현재)

**병렬 원칙**: Dev1 `GameSessionState` 단일화. Dev2 UI는 `OnPhaseChanged` 자체 구독. Dev2 Quest SO는 `Assets/Data/Contracts/Items/`만 참조.

```mermaid
flowchart TB
    subgraph Lead_W2["Lead — 독립"]
        direction TB
        LW1[01 배치모드]
        LW2[02 레시피 SO v1]
        LW3[03 생산틱·철체인]
        LW4[04 기계 6종+]
        LW5[05 WIP·고장·수리]
        LW6[06 출력기]
        LW1 --> LW2 --> LW3
        LW3 --> LW4 --> LW5 --> LW6
    end

    subgraph Dev1_W2["Dev1 — 독립"]
        direction TB
        D1W1[01 인벤토리 UI]
        D1W2[04 HUD 의뢰·인벤 요약]
        D1W3[03 세션 단일화]
        D1W1 --> D1W2 --> D1W3
    end

    subgraph Dev2_W2["Dev2 — 독립"]
        direction TB
        D2W1[01 의뢰 에셋 v1]
        D2W2[02 의뢰 출현 풀]
        D2W3[03 의뢰 수락 UI]
        D2W4[04 결산 의뢰 목록]
        D2W1 --> D2W2 --> D2W3 --> D2W4
    end

    subgraph Art_W2["Art — 독립"]
        AW1[수리·작업 모션]
        AW2[이브 초상화]
        AW1 --> AW2
    end

    INT_W2{{"금요일 통합 W2<br/>수락→배치→생산→결산목록"}}
    LW6 --> INT_W2
    D1W3 --> INT_W2
    D2W4 --> INT_W2
    AW2 --> INT_W2
```

| 역할 | Issue (기존 문서) | 완료 시 다른 팀에 줄 것 |
|------|-------------------|------------------------|
| Lead | [week2-lead/](./week2/week2-lead/) | `IFactoryProduction` 구현, 배치 시 `PlayerInventory` 갱신 |
| Dev1 | [week2-dev1/](./week2/week2-dev1/) 01, 02, 03 | `OnPhaseChanged` 이벤트, 인벤 UI |
| Dev2 | [week2-dev2/](./week2/week2-dev2/) 01~04 | `QuestManager.TryAccept`, `ActiveQuests` |
| Art | [week2-art/](./week2/week2-art/) | 모션 시트·이브 PNG → 고정 경로 |

---

### Week 3 — 생산 연동·튜토·납품

**병렬 원칙**: Dev1의 틱 연동은 Lead `ProductionTickSystem` **인터페이스**에만 의존 (구현 없으면 스텁). Dev2 납품 판정은 Lead 생산 결과와 **무관** — 인벤 아이템 수만 검사.

```mermaid
flowchart TB
    subgraph Lead_W3["Lead — 독립"]
        L3A[01 기계 10종+]
        L3B[02 벨트·출력기 물류]
        L3C[03 수작업·수동운반]
        L3D[04 레시피전환·생산요약]
        L3A --> L3B --> L3C --> L3D
    end

    subgraph Dev1_W3["Dev1 — 독립"]
        D3A[01 튜토리얼 패널 1차]
        D3B[02 HUD 보강]
        D3C[03 생산·Lead 틱 연동]
        D3A --> D3B --> D3C
    end

    subgraph Dev2_W3["Dev2 — 독립"]
        D3D[납품 자동차감·충족판정]
        D3E[보상 지급 골드·명성]
        D3F[멀티데이 납기 추적]
        D3D --> D3E --> D3F
    end

    subgraph Art_W3["Art — 독립"]
        A3A[레이 초상화]
        A3B[주인공 모션 보완]
        A3A --> A3B
    end

    INT_W3{{"금요일 통합 W3<br/>튜토→철체인틱→결산납품"}}
    L3D --> INT_W3
    D3C --> INT_W3
    D3F --> INT_W3
    A3B --> INT_W3
```

---

### Week 4 — 세이브·경제·대화 UI

**병렬 원칙**: Dev1 세이브 DTO는 [dev-contract.md](./dev-contract.md) · [02-data-structure.md](../02-data-structure.md) **W4 시작 전 동결**. Dev2는 `quests[]` 필드만 직렬화 구현.

```mermaid
flowchart TB
    subgraph Lead_W4["Lead — 독립"]
        L4A[05 레시피·기계 SO lock]
        L4B[06 스토리 이벤트 트리거 훅]
        L4A --> L4B
    end

    subgraph Dev1_W4["Dev1 — 독립"]
        D4A[JSON 세이브·로드]
        D4B[타이틀·슬롯·새게임]
        D4C[대화 UI 껍데기]
        D4D[ESC 일시정지]
        D4A --> D4B --> D4C --> D4D
    end

    subgraph Dev2_W4["Dev2 — 독립"]
        D4E[준비단계 골드구매 UI]
        D4F[명성 해금·의뢰풀 threshold]
        D4G[필수의뢰 실패 게임오버]
        D4H[미납 명성 50% 차감]
        D4E --> D4F --> D4G --> D4H
    end

    subgraph Art_W4["Art — 독립"]
        A4A[네메시스 초상화]
        A4B[주변인물 α · 4~5명 lock]
        A4A --> A4B
    end

    INT_W4{{"금요일 통합 W4<br/>세이브↔로드 1일차 복원"}}
    L4B --> INT_W4
    D4D --> INT_W4
    D4H --> INT_W4
    A4B --> INT_W4
```

---

### Week 5 — MVP 통합·폴리시

```mermaid
flowchart TB
    subgraph Lead_W5["Lead"]
        L5[07 MVP 통합·잔여버그]
    end

    subgraph Dev1_W5["Dev1"]
        D5A[스토리·튜토 전구간 연동]
        D5B[스토리 only 모드 분기]
        D5A --> D5B
    end

    subgraph Dev2_W5["Dev2"]
        D5C[의뢰·보상 수치 lock]
        D5D[결산 UI polish]
        D5C --> D5D
    end

    subgraph Art_W5["Art"]
        A5[캐릭터 폴리시·BGM placeholder]
    end

    MVP{{"MVP 빌드<br/>튜토~클리어 1회 플레이"}}
    L5 --> MVP
    D5B --> MVP
    D5D --> MVP
    A5 --> MVP
```

---

## 의존성 그래프 (통합 게이트만 연결)

실선 = 해당 주 **내부** 순서. 점선 = **금요일에만** 합치는 지점. 평일에는 점선을 끊고 각자 Mock으로 개발.

```mermaid
flowchart TB
    classDef gate fill:#f9f,stroke:#333,stroke-width:2px
    classDef track fill:#e8f4fc,stroke:#369

    W1C["W1 계약 고정"]:::gate

    L2["Lead W2"]:::track
    D12["Dev1 W2"]:::track
    D22["Dev2 W2"]:::track
    A2["Art W2"]:::track
    G2["통합 W2"]:::gate

    L3["Lead W3"]:::track
    D13["Dev1 W3"]:::track
    D23["Dev2 W3"]:::track
    A3["Art W3"]:::track
    G3["통합 W3"]:::gate

    L4["Lead W4"]:::track
    D14["Dev1 W4"]:::track
    D24["Dev2 W4"]:::track
    A4["Art W4"]:::track
    G4["통합 W4"]:::gate

    L5["Lead W5"]:::track
    D15["Dev1 W5"]:::track
    D25["Dev2 W5"]:::track
    A5["Art W5"]:::track
    MVP["MVP"]:::gate

    W1C --> L2 & D12 & D22 & A2
    W1C -.-> G2

    L2 & D12 & D22 & A2 --> G2
    G2 --> L3 & D13 & D23 & A3

    L3 & D13 & D23 & A3 --> G3
    G3 --> L4 & D14 & D24 & A4

    L4 & D14 & D24 & A4 --> G4
    G4 --> L5 & D15 & D25 & A5

    L5 & D15 & D25 & A5 --> MVP
```

---

## 브랜치·머지 규칙

| 규칙 | 내용 |
|------|------|
| 브랜치 | `lead/w2-*`, `dev1/w2-*`, `dev2/w2-*`, `art/w2-*` — 역할·주차별 |
| 금요일 | 각자 `develop`에 PR → **통합 담당(Lead)** 이 머지 순서: Dev1 → Dev2 → Lead → Art |
| 충돌 방지 | Dev1·Dev2는 **서로의 폴더 미수정**. Lead는 `GameFlow/`·`Quest/` **읽기만** |
| 씬 변경 | Factory 씬 = Lead, Settlement·Title = Dev1, Quest UI 프리팹 = Dev2 |
| 계약 변경 | `GameSessionState`·`QuestManager` public API 변경 시 **#dev-contract** 채널에 하루 전 공지 |

---

## 금요일 통합 데모 시나리오

| 주차 | 시나리오 | 검증 계약 |
|------|----------|-----------|
| **W2** | NewGame → 인벤 UI → 의뢰 수락(0/3→1/3) → Lead 배치 → 생산 1회전 → 결산 n/m → 다음 날 | `QuestManager`, `OnPhaseChanged`, 인벤 갱신 |
| **W3** | 튜토 패널 2단계 → 생산 중 철 체인 틱 → 결산 납품·보상 → HUD 갱신 | `IFactoryProduction`, `EvaluateDelivery` |
| **W4** | 저장 → 종료 → 로드 → 필수 의뢰 실패 시 게임오버 → 다른 슬롯 | 세이브 JSON, `GameOver` |
| **W5** | 1일차~클리어 의뢰 라인 1회 플레이 (스토리 only 옵션) | MVP 완료 기준 7항 ([dev-plan.md](./dev-plan.md)) |

---

## Dev1 vs Dev2 Issue 매핑 (기존 week2~3 문서)

| 기존 Issue | 담당 | 비고 |
|------------|------|------|
| 01-session-phase, 02-new-game, 03-global-hud, 04-settlement-stub, 05-scene-flow | **Dev1** | W1 완료 |
| 06-quest-data | **Dev2** | W1 골격 → W2 에셋 v1에서 확장 |
| 01-inventory-ui, 02-hud-quest-summary, 03-scene-flow-enhancement | **Dev1** | [week2-dev1/](./week2/week2-dev1/) · HUD는 Quest 이벤트 구독만 |
| 01-quest-assets-v1, 02-quest-pool, 03-quest-accept-ui, 04-settlement-quest-list | **Dev2** | [week2-dev2/](./week2/week2-dev2/) · 순서 01→04 |
| 01-tutorial-panel, 02-hud-enhancements, 03-production-integration | **Dev1** | W3 · 03은 Lead 인터페이스 연동 |
| team-integration (W2) | **전원** | [week2/team-integration.md](./week2/team-integration.md) |

---

## 관련 문서

- [dev-plan.md](./dev-plan.md) — MVP 범위·7주 원본 일정
- [lead-plan.md](./lead-plan.md) — Lead 3주 상세
- [week2/week2.md](./week2/week2.md) · [week3/week3.md](./week3/week3.md) — 현재 주차 Issue
- [dev-contract.md](./dev-contract.md) — **팀 간 API·에셋 계약 (고정)**
- [dev-gaps.md](./dev-gaps.md) — 미정 항목
