# 멀티데이 납기 추적

**역할**: Dev2 · **Week**: 3 · **Issue**: 01  
**선행**: W2 Quest SO · `Quest.deadlineDays` · `currentleftDeadlineDays`

## 1. 이 작업물

당일이 아닌 의뢰의 남은 일수를 추적하고, Prepare·결산 UI에 표시한다.

**코드**: `Assets/Scripts/Quest/` — `QuestManager`, UI

## 2. 설명

- `deadlineDays`: 수락 후 며칠 안에 납품 (0 = 당일)
- 런타임: `currentleftDeadlineDays`로 남은 일수 추적
- 일차가 지나면 남은 일수가 줄고, 0이면 「오늘 마감」
- UI: Prepare·결산에 `납기 D-n` / `오늘 마감` (만료 시 빨간색)

## 3. 완료 기준

- [ ] 멀티데이 의뢰 남은 일수 추적
- [ ] 일차 진행 후 일수 감소·활성 유지
- [ ] Prepare·결산에 D-n / 오늘 마감 표시
- [ ] 세이브/로드 후 납기 일수 유지
