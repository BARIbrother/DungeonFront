# 경제·명성 해금 UI

> **역할**: Dev2 · **Week**: 3 · **Issue**: 02  
> **선행**: `GameSessionState.gold/reputation` · W2 상점 껍데기  
> **연동**: [05-quest-lock-polish](./05-quest-lock-polish.md) threshold · Lead 기계 id  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

Prepare 단계 **골드 구매(상점)** · **명성 해금** · **의뢰 풀 threshold** UI.

**코드**: `Assets/Scripts/Quest/ShopUI.cs`, `UnlockManager.cs` (신규)

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| P키 상점 패널 (기존 `shopWindow`) | 구역 확장 구매 |
| 명성 조건 기계/레시피 해금 버튼 | Lead 배치 로직 |
| 명성별 의뢰 풀 단계 | 전체 밸런스 시뮬 |

---

## 3. UI 표시 규칙 (필수)

Dev1이 상점을 토글하지 않음 — **Dev2 자체 구독**:

```csharp
void OnEnable() {
    GameSessionState.Instance.OnPhaseChanged += phase => {
        shopPanel.SetActive(phase == GamePhase.Prepare);
        unlockPanel.SetActive(phase == GamePhase.Prepare);
    };
}
```

Prepare 중 P키: `GameSessionState`가 `shopWindow` 토글 — Dev2는 `shopWindow` **내부** UI만 채움.

---

## 4. 상점 (골드)

| 항목 | 스펙 |
|------|------|
| 목록 | ScriptableObject `ShopCatalog` — itemId, price, displayName |
| 구매 | `gold >= price` → `GameSessionState` 골드 차감 + `PlayerInventory.Add` |
| MVP 최소 | 재료 1종 (예: iron_ore ×1) + 기계 1종 (해금된 `machineDefId`) |

```csharp
public bool TryPurchase(string entryId) {
    if (session.gold < entry.price) return false;
    session.AddGold(-entry.price); // Dev1 API 합의
    inventory.Add(entry.ToItemEntry());
    return true;
}
```

**Contracts Items만** 참조 — [dev-contract.md](../../dev-contract.md).

---

## 5. 명성 해금

[03-machine-plan.md](../../../03-machine-plan.md) 해금 초안:

| machineDefId | 명성 조건 |
|--------------|-----------|
| `제단_1` | 350 |
| `주조소_1` | 500 |
| 마나 계열 4종 | 2번째 스토리 의뢰 클리어 (의뢰 플래그 또는 명성 0) |

`UnlockManager`:

```csharp
public bool IsUnlocked(string machineDefId);
public bool TryUnlock(string machineDefId); // 명성 충족 시 플래그 저장
```

- 해금 후: 상점에서 구매 가능 또는 NewGame 외 **인벤 직접 지급은 하지 않음** (구매로만)
- 튜토 4단계: 명성으로 해금 → 골드 구매 ([01-core-loop.md](../../../01-core-loop.md))

---

## 6. 의뢰 풀 threshold

[05-quest-lock-polish](./05-quest-lock-polish.md)와 **동일 표** lock:

| reputation >= | 풀 |
|---------------|-----|
| 0 | 튜토 + 기본 |
| 50 | tier 1 일반 |
| 150 | tier 2 |

`QuestManager.RefreshAvailableQuests()`:

```csharp
int rep = GameSessionState.Instance.reputation;
availableQuestsToday = pool.Filter(q => q.minReputation <= rep);
```

---

## 7. 구현 단계

- [ ] `ShopCatalog.asset` — 최소 2항목
- [ ] `ShopUI` — 목록·구매 버튼·골드 표시
- [ ] `UnlockManager` — PlayerPrefs 또는 SaveData 플래그 (세이브 연동 권장)
- [ ] `UnlockUI` — 조건 텍스트·해금 버튼
- [ ] `RefreshAvailableQuests` threshold
- [ ] Dev1 `AddGold` / reputation API — 없으면 W3에 추가 요청

---

## 8. 검증 시나리오

- [ ] Prepare P키 → 상점 표시
- [ ] 골드 충분 → iron_ore 구매 → 인벤 +1, 골드 차감
- [ ] 골드 부족 → 구매 실패 메시지
- [ ] 명성 350 달성 → 제단 해금 버튼 활성 → 해금 후 상점에 제단 등장
- [ ] 명성 50 → 의뢰 풀 항목 증가

---

## 9. 완료 기준

- [ ] §8 통과
- [ ] Production 페이즈에서 상점 **열리지 않음**
- [ ] Lead `machineDefId`와 해금 id 일치

---

## 10. 관련 문서

- [03-machine-plan.md](../../../03-machine-plan.md)
- [05-quest-lock-polish](./05-quest-lock-polish.md)
