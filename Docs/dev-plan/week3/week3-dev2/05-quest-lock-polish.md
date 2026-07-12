# 의뢰 lock·결산 polish

> **역할**: Dev2 · **Week**: 3 · **Issue**: 05  
> **선행**: W2 [05-delivery-rewards](../../week2/week2-dev2/05-delivery-rewards.md) · [04-settlement-quest-list](../../week2/week2-dev2/04-settlement-quest-list.md)  
> **후속**: [week3-dev1/06-team-integration](../week3-dev1/06-team-integration.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) · QuestManager API

---

## 1. 이 작업물

튜토리얼부터 MVP 클리어까지 필요한 **의뢰 SO·보상 수치를 확정(lock)** 하고, 결산 UI를 **플레이 가능한 수준**으로 다듬는다.  
「lock」= 기획·밸런스 **확정 후 SO 반영**. 게임 내 해금(unlock)과 다름.

**코드 위치**: `Assets/Scripts/Quest/`  
**에셋 위치**: `Assets/Data/Quests/` (또는 W2에서 사용 중인 경로 유지)

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 튜토~클리어 의뢰 SO 전체 확정 | 신규 던전·의뢰 ID 체계 변경 |
| 골드·명성·아이템 보상 수치 lock | Lead 레시피·기계 SO (Lead [05-recipe-data-lock](../week3-lead/05-recipe-data-lock.md)) |
| 결산 UI: n/m, 빨간 버튼, 보상 요약 | Dev1 타이틀·세이브 UI |
| 의뢰 풀 명성 threshold lock | 상점 가격표 전체 밸런스 (02-economy와 분담) |

---

## 3. 현재 코드 상태

| 파일 | 상태 |
|------|------|
| `Quest.cs` | `title`, `requiredItems`, `rewards`, `deadlineDays`, `currentleftDeadlineDays` |
| `QuestManager.cs` | `acceptQuest`, `progressQuest`, `finishQuest`, `givePlayerReward` — **골드/명성은 ItemEntry만** 지급 중일 수 있음 |
| `GameSessionState` | `gold`, `reputation` 필드 존재 — 보상 시 **반드시 여기에도 반영** 필요 |

**갭**: `isMandatory` 필드 없음 → W3에 `Quest`에 `bool isMandatory` 추가 또는 별도 Story Quest 목록 SO.

---

## 4. 산출물

| 산출물 | 경로 |
|--------|------|
| Quest SO (튜토~클리어) | `Assets/Data/Quests/Quest_00100001.asset` 등 |
| 결산 polish | `SettlementQuestListUI.cs` (또는 W2 UI 확장) |
| (선택) Quest pool config | `QuestPoolConfig.asset` — 명성 threshold |
| lock 문서 1장 | 팀 채널 또는 `Docs/` — **수치 표** (코드 외부 합의용) |

---

## 5. 데이터 스펙

### 5-1. 의뢰 ID 규칙

- 8자리: `DDDQQQQQ` — MVP 던전 `001`
- 상세: [01-core-loop.md](../../../01-core-loop.md) § 의뢰

### 5-2. 필수(스토리) 의뢰 골격

[04-story.md](../../../04-story.md) 기준. **최소 1종은 아래 수치로 확정**, 나머지는 표 채운 뒤 lock.

| questId | isMandatory | 납기 | 요구 (Contracts itemId) | 보상 (lock 대상) |
|---------|-------------|------|-------------------------|------------------|
| `00100001` | **true** | 당일 (`deadlineDays=0` 또는 당일 규칙) | iron_ore×5, iron×5, iron_plate×1, iron_rod×1 | 채굴기_1×1, gold **(숫자 확정)**, reputation **(숫자 확정)** |
| `00100002` | true | *(기획)* | *(마법 라인)* | 마나 계열 해금 연동 |
| … | | | | |
| 마지막 | true | *(기획)* | | **엔딩** 트리거 |

**00100001 확정 규칙** ([07-planning-quest-story](../../week1/week1-lead/07-planning-quest-story.md)):

- 보상 종류: **채굴기·골드·명성만** (다른 아이템 보상 없음)
- 수락: 필수
- Contracts Items만 참조: `Assets/Data/Contracts/Items/`

### 5-3. 보상 지급 규칙

| 보상 타입 | API |
|-----------|-----|
| 아이템·기계 | `PlayerInventory.Add(ItemEntry)` |
| 골드 | `GameSessionState` — **공개 setter 또는 `AddGold(int)` 추가** (Dev1과 합의) |
| 명성 | `GameSessionState` — **`AddReputation(int)`** |

