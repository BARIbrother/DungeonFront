# JSON 세이브·로드

**역할**: Dev1 · **Week**: 3 · **Issue**: 04  
**선행**: W2 `SaveData` DTO 동결 · Title 셸

## 1. 이 작업물

동결된 `SaveData`로 JSON 저장·불러오기를 구현하고, Title 슬롯 1~3과 연동한다.

**코드**: `SaveLoad.cs`, `SaveData.cs` (`Assets/Scripts/GameFlow/`)  
**경로**: `Application.persistentDataPath/save_slot_{0-2}.json`

## 2. SaveData 스키마 (동결)

| 필드 | 타입 (예) | 담당 |
|------|-----------|------|
| `version` | int | Dev1 |
| `day` | int | Dev1 |
| `phase` | string/enum | Dev1 |
| `gold` | int | Dev1 |
| `reputation` | int | Dev1 |
| `inventoryItems` | `ItemSave[]` (`itemId`, `count`) | Dev1 |
| `machines` | `MachineInventorySave[]` (`machineDefId`, `instanceId`) | Dev1 |
| `factory` | string (JSON) | `IFactorySave.ExportJson` / `ImportJson` |
| `quests` | `AcceptedQuestSave[]` | `IQuestSaveProvider` |

W2: `factory = null`, `quests = []` 허용. 시그니처 변경 시 `version++`.

```csharp
public interface IFactorySave {
    string ExportJson();
    void ImportJson(string json);
}
```

## 3. 저장·로드 흐름

**Save**: session(day/phase/gold/rep) → inventory → `IFactorySave.ExportJson` → `IQuestSaveProvider.Export` → `JsonUtility.ToJson` → 파일

**Load**: 파일 → SaveData → session·inventory 복원 → Factory 씬 후 `ImportJson` → quests Import → `OnPhaseChanged`

## 4. Title · autosave

| 버튼 | 동작 |
|------|------|
| 새 게임 | `NewGame()` — 덮어쓰기 안 함 |
| 불러오기 | 슬롯 → `SaveLoad.Load` → Factory |
| 슬롯 UI | 빈 슬롯 / 일차 표시 |

autosave: 일차 증가 후 · 필수 의뢰 클리어 후. 슬롯 `autosaveSlot` (기본 0).

## 5. 완료 기준

- [ ] 슬롯 1~3 수동 저장·불러오기
- [ ] day·gold·인벤 round-trip
- [ ] `factory` / `quests` null-safe (미구현 시 skip)
- [ ] Load 시 phase에 맞는 씬 · `OnPhaseChanged`
- [ ] 손상 JSON 시 크래시 없이 안내
