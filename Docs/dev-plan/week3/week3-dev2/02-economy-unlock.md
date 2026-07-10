## 경제·명성 해금 UI

### 이 작업물

준비 단계 **골드 구매**(재료·기계 placeholder) · **명성 기반 해금** · 의뢰 풀 threshold.  
구 7주 5주 「경제」「해금」 항목.

**코드**: `Assets/Scripts/Quest/ShopUI.cs`, `UnlockManager.cs` (또는 Quest 폴더 내)

### UI

- [ ] 상점 패널 — 재료·기계 목록·골드 차감 (`GameSessionState`)
- [ ] 해금 패널 — 명성 조건·기계/레시피 unlock 버튼
- [ ] 의뢰 풀 — 누적 명성 threshold (W2 풀 확장)

### 표시 규칙 (Dev1 개입 없음)

```csharp
GameSessionState.Instance.OnPhaseChanged += phase => {
    shopPanel.SetActive(phase == GamePhase.Prepare);
};
```

### 독립 개발

- [ ] 구매 아이템 — Contracts Items·기계 id
- [ ] Lead 10종+ 미완 시 **해금 목록 하드코딩** → 금요일 SO 연동

### 완료 기준

- [ ] 골드로 재료 1종 구매 → 인벤 증가
- [ ] 명성 조건 충족 시 기계 1종 해금 표시
