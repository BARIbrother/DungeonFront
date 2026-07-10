## 의뢰 세이브 직렬화

### 이 작업물

Dev1 [SaveData](../../week2/week2-dev1/04-save-dto-freeze.md)의 `quests[]` 필드 **직렬화·복원**.

**인터페이스** (Dev1 제공 훅):

```csharp
public interface IQuestSaveProvider {
    AcceptedQuestSave[] Export();
    void Import(AcceptedQuestSave[] data);
}
```

### 필드 (`AcceptedQuestSave`)

- `questId`, `daysRemaining`, `acceptedDay` (최소)

### 독립 개발

- [ ] Dev1 `SaveLoad` 없이 `Export`/`Import` 단위 테스트
- [ ] Lead `factory` 필드와 **무관**

### 완료 기준

- [ ] 수락 의뢰 1건 저장 → 로드 후 `ActiveQuests` 복원
- [ ] Dev1 [04-save-load](../week3-dev1/04-save-load.md)와 금요일 통합
