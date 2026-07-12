# 병렬 개발 로드맵 (3주)

> **기준 문서**: [dev-plan.md](./dev-plan.md) · [lead-plan.md](./lead-plan.md) · [dev-contract.md](./dev-contract.md)  
> **팀**: Lead · Dev1 · Dev2 · Art (4명)  
> **목적**: **3주 안에 MVP 완료**. 각 주차에서 4명의 작업은 **서로 기다리지 않고** 병렬 진행, **금요일 통합**에서만 합친다.

---

## 독립 작업 원칙

| 규칙 | 내용 |
|------|------|
| 평일 | 각자 Mock·스텁으로 자기 트랙만 개발. **다른 역할의 미완 구현을 전제로 하지 않는다** |
| 계약 | [dev-contract.md](./dev-contract.md) public API만 읽기·쓰기. W1 종료 시 고정 |
| 금요일 | 4트랙 머지 → 통합 데모. 이때만 실연동 검증 |
| Dev1 ↔ Dev2 | 서로의 폴더·UI Manager **미수정**. 이벤트·계약 API만 사용 |
| Lead ↔ Dev | Lead는 `GameFlow/`·`Quest/` **읽기만**. Dev는 Factory 씬 **미수정** |

```mermaid
flowchart LR
    subgraph Dev1["Dev1 — GameFlow"]
        GSS[GameSessionState]
        HUD[GlobalHUD]
        SAV[SaveLoad]
        TUT[Tutorial]
    end

    subgraph Dev2["Dev2 — Quest"]
        QM[QuestManager]
        ECO[Economy]
        SET[Settlement]
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

    GSS -.->|계약 API| PTK
    GSS -.->|계약 API| QM
    QM -.->|계약 API| HUD
    QM -.->|계약 API| INV[PlayerInventory]
    MOT -.->|PNG| PLC
    PRT -.->|PNG| TUT
```

점선 = **금요일 통합 시** 연결. 평일에는 끊고 Mock 사용.

---

## 전체 타임라인

```mermaid
gantt
    title MVP 3주 병렬 로드맵
    dateFormat YYYY-MM-DD
    axisFormat %m/%d

    section Lead
    W1 Factory기반 ✅           :done, lead1, 2026-06-30, 7d
    W2 6종·고장·출력기         :lead2, 2026-07-07, 7d
    W3 10종·수작업·MVP통합      :lead3, after lead2, 7d

    section Dev1
    W1 세션·HUD·씬 ✅           :done, d1_1, 2026-06-30, 7d
    W2 인벤·HUD·SaveDTO·타이틀  :d1_2, 2026-07-07, 7d
    W3 튜토·세이브·대화·스토리   :d1_3, after d1_2, 7d

    section Dev2
    W1 Quest골격 ✅             :done, d2_1, 2026-06-30, 7d
    W2 의뢰UI·납품·보상         :d2_2, 2026-07-07, 7d
    W3 경제·게임오버·lock       :d2_3, after d2_2, 7d

    section Art
    W1 주인공·가이드 ✅         :done, art1, 2026-06-30, 7d
    W2 수리모션·이브초상        :art2, 2026-07-07, 7d
    W3 레이·표정·모션보완       :art3, after art2, 7d

    section 통합
    W1 통합 ✅                  :milestone, done, 2026-07-04, 1d
    W2 금요일 통합              :milestone, 2026-07-11, 1d
    W3 MVP 빌드                 :milestone, 2026-07-18, 1d
```

---

## Week 1 — 기반 ✅

배치·틱·벨트·WIP·SO·`GameSessionState`·`QuestManager` 골격 등 **구현 완료**.  
기획 참고: [week1/week1.md](./week1/week1.md)

**계약 고정**: `GameSessionState` · `PlayerInventory` · `Quest` SO · Contracts Items

---

## Week 2 — 의뢰 UI · 납품 · 6종+

```mermaid
flowchart TB
    subgraph Lead_W2["Lead — 독립"]
        LW4[04 기계 6종+]
        LW5[05 고장]
        LW6[06 출력기]
        LW4 --> LW5 --> LW6
    end

    subgraph Dev1_W2["Dev1 — 독립"]
        D1W1[01 인벤토리 UI]
        D1W2[02 HUD]
        D1W3[03 세션 단일화]
        D1W4[04 SaveDTO]
        D1W5[05 타이틀]
        D1W1 --> D1W2 --> D1W3 --> D1W4 --> D1W5
    end

    subgraph Dev2_W2["Dev2 — 독립"]
        D2W1[01~05 의뢰·납품]
    end

    subgraph Art_W2["Art — 독립"]
        AW1[수리 모션]
        AW2[이브 초상]
        AW1 --> AW2
    end

    INT_W2{{"금요일 W2"}}
    LW6 & D1W5 & D2W1 & AW2 --> INT_W2
```

