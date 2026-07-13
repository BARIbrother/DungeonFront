# 의뢰 세이브 직렬화

> **역할**: Dev2 · **Week**: 3 · **Issue**: 04  
> **선행**: W2 [04-save-dto-freeze](../../week2/week2-dev1/04-save-dto-freeze.md)  
> **연동**: Dev1 [04-save-load](../week3-dev1/04-save-load.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) — `IQuestSaveProvider`

---

## 1. 이 작업물

`SaveData.quests[]`를 **Export/Import**하여 수락 중인 의뢰·납기 상태를 복원한다.

**코드**: `Assets/Scripts/Quest/QuestSaveProvider.cs` — `IQuestSaveProvider` 구현

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| `quests[]` 직렬화·복원 | `SaveData` 다른 필드 수정 |
| `QuestManager`와 연동 | Available 풀 오늘 목록 (선택 재생성) |

---

## 3. DTO

[02-data-structure.md](../../../02-data-structure.md):

```csharp
[Serializable]
public class AcceptedQuestSave {
    public string questId;
    public int daysRemaining;   // = currentleftDeadlineDays
    public int acceptedDay;
}

public interface IQuestSaveProvider {
    AcceptedQuestSave[] Export();
    void Import(AcceptedQuestSave[] data);
}
```

- `questId` — SO 조회 키 (8자리 문자열)
- Dev1은 DTO **필드 추가만** — 시그니처 변경 시 `version++`

---

## 4. Export

```csharp
public AcceptedQuestSave[] Export() {
  return questManager.currentQuests.Select(q => new AcceptedQuestSave {
    questId = q.name 또는 q.questId 필드, // SO에 id 필드 권장
    daysRemaining = q.currentleftDeadlineDays,
    acceptedDay = q.acceptedDay,
  }).ToArray();
}
```

**주의**: 런타임 `Quest` 인스턴스는 `ScriptableObject.CreateInstance` — **원본 SO id** 보존 필드 필요:

```csharp
// Quest 런타임 인스턴스에
public string sourceQuestId;
```

---

## 5. Import

```csharp
public void Import(AcceptedQuestSave[] data) {
    questManager.ClearActive(); // currentQuests 비우기
    foreach (var s in data ?? Array.Empty<AcceptedQuestSave>()) {
        var template = questDatabase.Get(s.questId);
        var instance = CreateQuestInstance(template);
        instance.currentleftDeadlineDays = s.daysRemaining;
        instance.acceptedDay = s.acceptedDay;
        questManager.currentQuests.Add(instance);
    }
}
```

- `QuestDatabase` — `Assets/Data/Quests/`에서 id 로드
- Import 후 Dev1 HUD — `OnQuestsChanged` 발행

---

## 6. Dev1 연동

`SaveLoad.Save()`:

```csharp
saveData.quests = questSaveProvider.Export();
```

`SaveLoad.Load()`:

```csharp
questSaveProvider.Import(saveData.quests);
```

`QuestSaveProvider` — 씬에 1개 또는 `QuestManager` 동일 GameObject.

---

## 7. 구현 단계

- [ ] `Quest.sourceQuestId` — `acceptQuest` 시 설정
- [ ] `QuestDatabase.Get(questId)` — null 시 로그
- [ ] `QuestSaveProvider` — Export/Import
- [ ] `QuestManager.OnQuestsChanged` 이벤트 (W3 contract)
- [ ] Dev1 SaveLoad 연결
- [ ] 빈 배열 / null Import — 크래시 없음

---

## 8. 검증 시나리오

- [ ] 의뢰 1건 수락 → 저장 → Title → 로드 → `currentQuests.Count == 1`
- [ ] `daysRemaining` 값 유지
- [ ] `00100001` 필수 의뢰 — 요구품 상태는 인벤 세이브와 **합쳐** 납품 가능
- [ ] 존재하지 않는 questId — 스킵 + 경고

---

## 9. 완료 기준

- [ ] §8 통과
- [ ] [04-save-load](../week3-dev1/04-save-load.md)와 연동 머지
- [ ] `SaveData` quests 외 필드 **미수정**

---

## 10. 관련 문서

- [01-multi-day-deadline](./01-multi-day-deadline.md)
- [dev-contract.md](../../dev-contract.md)
