# 게임오버·미납 페널티

> **역할**: Dev2 · **Week**: 3 · **Issue**: 03  
> **선행**: [01-multi-day-deadline](./01-multi-day-deadline.md) · W2 납품  
> **기획**: [01-core-loop.md](../../../01-core-loop.md) — 미납 명성 50%  
> **코드**: `Assets/Scripts/Quest/GameOverController.cs`

---

## 1. 이 작업물

**필수(스토리) 의뢰 실패** → 게임오버 UI → Title 복귀.  
**일반 의뢰 미납** → 보상 명성 **50%**만 지급 (또는 차감).

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 필수 의뢰 납기 만료·미충족 판정 | 세이브 슬롯 자동 삭제 |
| 게임오버 UI + Title 로드 | 엔딩 실패 분기 연출 |
| 미납 50% 명성 규칙 | 골드 페널티 (MVP 없음) |

---

## 3. 판정 규칙

| 조건 | 결과 |
|------|------|
| `isMandatory == true` + 납기 만료 + 미납품 | **게임오버** |
| `isMandatory == true` + 요구품 부족한 채 만료 결산 | **게임오버** |
| 일반 의뢰 + 납품 성공 | 정상 보상 |
| 일반 의뢰 + **미납** (만료 또는 스킵) | 명성 보상 × **0.5** |
| 일반 의뢰 + 부분 보유 | 납품 불가 (기존) — 미납 처리 |

### 납기 만료 시점

결산 페이즈 `EvaluateDelivery()` (또는 「다음 날」 직전):

1. `currentleftDeadlineDays <= 0` 이고 아직 `currentQuests`에 있음
2. `progressQuest` 실패 → 미납 분기

---

## 4. Quest 필드

```csharp
// Quest.cs — W3 추가
public bool isMandatory;
public int rewardReputation; // givePlayerReward에서 사용
```

테스트 SO:

| questId | isMandatory |
|---------|-------------|
| `00100001` | **true** |
| `00100999` | false |
| `00100888` (신규 테스트) | **true**, 당일, 요구품 불가능 | QA용 게임오버 |

---

## 5. 게임오버 UI

| 요소 | 내용 |
|------|------|
| 트리거 | 필수 의뢰 실패 판정 직후 |
| 본문 | 「필수 의뢰를 완료하지 못했습니다」 |
| 버튼 | Title로 — `SceneManager.LoadScene("Title")` |
| 세이브 | **삭제하지 않음** — 다른 슬롯 재도전 ([01-core-loop.md](../../../01-core-loop.md)) |

---

## 6. 미납 50% 구현

```csharp
void ApplyReward(Quest quest, bool delivered) {
    if (!delivered) {
        if (quest.isMandatory) TriggerGameOver();
        return;
    }
    int rep = quest.rewardReputation;
    if (WasUnderDelivered(quest)) // 미납 플래그
        rep = Mathf.RoundToInt(rep * 0.5f);
    session.AddReputation(rep);
}
```

**미납 정의**: 만료일까지 `progressQuest` 한 번도 성공 못 함 → 결산에서 「포기」 또는 다음 날 넘김.

---

## 7. 구현 단계

- [ ] `Quest.isMandatory` — SO 설정
- [ ] `EvaluateDelivery()` — 결산 진입 시 호출 (Dev2)
- [ ] 만료 의뢰 순회 — 필수/일반 분기
- [ ] `GameOverController` UI
- [ ] 일반 미납 시 50% — 로그로 수치 확인
- [ ] 테스트 필수 SO `00100888` (선택)

---

## 8. 검증 시나리오

- [ ] 필수 `00100001` — 요구품 없이 결산만료 → 게임오버 → Title
- [ ] 일반 멀티데이 — 만료 후 미납 → 50% 명성 (정상 납품 대비 절반)
- [ ] 필수 의뢰 성공 → 게임오버 없음
- [ ] 게임오버 후 다른 슬롯 불러오기 가능

---

## 9. 완료 기준

- [ ] §8 통과
- [ ] [05-quest-lock-polish](./05-quest-lock-polish.md) 필수 플래그와 일치
- [ ] [06-team-integration](../week3-dev1/06-team-integration.md) 「게임오버」항목

---

## 10. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md) — 메타·UI 표
- [01-multi-day-deadline](./01-multi-day-deadline.md)
