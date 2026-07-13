# 생산 종료 요약

> **역할**: Lead · **Week**: 3 · **Issue**: 04  
> **선행**: `GameSessionState.StartProduction` / `ForceEndProduction` · `Machine` 생산 종료 처리  
> **연동**: Dev1 — `OnPhaseChanged`로 Settlement 진입  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

생산 페이즈 **종료 시**(5분 만료 또는 Dev1 `ForceEndProduction`) Factory 화면에 **모달 요약 팝업**을 띄운다.  
완성품 목록 표시 후 확인 → **Settlement 페이즈** 진입.

**코드**: `Assets/Scripts/GameFlow/` 또는 `Assets/Scripts/Production/ProductionSummaryUI.cs` (신규)

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 완성품 (아이템·수량) 목록 | 결산 의뢰 UI (Dev2) |
| 확인 버튼 → `SetPhase(Settlement)` | 인벤으로 이동된 품목 **재이동** (이미 자동 처리) |
| WIP 환원 요약 (선택) | 세이브 |

---

## 3. 코어 루프 정의

[01-core-loop.md](../../../01-core-loop.md) **생산 종료 시 자동 처리**:

1. 기계 WIP → 재료 환원, 기계 상태 초기화
2. 맵·기계 **완성품** → **인벤토리** 일괄 이동
3. **요약 팝업** 표시

**타이밍**: `GameSessionState`가 Production → Settlement 전환 **직전** 또는 **직후**에 팝업.  
권장: **인벤 이동 완료 후** 팝업 → 확인 시 `SetPhase(Settlement)`.

---

## 4. UI 스펙

| 요소 | 내용 |
|------|------|
| 트리거 | `phase == Production` 종료 (타이머 0 또는 ForceEnd) |
| 형태 | Factory 씬 **모달** (전체 화면 dim + 패널) |
| 제목 | 예: 「생산 종료」 |
| 본문 | 완성품 리스트 — `displayName × count` |
| (선택) | WIP 환원: 「진행 중이던 ○○ → 재료 반환」 |
| 버튼 | **확인** 1개 — 클릭 시 팝업 닫기 + Settlement |
| 입력 | 팝업 열림 중 **게임 입력 잠금** (이동·배치 불가) |

### 빈 목록

- 생산 0이면 「이번 생산에서 완성된 품목이 없습니다」 표시 후 확인 가능

---

## 5. 데이터 수집

생산 종료 핸들러에서 **인벤 이동 전** 스냅샷 권장:

```csharp
public struct ProductionSummaryLine {
    public string itemId;
    public string displayName;
    public int count;
}

// 의사코드
List<ProductionSummaryLine> lines = CollectFinishedGoodsFromMap();
TransferAllFinishedGoodsToInventory(); // 기존 로직
ShowSummaryModal(lines);
// OnConfirm → GameSessionState.Instance.SetPhase(GamePhase.Settlement);
```

**수집 대상**: outputPort, 벨트 heldItem, 창고 버퍼 등 맵 위 완성품.  
WIP 환원분은 요약 **제외** (선택 섹션만).

---

## 6. Dev1 연동

| 항목 | 담당 |
|------|------|
| `SetPhase(Settlement)` 호출 | Lead 팝업 확인 시 **또는** Dev1이 `OnPhaseChanged`만 구독 |
| 생산 타이머 | `GameSessionState.ProductionRemainingSeconds` — HUD는 Dev1 |
| 틱 정지 | Settlement 진입 시 `TickManager` / `IFactoryProduction` 정지 — Dev1 W3 |

**주의**: Dev1이 `orderWindow` 등을 직접 토글하지 않음. Settlement UI는 Dev2.

---

## 7. 구현 단계

- [ ] 생산 종료 진입점 1곳에 훅 (`GameSessionState` Update 또는 전용 `EndProduction()`)
- [ ] 맵 완성품 → 인벤 일괄 이동 로직 (없으면 구현)
- [ ] WIP 리셋·환원 (기존 `Machine` 로직 호출)
- [ ] `ProductionSummaryUI` — 리스트 바인딩
- [ ] 확인 → `SetPhase(Settlement)` — **유효 전환**만 (`Production → Settlement`)
- [ ] `StoryEventBus.Raise("OnProductionEnded")` — [06-story-hooks](./06-story-hooks.md)

---

## 8. 검증 시나리오

- [ ] Prepare → StartProduction → **5분 대기** (또는 `ForceEndProduction`) → 요약 팝업
- [ ] 채굴+용광로 가동 후 종료 → iron_ore·iron 등 목록 표시
- [ ] 확인 → Settlement 페이즈, Factory 입력 잠금
- [ ] 팝업 중 플레이어 이동 불가
- [ ] [06-story-hooks](./06-story-hooks.md) `OnProductionEnded` Dev1 수신

---

## 9. 완료 기준

- [ ] §8 시나리오 통과
- [ ] 인벤 수량 = 요약 수량과 일치 (이동 후)
- [ ] 크래시·중복 Settlement 전환 없음

---

## 10. 관련 문서

- [01-core-loop.md](../../../01-core-loop.md)
- [06-story-hooks](./06-story-hooks.md)
- [07-mvp-integration](./07-mvp-integration.md)
