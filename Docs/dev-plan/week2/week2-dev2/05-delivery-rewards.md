## 납품·보상 (1차)

### 이 작업물

결산 단계 **납품 자동 차감**·**충족 판정**·**보상 지급** (골드·명성·아이템).  
구 7주 3주 Dev2 항목을 **W2로 앞당김** — Lead 생산과 **무관**, 인벤 수만 검사.

**코드**: `Assets/Scripts/Quest/SettlementDelivery.cs` (신규 또는 확장)

### 동작

| 단계 | 내용 |
|------|------|
| 판정 | `PlayerInventory.GetCount(itemId)` vs 의뢰 요구량 |
| 납품 | 충족 시 `Remove(itemId, amount)` |
| 보상 | `GameSessionState` 골드·명성 + `PlayerInventory.Add()` |

### 독립 개발 (Mock)

```csharp
// 테스트: Contracts Items로 인벤 채운 뒤 결산 진입
inventory.Add(new ItemEntry { item = ironOreContract, count = 5 });
```

- [ ] Lead 생산·틱 **불필요** — 수동 `Add()`로 검증
- [ ] Dev1 HUD — `OnQuestsChanged` 이벤트만 발행

### UI

- [ ] 결산 목록 n/m (04와 연동)
- [ ] 납품 버튼 — 부족 시 비활성·빨간 표시
- [ ] 보상 토스트 또는 목록 갱신

### 완료 기준

- [ ] 튜토 의뢰 `00100001` — 인벤에 요구품 있을 때 납품·골드·명성 증가
- [ ] 부족 시 납품 불가
