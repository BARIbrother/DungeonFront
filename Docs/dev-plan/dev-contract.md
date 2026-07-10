# 팀 간 개발 계약 (Contract)

> **상태**: Week 1 기준 **고정**. 시그니처 변경 시 `#dev-contract`에 **1일 전** 공지 + 전원 합의.  
> **상위**: [parallel-roadmap.md](./parallel-roadmap.md) · [02-data-structure.md](../02-data-structure.md)

---

## 원칙

1. **단일 세션 소스**: `GameSessionState`만 사용. `GameFlowController`는 **레거시** — 신규 코드에서 참조 금지, Dev1이 W2에 통합·제거.
2. **인벤 단일 소스**: `PlayerInventory`만 사용. `GameSessionState.inventory` 스텁 **사용 금지**.
3. **UI 소유**: 각 역할이 **자기 UI만** 표시/숨김. Dev1이 Dev2 창(`orderWindow` 등)을 직접 토글하지 않는다.
4. **쓰기 권한**: 아래 표의 **허용 API**만 호출. 그 외 필드 직접 수정 금지.

---

## GameSessionState (Dev1 · `Assets/Scripts/GameFlow/`)

| 구분 | 멤버 | Lead | Dev2 | Dev1 |
|------|------|:----:|:----:|:----:|
| 읽기 | `Instance`, `Day`, `Phase`, `Gold`, `Reputation` | ✅ | ✅ | ✅ |
| 읽기 | `ProductionRemainingSeconds` | ✅ | — | ✅ |
| 이벤트 | `OnPhaseChanged(GamePhase)` | 구독 | 구독 | 발행 |
| 호출 | `NewGame()` | — | — | ✅ |
| 호출 | `SetPhase(GamePhase)` | — | — | ✅ |
| 호출 | `StartProduction()` | — | — | ✅ |
| 호출 | `AdvanceDay()` | — | — | ✅ |
| 호출 | `ForceEndProduction()` | — | — | ✅ (디버그) |

**Dev1이 하지 않는 것**

- `orderWindow`·`shopWindow` 등 **타 역할 UI** GameObject 참조·토글
- 페이즈 외 **의뢰·상점 로직**

**Mock (Lead / Dev2)**

```csharp
// 테스트 씬: 페이즈만 수동 설정
GameSessionState.Instance.SetPhase(GamePhase.Prepare);
```

---

## PlayerInventory (Dev1 API · `Assets/Scripts/Player/`)

코드 위치는 `Player/`이나 **API 소유·변경은 Dev1**이 담당한다.

| 구분 | 멤버 | Lead | Dev2 | Dev1 |
|------|------|:----:|:----:|:----:|
| 읽기 | `GetCount(itemId)` | ✅ | ✅ | ✅ |
| 읽기 | `Machines`, `GetInInventoryCount()` *(추가 예정)* | ✅ | ✅ | ✅ |
| 이벤트 | `OnItemsChanged` *(추가 예정)* | — | 구독 | 발행 |
| 이벤트 | `OnMachinesChanged` | 구독 | — | 발행 |
| 쓰기 | `Add(ItemEntry)` | ✅ 생산 산출 | ✅ 보상 | ✅ |
| 쓰기 | `Remove(itemId, amount)` | — | ✅ 납품 | ✅ |
| 쓰기 | `AddMachine(def)` | — | — | ✅ NewGame |
| 쓰기 | `TryRemoveMachine(instanceId)` | ✅ 배치 | — | — |
| 쓰기 | `ReturnMachine(entry)` *(추가 예정)* | ✅ 회수 | — | — |

**Lead 쓰기 범위**: 생산 완료·WIP 환원·포트 회수로 **아이템 증감**, 배치/회수로 **기계 목록**만 변경.

**Mock (Dev2)**

```csharp
inventory.Add(new ItemEntry { item = contractItemIronOre, count = 5 });
```

---

## QuestManager (Dev2 · `Assets/Scripts/Quest/`)

| 구분 | 멤버 | Dev1 | Dev2 | Lead |
|------|------|:----:|:----:|:----:|
| 읽기 | `ActiveQuests` (=`currentQuests` 래핑) | ✅ | ✅ | — |
| 읽기 | `AvailableQuestsToday` | — | ✅ | — |
| 이벤트 | `OnQuestAccepted(int activeCount)` *(추가 예정)* | 구독 | 발행 | — |
| 이벤트 | `OnQuestsChanged` *(추가 예정)* | 구독 | 발행 | — |
| 호출 | `TryAccept(string questId)` *(기존 `acceptQuest` 대체)* | — | ✅ | — |
| 호출 | `RefreshAvailableQuests()` | — | ✅ | — |
| 호출 | `EvaluateDelivery()` *(W3)* | — | ✅ | — |

**레거시** (W2 통합 시 제거): `acceptQuest(Quest)` 직접 노출 — 내부에서 `TryAccept`로 위임.

**Mock (Dev1 HUD)**

```csharp
// Dev2 미완 시
displayText = "의뢰: 0/3";
// 통합 시
QuestManager.Instance.OnQuestAccepted += count => displayText = $"의뢰: {count}/3";
```

---

## IFactoryProduction (Lead · `Assets/Scripts/Production/`)

Dev1 W3 연동. W2는 스텁만 있어도 됨.