`givePlayerReward`에서 rewards ItemEntry와 별도로 gold/reputation 필드를 Quest SO에 두는 방식 권장:

```csharp
// Quest.cs 추가 예시
public int rewardGold;
public int rewardReputation;
public bool isMandatory;
```

### 5-4. 미납·게임오버 (연동)

| 조건 | 처리 | Issue |
|------|------|-------|
| 일반 의뢰 미납 | 명성 보상 **50%** | [03-gameover-penalty](./03-gameover-penalty.md) |
| 필수 의뢰 실패 | 게임오버 → Title | 동일 |

### 5-5. 의뢰 풀 threshold (lock)

| 누적 명성 | 해금 내용 (예시) |
|-----------|------------------|
| 0 | 튜토 풀만 |
| 50 | 일반 의뢰 1단계 |
| 150 | 일반 의뢰 2단계 |
| … | [02-economy-unlock](./02-economy-unlock.md)와 표 통일 |

---

## 6. 결산 UI polish

W2 [04-settlement-quest-list](../../week2/week2-dev2/04-settlement-quest-list.md) 위에 추가.

| UI 요소 | 동작 |
|---------|------|
| 진행도 | 각 요구품 `보유 n / 요구 m` 텍스트 |
| 납품 버튼 | 전부 충족 시만 활성 |
| 부족 시 | 버튼 **빨간색** + 툴팁 「○○ 부족」 |
| 보상 요약 | 납품 성공 직후 골드·명성·아이템 목록 표시 |
| 멀티데이 | `D-n` 표시 — [01-multi-day-deadline](./01-multi-day-deadline.md) |

**표시 규칙** (Dev1 개입 없음):

```csharp
GameSessionState.Instance.OnPhaseChanged += phase => {
    settlementPanel.SetActive(phase == GamePhase.Settlement);
};
```

---

## 7. 구현 단계

### 7-1. SO lock

- [ ] 스토리 의뢰 목록을 [04-story.md](../../../04-story.md) 표와 대조
- [ ] 각 Quest SO: `questId`(에셋명 또는 필드), `requiredItems`, `deadlineDays`, `isMandatory`
- [ ] `00100001` 골드·명성 **숫자 팀 합의 후** SO 입력
- [ ] Contracts Items id만 참조 (`iron_ore`, `iron`, `iron_plate`, `iron_rod`)
- [ ] 채굴기 보상: `Assets/Data/Contracts/Machines/채굴기_1` 참조

### 7-2. 보상 로직

- [ ] `finishQuest` / `givePlayerReward`에서 gold·reputation 반영
- [ ] 미납 시 50% 명성 — `EvaluateDelivery` 또는 결산 버튼 실패 분기

### 7-3. 결산 UI

- [ ] n/m 표시 — `PlayerInventory.GetCount(itemId)`
- [ ] 부족 시 버튼 비활성 + 빨간 스타일
- [ ] 보상 요약 패널 또는 토스트

### 7-4. 풀 threshold

- [ ] `RefreshAvailableQuests()`에서 `GameSessionState.reputation` 기준 필터
- [ ] threshold 수치 lock 후 변경 시 팀 공지

---

## 8. 독립 개발 (Mock)

Lead 생산 없이 검증:

```csharp
// 테스트 씬: 결산 페이즈 강제
GameSessionState.Instance.SetPhase(GamePhase.Settlement);
inventory.Add(new ItemEntry { item = ironOreContract, count = 5 });
// ... 요구품 수동 Add 후 납품 버튼 클릭
```

- [ ] `00100001` 전량 보유 → 납품 → 골드·명성·채굴기 증가
- [ ] 1개 부족 → 버튼 비활성·빨간 표시

---

## 9. 완료 기준

- [ ] 튜토~클리어 의뢰 SO가 **id·요구·납기·보상·필수** 모두 채워짐
- [ ] `00100001` 수치 lock — 팀 채널에 표 공유
- [ ] 결산에서 n/m·부족 빨간 버튼·보상 요약 동작
- [ ] 의뢰 풀 threshold 수치 lock
- [ ] 클리어 의뢰 1회 **플레이스루** 납품·보상 수치가 기획 표와 일치

---

## 10. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md) — 의뢰·미납 50%
- [04-story.md](../../../04-story.md) — 스토리 의뢰·이벤트
- [02-data-structure.md](../../../02-data-structure.md) — questId·AcceptedQuestSave
- [04-quest-serialize](./04-quest-serialize.md) — 세이브 연동
