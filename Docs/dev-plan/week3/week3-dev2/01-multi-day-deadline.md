## 멀티데이 납기 추적

### 이 작업물

당일이 아닌 **멀티데이** 의뢰의 남은 일수·결산 표시.  
`00100001`은 당일 — 별도 테스트용 멀티데이 Quest SO 1종 추가.

**코드**: `Assets/Scripts/Quest/` — `ActiveQuest`에 `daysRemaining` 등

### 동작

- [ ] 수락 시 `dueDay` 또는 `daysRemaining` 저장
- [ ] `AdvanceDay()` 시 감소 (Dev1 이벤트 구독 또는 결산 시)
- [ ] 결산 UI — 「납기 D- n」·당일 강조

### 독립 개발

- [ ] `GameSessionState.Day` 읽기만 — Dev1 API 계약
- [ ] Lead·생산 무관

### 완료 기준

- [ ] 멀티데이 의뢰 수락 후 다음 날에도 활성 유지
- [ ] 납기 만료일 결산에서 실패·미납 처리 연동 (03)
