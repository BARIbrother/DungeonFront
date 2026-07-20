# 의뢰 세이브 직렬화

**역할**: Dev2 · **Week**: 3 · **Issue**: 04  
**선행**: W2 `SaveData` DTO 동결

## 1. 이 작업물

`SaveData.quests[]` Export/Import로 수락 중 의뢰·납기 상태를 복원한다.

**코드**: `QuestSaveProvider.cs` — `IQuestSaveProvider`

## 2. DTO

```csharp
[Serializable]
public class AcceptedQuestSave {
    public string questId;
    public int daysRemaining;   // currentleftDeadlineDays
    public int acceptedDay;
}

public interface IQuestSaveProvider {
    AcceptedQuestSave[] Export();
    void Import(AcceptedQuestSave[] data);
}
```

`SaveData`의 quests 외 필드는 수정하지 않음. DTO 변경 시 `version++`.

런타임 인스턴스에 `sourceQuestId` 보존 (`acceptQuest` 시 설정).

## 3. Export / Import

**Export**: `currentQuests` → `questId`, `daysRemaining`, `acceptedDay`

**Import**: ClearActive → SO 조회 → 인스턴스 생성 → 일수·acceptedDay 복원 → `currentQuests`  
없는 questId는 스킵 + 경고. null/빈 배열 크래시 없음.  
Import 후 `OnQuestsChanged` 발행.

## 4. 완료 기준

- [ ] `IQuestSaveProvider` Export/Import 구현
- [ ] 수락 1건 저장→로드 → `currentQuests.Count == 1`
- [ ] `daysRemaining` · `acceptedDay` 유지
- [ ] 없는 questId 스킵 · null Import 안전
- [ ] `SaveData` quests 외 필드 미수정
