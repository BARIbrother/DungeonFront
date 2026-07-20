# 의뢰 lock·결산 polish

**역할**: Dev2 · **Week**: 3 · **Issue**: 05  
**선행**: W2 납품·결산 목록

## 1. 이 작업물

튜토~클리어용 의뢰 SO·보상 수치를 lock하고, 결산 UI를 플레이 가능 수준으로 다듬는다.  
「lock」= 기획 확정 후 SO 반영.

**코드**: `Assets/Scripts/Quest/`  
**에셋**: `Assets/Data/Quests/`

## 2. 현재 갭

| 항목 | 상태 |
|------|------|
| `Quest` | title, requiredItems, rewards, deadlineDays, currentleftDeadlineDays |
| 보상 | ItemEntry 위주 — gold/reputation을 `GameSessionState`에 반영 필요 |
| `isMandatory` | 추가 필요 |

```csharp
public int rewardGold;
public int rewardReputation;
public bool isMandatory;
```

## 3. `00100001` (확정 골격)

| 항목 | 내용 |
|------|------|
| isMandatory | true |
| 납기 | 당일 (`deadlineDays=0`) |
| 요구 | `iron_ore`×5, `iron`×5, `iron_plate`×1, `iron_rod`×1 |
| 보상 | `채굴기_1`×1, 골드 *(숫자 미정)*, 명성 *(숫자 미정)* — 이 세 가지만 |
| 수락 | 필수 |

Contracts Items만 참조. 골드·명성 숫자는 `05-quest-lock-polish_missingReq.md`를 채운다.

## 4. 의뢰 풀 threshold (lock)

| reputation >= | 풀 |
|---------------|-----|
| 0 | 튜토 |
| 50 | 일반 1단계 |
| 150 | 일반 2단계 |

## 5. 결산 UI polish

| 요소 | 동작 |
|------|------|
| 진행도 | 보유 n / 요구 m |
| 납품 버튼 | 전부 충족 시만 활성 |
| 부족 | 버튼 빨강 + 「○○ 부족」 |
| 보상 요약 | 납품 직후 골드·명성·아이템 |
| 멀티데이 | D-n |

Settlement 페이즈에만 패널 표시.

## 6. 완료 기준

- [ ] `00100001` SO: id·요구·납기·필수·채굴기 보상 반영
- [ ] gold/reputation 지급이 `GameSessionState`에 반영
- [ ] 결산 n/m · 부족 빨간 버튼 · 보상 요약
- [ ] 풀 threshold lock · Refresh 필터
- [ ] `00100001` 전량 보유 납품 1회 통과