| 역할 | Issue | Mock (평일) |
|------|-------|-------------|
| Lead | [week2-lead/](./week2/week2-lead/) | Dev1 페이즈 수동 토글 |
| Dev1 | [week2-dev1/](./week2/week2-dev1/) | `QuestManager` → HUD `0/3` |
| Dev2 | [week2-dev2/](./week2/week2-dev2/) | 인벤에 Contracts Items `Add()` |
| Art | [week2-art/](./week2/week2-art/) | — |

---

## Week 3 — MVP 완성

```mermaid
flowchart TB
    subgraph Lead_W3["Lead — 독립"]
        L3A[01 10종+]
        L3C[03 수작업]
        L3D[04 생산 요약]
        L3E[05 lock]
        L3F[06 스토리]
        L3G[07 MVP]
        L3A --> L3C --> L3D --> L3E --> L3F --> L3G
    end

    subgraph Dev1_W3["Dev1 — 독립"]
        D3A[01 튜토]
        D3B[02 HUD]
        D3D[04 세이브]
        D3E[05 대화]
        D3A --> D3B --> D3D --> D3E
    end

    subgraph Dev2_W3["Dev2 — 독립"]
        D3F[01~05 경제·게임오버]
    end

    subgraph Art_W3["Art — 독립"]
        A3A[레이·표정·모션]
    end

    MVP{{"MVP"}}
    L3G & D3E & D3F & A3A --> MVP
```

| 역할 | Issue | Mock (평일) |
|------|-------|-------------|
| Lead | [week3-lead/](./week3/week3-lead/) | `StoryEventBus.RaiseMock()` |
| Dev1 | [week3-dev1/](./week3/week3-dev1/) | `factory: null`, `quests: []` |
| Dev2 | [week3-dev2/](./week3/week3-dev2/) | reputation 하드코딩 |
| Art | [week3-art/](./week3/week3-art/) | — |

---

## 의존성 그래프 (통합 게이트만 연결)

```mermaid
flowchart TB
    classDef gate fill:#f9f,stroke:#333,stroke-width:2px
    classDef track fill:#e8f4fc,stroke:#369

    W1C["W1 계약 고정 ✅"]:::gate

    L2["Lead W2"]:::track
    D12["Dev1 W2"]:::track
    D22["Dev2 W2"]:::track
    A2["Art W2"]:::track
    G2["통합 W2"]:::gate

    L3["Lead W3"]:::track
    D13["Dev1 W3"]:::track
    D23["Dev2 W3"]:::track
    A3["Art W3"]:::track
    MVP["MVP ✅"]:::gate

    W1C --> L2 & D12 & D22 & A2
    L2 & D12 & D22 & A2 --> G2
    G2 --> L3 & D13 & D23 & A3
    L3 & D13 & D23 & A3 --> MVP
```

실선 = 해당 주 **내부** 순서. 다른 역할 간 화살표는 **금요일에만** 연결.

---

## Mock 요약표

| 소비자 | 필요 대상 | 평일 Mock |
|--------|-----------|-----------|
| Dev1 HUD | `QuestManager` | `의뢰: 0/3` 하드코딩 |
| Dev1 세이브 | Lead `IFactorySave` | `factory: null` 저장 |
| Dev1 스토리 | Lead `StoryEventBus` | `RaiseMock(id)` 로컬 테스트 |
| Dev2 의뢰 | `PlayerInventory` | Contracts Items `Add()` |
| Dev2 경제 | 명성 해금 표 | reputation 임의값 |

---

## 브랜치·머지 규칙

| 규칙 | 내용 |
|------|------|
| 브랜치 | `lead/wN-*`, `dev1/wN-*`, `dev2/wN-*`, `art/wN-*` |
| 금요일 머지 순서 | Dev1 → Dev2 → Lead → Art → `develop` |
| 계약 변경 | `GameSessionState`·`QuestManager` public API 변경 시 **#dev-contract** 1일 전 공지 |

---

## 금요일 통합 데모

| 주차 | 시나리오 |
|------|----------|
| **W1** ✅ | Factory 이동 → 생산 타이머 → 결산 1회전 |
| **W2** | NewGame → 인벤 UI → 의뢰 수락 → Lead 배치 → 생산 틱 → 결산 **납품·보상** → 다음 날 |
| **W3** | 타이틀 슬롯 → 로드 → 튜토 2단계 → 10종+·벨트 체인 생산 → 스토리 이벤트 → 클리어 의뢰 납품 → 세이브·재시작 |

---

## 관련 문서

- [dev-plan.md](./dev-plan.md) — MVP 범위·3주 일정표
- [dev-contract.md](./dev-contract.md) — API·에셋 계약
- [lead-plan.md](./lead-plan.md) — Lead 상세
- [week2/team-integration.md](./week2/team-integration.md) — W2 통합 체크리스트
- [week3/week3-dev1/06-team-integration.md](./week3/week3-dev1/06-team-integration.md) — W3 통합 체크리스트
