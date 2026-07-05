## 결산 의뢰 목록 (1차)

### 이 작업물

[week1 결산 스텁](../../week1/week1-dev/04-settlement-stub.md) 확장 — **수락한 의뢰 목록** 표시. 납품·보상은 후속.

**코드**: `Assets/Scripts/` (예: `SettlementQuestListUI.cs`)  
**데이터**: `QuestManager.currentQuests` · `Quest.requiredItems`

### 표시 (의뢰당 1행)

| 열 | 내용 |
|----|------|
| 이름 | `Quest.title` |
| 납기 | `currentleftDeadlineDays` 일 남음 |
| 진행 | 보유 **n** / 요구 **m** (`requiredItems.entries`) |
| 납품 | 버튼 **비활성** 또는 placeholder |

- [ ] `PlayerInventory`와 `requiredItems` 비교해 n/m 계산

### 없어도 되는 것 (Week 3~)

- `progressQuest` 연동·보상·미납·게임오버

### 완료 기준

- [ ] 수락 후 결산 화면에 의뢰 1행 이상 표시
- [ ] n/m이 인벤과 일치
