## 의뢰 데이터 골격

### 이 작업물

의뢰 **정의(SO)** 와 **런타임 수락 목록**의 빈 껍데기. Week 1에는 **수락 UI·납품 없음**.  
`00100001` 에셋·수치는 Lead [07-planning-quest-story](../week1-lead/07-planning-quest-story.md) 확정 후 채움.

**스키마**: `02-data-structure.md` Quest 섹션  
**코드**: `Assets/Scripts/`

### `QuestDefinition` : ScriptableObject (필드)

| 필드 | 타입 | 설명 |
|------|------|------|
| `id` | string | `00100001` 형식 |
| `dungeonId` | string | `001` |
| `displayName` | string | UI용 |
| `isMandatory` | bool | 필수 의뢰 |
| `dueDays` | int | 납기 (0 = 당일) |
| `requirements` | ItemGroup | 요구 물품 |
| `rewardsGold` | int | |
| `rewardsReputation` | int | |
| `rewardMachineDefIds` | string[] | 예: 채굴기 지급 |

Week 1: **placeholder SO 0~1개** (`00100001` 초안 있으면 1개 생성)

### `QuestDatabase`

- [ ] `QuestDefinition Get(string questId)`
- [ ] 에셋 목록 또는 Resources 폴더 등록

### 런타임 `AcceptedQuestState` (세션)

```csharp
class AcceptedQuestState {
    string questId;
    int acceptedDay;
    // dueDay 등 — dueDays 기반 계산
}
```

- [ ] `GameSessionState.quests` = `List<AcceptedQuestState>`
- [ ] `NewGame()` → **빈 리스트**

### 완료 기준

- [ ] `QuestDefinition` 클래스 컴파일
- [ ] `QuestDatabase.Get("00100001")` 테스트 가능 (SO 있으면 non-null)
- [ ] `NewGame()` 후 `quests.Count == 0`
- [ ] (선택) 기획 수치 확정 시 `00100001` SO 에셋 1개 생성