```csharp
public interface IFactoryProduction
{
    void StartTick();
    void StopTick();
    bool IsRunning { get; }
}
```

| 호출 | 시점 | 호출자 |
|------|------|--------|
| `StartTick()` | `Phase == Production` 진입 | Dev1 (`OnPhaseChanged` 구독 또는 `StartProduction` 내부) |
| `StopTick()` | `Phase == Settlement` 진입 | Dev1 |

**Mock (Dev1 W2)**: `NullFactoryProduction` — `Debug.Log`만 출력.

---

## IFactorySave (Lead · W3)

Dev1 세이브의 `factory` 필드. W2~W3 평일 Dev1은 `null` 저장 허용.

```csharp
public interface IFactorySave
{
    string ExportJson();
    void ImportJson(string json);
}
```

| 호출 | 담당 |
|------|------|
| `ExportJson` / `ImportJson` | Lead 구현 · Dev1 `SaveLoad`에서 호출 |

---

## StoryEventBus (Lead 발행 · Dev1 구독 · W3)

```csharp
public static class StoryEventBus
{
    public static event Action<string> OnStoryEvent;
    public static void Raise(string eventId) => OnStoryEvent?.Invoke(eventId);
    public static void RaiseMock(string eventId) => OnStoryEvent?.Invoke(eventId);
}
```

Lead만 `Raise` 호출. Dev1은 구독·대화 UI만.

---

## IQuestSaveProvider (Dev2 · W3)

Dev1 `SaveData.quests[]` 직렬화.

```csharp
public interface IQuestSaveProvider
{
    AcceptedQuestSave[] Export();
    void Import(AcceptedQuestSave[] data);
}
```

---

## 정적 계약 에셋 (팀 공유 · W1 고정)

역할 간 **SO 참조 의존**을 없애기 위해, 아래 에셋은 **Week 1에 Lead가 생성**하고 이후 **id 변경 금지**.

| 경로 | 소유 | 소비자 | 최소 목록 |
|------|------|--------|-----------|
| `Assets/Data/Contracts/Items/` | Lead 생성·유지 | Dev2 Quest SO | `iron_ore`, `iron`, `iron_rod`, `iron_plate` |
| `Assets/Data/Contracts/Machines/` | Lead | Dev1 NewGame | `채굴기_1`, `용광로_1`, `제작기_1`, `창고_1` |
| `Assets/Art/UI/Portraits/` | Art | Dev2, Dev1 | placeholder PNG 허용 |
| `Assets/Art/Characters/Protagonist/` | Art | Lead | placeholder 시트 허용 |

Dev2는 Quest SO 작성 시 **Contracts Items만** 참조. Lead 전용 Item 에셋 경로 직접 참조 금지.

---

## UI·씬 소유

| 대상 | 소유 | 비고 |
|------|------|------|
| Factory 씬 | Lead | 맵·기계·배치 |
| Settlement 씬 | Dev1 | 씬 로드·전환 |
| Title 씬 | Dev1 | W2 껍데기 · W3 세이브 연동 |
| 인벤·글로벌 HUD | Dev1 | `GameFlow/` |
| 의뢰 수락·결산 목록 UI | Dev2 | `Quest/` — **자체** `OnPhaseChanged` 구독으로 표시/숨김 |
| 상점·구매 UI | Dev2 | W3 — Dev1은 씬만 제공 |

**Dev2 UI 표시 규칙** (Dev1 개입 없음)

```csharp
GameSessionState.Instance.OnPhaseChanged += phase => {
    questPanel.SetActive(phase == GamePhase.Prepare);
    settlementQuestList.SetActive(phase == GamePhase.Settlement);
};
```

---

## 세이브 JSON 스키마 (W2 동결 · Dev1 소유 DTO)

Dev1이 **DTO 필드만** 정의 (**W2 금요일 동결**). Dev2가 W3에 `quests` 직렬화·복원 구현.  
전체 스키마: [02-data-structure.md § 세이브 데이터](../02-data-structure.md#세이브-데이터)

| 필드 | 직렬화 담당 | 비고 |
|------|-------------|------|
| `day`, `phase`, `gold`, `reputation` | Dev1 | |
| `inventoryItems`, `machines`, `factory` | Dev1 + Lead 데이터 | |
| `quests[]` | **Dev2** | `AcceptedQuestSave` — Dev1은 필드 추가만 |
| `version` | Dev1 | 스키마 변경 시 +1 |

W2 종료 시 `SaveData` 클래스 시그니처 **동결**. Dev2는 `quests` 외 필드 수정 금지.

---

## 폴더·브랜치 규칙

| 역할 | 수정 가능 | 읽기만 |
|------|-----------|--------|
| Dev1 | `GameFlow/`, `Player/PlayerInventory.cs` | `Quest/`, `Placement/`, `Production/`, `Machine/` |
| Dev2 | `Quest/` | `GameFlow/`, `Player/` |
| Lead | `Placement/`, `Production/`, `Machine/`, `Grid/`, `Item/`, Factory 씬 | `GameFlow/`, `Quest/` |
| Art | `Assets/Art/` | — |

---

## 관련 문서

- [parallel-roadmap.md](./parallel-roadmap.md)
- [week2/team-integration.md](./week2/team-integration.md)
- [02-data-structure.md](../02-data-structure.md)
