## 결산 의뢰 목록 (1차)

> **담당**: Dev2 · `Assets/Scripts/Quest/`  
> **권장 순서**: Dev2 **4번째**

### 이 작업물

결산 단계 **수락한 의뢰 목록** 표시. 납품·보상은 [05-delivery-rewards](./05-delivery-rewards.md).

**코드**: `Assets/Scripts/Quest/` (예: `SettlementQuestListUI.cs`)  
**데이터**: `QuestManager.ActiveQuests` · `Quest.requiredItems`

### 표시 (의뢰당 1행)

| 열 | 내용 |
|----|------|
| 이름 | `Quest.title` |
| 납기 | `currentleftDeadlineDays` 일 남음 |
| 진행 | 보유 **n** / 요구 **m** |
| 납품 | 버튼 **비활성** 또는 placeholder |

- [ ] `PlayerInventory.GetCount(itemId)`로 n/m 계산 (**읽기만**)

### UI 표시 (Dev1 비의존)

- [ ] `OnPhaseChanged` 구독 — `Settlement`일 때만 목록 표시
- [ ] Settlement 씬 프리팹은 Dev2 소유; Dev1은 씬 로드만 담당

### 없어도 되는 것 (Week 3~)

- `EvaluateDelivery` · 보상·미납·게임오버

### 독립 개발 (Mock)

- [ ] Contracts Items로 테스트 인벤 주입 후 n/m 검증

### 완료 기준

- [ ] 수락 후 결산 화면에 의뢰 1행 이상 표시
- [ ] n/m이 인벤과 일치
