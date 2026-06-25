## 새 게임·인벤토리 초기화

### 이 작업물

**새 게임** 버튼(또는 디버그) 한 번으로 `01-core-loop.md`에 정한 **시작 상태**로 세션을 채운다.  
플레이어 **위치**는 Lead [02-player-movement](../week1-lead/02-player-movement.md)가 처리.

**코드**: `Assets/Scripts/` (예: `InventoryState.cs`, `GameBootstrap.NewGame()`)

### 시작 상태 (반드시 이 값)

| 항목 | 값 |
|------|-----|
| 일차 | 1 |
| 페이즈 | Prepare |
| 골드 | 0 |
| 명성 | 0 |
| 아이템 | 없음 (빈 Dictionary) |
| 기계 | 아래 3개, 전부 **인벤토리** |

### 시작 기계 3개

| machineDefId | machineTypeId | placement |
|--------------|---------------|-----------|
| `채굴기_1` | 채굴기 | `InInventory` |
| `용광로_1` | 용광로 | `InInventory` |
| `모루_1` | 모루 | `InInventory` |

각 인스턴스:

- [ ] `instanceId` = 새 **GUID** 문자열 (`02-data-structure.md`)
- [ ] `gridX/gridY` = 없음 (맵 미배치)
- [ ] `machineDefId` 위 표와 동일

### `InventoryState` 구조

```csharp
class InventoryState {
    Dictionary<string, int> items;           // itemId → count
    List<MachineInstanceState> machines;
}

enum MachinePlacement { InInventory, PlacedOnMap }

class MachineInstanceState {
    string instanceId;
    string machineDefId;
    MachinePlacement placement;
    int gridX, gridY;  // PlacedOnMap일 때만
}
```

### `NewGame()` 순서

1. [ ] `GameSessionState` 모든 필드 초기화 (day, gold, rep, phase, factory 빈 맵, quests 빈 목록)
2. [ ] 위 기계 3개 `machines`에 추가
3. [ ] `productionEndTime` 클리어
4. [ ] **이벤트 발행** — Lead 스폰용  
   예: `OnNewGame?.Invoke()` 또는 `PlayerSpawner.SpawnAtCenter()`
5. [ ] [05-scene-flow](./05-scene-flow.md)에서 Factory 씬 로드 트리거

### 완료 기준

- [ ] `NewGame()` 호출 후 gold=0, reputation=0, day=1, phase=Prepare
- [ ] `machines.Count == 3`, 전부 `InInventory`
- [ ] 맵에 `PlacedOnMap` 기계 0개
- [ ] Lead `OnNewGame` 구독 시 플레이어가 맵 중앙에 스폰됨 (연동 후)
