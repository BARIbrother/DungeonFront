# 멀티데이 납기 추적

> **역할**: Dev2 · **Week**: 3 · **Issue**: 01  
> **선행**: W2 Quest SO · `Quest.deadlineDays` · `currentleftDeadlineDays`  
> **연동**: [03-gameover-penalty](./03-gameover-penalty.md) · [05-quest-lock-polish](./05-quest-lock-polish.md)  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

**당일이 아닌** 의뢰의 남은 일수를 추적하고, Prepare·결산 UI에 표시한다.  
테스트용 **멀티데이 Quest SO 1종** 추가.

**코드**: `Assets/Scripts/Quest/` — `QuestManager`, `ActiveQuest` 래퍼 (선택), UI

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| `daysRemaining` / `dueDay` 런타임 필드 | 달력 UI |
| `AdvanceDay()` 시 감소 | 실시간 시계 납기 |
| 결산 「D-n」표시 | 새 Quest ID 체계 |

---

## 3. 데이터 모델

### Quest SO (정적)

| 필드 | 설명 |
|------|------|
| `deadlineDays` | 수락 후 **며칠 안**에 납품 (0 = 당일) |

[01-core-loop.md](../../../01-core-loop.md) 예시: 철 판 500개 **5일** 의뢰.

### 런타임 (수락 인스턴스)

현재 `QuestManager.CreateQuestInstance`가 복사:

```csharp
instance.currentleftDeadlineDays = source.deadlineDays;
```

**추가 권장**:

```csharp
public int acceptedDay;  // 수락한 GameSessionState.day
// dueDay = acceptedDay + deadlineDays (당일 납기면 acceptedDay)
```

### 테스트 SO

| questId | deadlineDays | isMandatory | 용도 |
|---------|--------------|-------------|------|
| `00100999` (임시) | **3** | false | 멀티데이 QA |
| `00100001` | 0 | true | 당일 — 회귀 |

에셋: `Assets/Data/Quests/Quest_00100999.asset`

---

## 4. 일차 진행 규칙

| 이벤트 | 동작 |
|--------|------|
| `acceptQuest` | `currentleftDeadlineDays = deadlineDays`, `acceptedDay = day` |
| `AdvanceDay()` (Settlement → Prepare) | 모든 `currentQuests`에 대해 `currentleftDeadlineDays--` (당일 납기는 0 유지) |
| 결산 진입 | `currentleftDeadlineDays == 0` → **오늘 마감** 강조 |

**당일 납기** (`deadlineDays == 0`):

- 수락한 **그날** 결산에서만 납품 가능
- 다음 날 Advance 시 미납 처리 ([03-gameover-penalty](./03-gameover-penalty.md))

---

## 5. UI

| 위치 | 표시 |
|------|------|
| Prepare 의뢰 목록 | `납기 D-3` / `오늘 마감` |
| Settlement | 동일 + 만료 시 빨간색 |
| (선택) HUD | 가장 임박한 의뢰 1건 |

```csharp
string FormatDeadline(Quest q, int today) {
    if (q.currentleftDeadlineDays <= 0) return "오늘 마감";
    return $"D-{q.currentleftDeadlineDays}";
}
```

---

## 6. 구현 단계

- [ ] `acceptedDay` 필드 — `CreateQuestInstance`에서 설정
- [ ] `GameSessionState.AdvanceDay` 후킹 — QuestManager `OnDayAdvanced()`
- [ ] 테스트 SO `00100999` — 풀에 추가 (Prepare Refresh)
- [ ] UI 바인딩 — [04-settlement-quest-list](../../week2/week2-dev2/04-settlement-quest-list.md) 확장
- [ ] [04-quest-serialize](./04-quest-serialize.md) — `daysRemaining`, `acceptedDay` 저장

---

## 7. 검증 시나리오

- [ ] 멀티데이 의뢰 수락 → `AdvanceDay` 1회 → `currentleftDeadlineDays` 감소
- [ ] 다음 날 Prepare에서도 **활성** 유지
- [ ] D-0 결산에서만 당일 납기 납품 가능
- [ ] 만료일 미납 → [03-gameover-penalty](./03-gameover-penalty.md) (필수/일반 분기)

---

## 8. 완료 기준

- [ ] §7 통과
- [ ] 테스트 SO 1종 존재
- [ ] 세이브/로드 후 납기 일수 유지 ([04-quest-serialize](./04-quest-serialize.md))

---

## 9. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md) — 멀티데이 예시
- [05-quest-lock-polish](./05-quest-lock-polish.md)
