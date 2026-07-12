# JSON 세이브·로드

> **역할**: Dev1 · **Week**: 3 · **Issue**: 04  
> **선행**: W2 [04-save-dto-freeze](../../week2/week2-dev1/04-save-dto-freeze.md) · [05-title-shell](../../week2/week2-dev1/05-title-shell.md)  
> **연동**: Lead `IFactorySave` · Dev2 [04-quest-serialize](../week3-dev2/04-quest-serialize.md)  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

W2에 동결한 `SaveData` DTO로 **JSON 저장·불러오기**를 구현하고, Title 슬롯 1~3과 연동한다.

**코드**: `Assets/Scripts/GameFlow/SaveLoad.cs`, `SaveData.cs`  
**경로**: `Application.persistentDataPath/save_slot_{0-2}.json`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 슬롯 1~3 수동 저장·로드 | 클라우드 세이브 |
| autosave (선택 슬롯) | 세이브 암호화 |
| Dev1 필드 직렬화 | Dev2 `quests[]` **구현** (Dev2 담당) |
| Lead `factory` 필드 호출 | Quest SO 에셋 작성 |

---

## 3. SaveData 스키마 (동결)

[02-data-structure.md](../../../02-data-structure.md#세이브-데이터) · W2 DTO:

| 필드 | 타입 (예) | 직렬화 담당 |
|------|-----------|-------------|
| `version` | int | Dev1 |
| `day` | int | Dev1 |
| `phase` | string/enum | Dev1 |
| `gold` | int | Dev1 |
| `reputation` | int | Dev1 |
| `inventoryItems` | `ItemSave[]` | Dev1 — `itemId`, `count` |
| `machines` | `MachineInventorySave[]` | Dev1 — `machineDefId`, `instanceId` |
| `factory` | string (JSON) | Dev1 호출 → Lead `IFactorySave.ExportJson()` |
| `quests` | `AcceptedQuestSave[]` | **Dev2** `IQuestSaveProvider` |

```csharp
// dev-contract
public interface IFactorySave {
    string ExportJson();
    void ImportJson(string json);
}
```

W2: `factory = null`, `quests = []` 허용.

---

## 4. 저장·로드 흐름

### Save

```
1. GameSessionState → day, phase, gold, reputation
2. PlayerInventory → inventoryItems, machines (인벤에 있는 기계만)
3. IFactorySave.ExportJson() → factory (맵 배치·포트·WIP)
4. IQuestSaveProvider.Export() → quests
5. JsonUtility.ToJson(saveData) → 파일 쓰기
```

### Load

```
1. 파일 읽기 → SaveData
2. GameSessionState 복원 + NewGame 대체
3. PlayerInventory Import
4. Factory 씬 로드 후 IFactorySave.ImportJson(factory)
5. IQuestSaveProvider.Import(quests)
6. OnPhaseChanged 발행 — UI 동기화
```

---

## 5. Title UI 연동

W2 [05-title-shell](../../week2/week2-dev1/05-title-shell.md):

| 버튼 | 동작 |
|------|------|
| 새 게임 | `NewGame()` — 세이브 덮어쓰기 안 함 |
| 불러오기 | 슬롯 선택 → `SaveLoad.Load(slot)` → Factory 씬 |
| (슬롯 UI) | 빈 슬롯 / 일차·day 표시 |

**autosave** ([01-core-loop.md](../../../01-core-loop.md)):

- 트리거: 일차 증가 후, **필수 의뢰 클리어** 후
- 슬롯: 설정 `autosaveSlot` (기본 0)

---

## 6. 구현 단계

- [ ] `SaveLoad.cs` — `Save(int slot)`, `Load(int slot)`, `HasSave(slot)`
- [ ] `SaveData.cs` — W2 시그니처 **변경 시 version++** + 팀 공지
- [ ] Title 버튼 연결
- [ ] Load 시 `phase`에 맞는 씬 (Factory / Settlement) 로드
- [ ] `IFactorySave` null-safe — Lead 미구현 시 skip
- [ ] `IQuestSaveProvider` — Dev2 구현체 Find 또는 DI

---

## 7. 검증 시나리오

- [ ] 슬롯 1 저장 → 게임 종료 → 불러오기 → day·gold·인벤 동일
- [ ] 맵에 기계 배치·포트 아이템 → 저장 → 로드 후 배치·포트 복원 (Lead `IFactorySave` 완료 시)
- [ ] 수락 의뢰 1건 → 저장 → 로드 후 `currentQuests` 복원 (Dev2 완료 시)
- [ ] 손상 JSON — 에러 메시지, 크래시 없음

---

## 8. 완료 기준

- [ ] Title 슬롯 1~3 저장·불러오기 동작
- [ ] §7 시나리오 (factory/quests는 연동 완료 시)
- [ ] `SaveData` 시그니처 W2 동결 유지 — `quests` 외 필드 Dev2 수정 금지

---

## 9. 관련 문서

- [04-quest-serialize](../week3-dev2/04-quest-serialize.md)
- [week3-lead/07-mvp-integration](../week3-lead/07-mvp-integration.md)
- [02-data-structure.md](../../../02-data-structure.md)
