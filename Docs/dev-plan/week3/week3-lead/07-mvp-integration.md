# Lead — MVP 통합·버그 수정

> **역할**: Lead · **Week**: 3 · **Issue**: 07  
> **시점**: 금요일  
> **선행**: W3 Lead Issue 01~06 + 타 역할 산출물  
> **통합 문서**: [week3-dev1/06-team-integration](../week3-dev1/06-team-integration.md)

---

## 1. 이 작업물

Lead·Dev1·Dev2·Art **W3 산출물을 develop에 머지**하고, MVP 통합 데모 체크리스트를 통과시킨다.  
Lead 담당 영역 버그 우선 수정.

---

## 2. 머지 순서

[week3-dev1/06-team-integration](../week3-dev1/06-team-integration.md)와 동일:

```
dev1/w3-* → dev2/w3-* → lead/w3-* → art/w3-* → develop
```

충돌 시:

| 영역 | 소유 |
|------|------|
| `GameFlow/` | Dev1 |
| `Quest/` | Dev2 |
| `Machine/`, `Placement/`, Factory 씬 | Lead |
| `Assets/Art/` | Art |

---

## 3. 통합 체크리스트 (Lead 검증 관점)

### 3-1. Factory·생산

1. [ ] NewGame → Factory 씬 · HUD 표시 (Dev1)
2. [ ] 10종+ 기계 중 해금된 type 배치·가동 ([01-machines-10plus](./01-machines-10plus.md))
3. [ ] 벨트 또는 수동 운반으로 **튜토 의뢰 물품** 생산 ([05-recipe-data-lock](./05-recipe-data-lock.md))
4. [ ] 수작업 제작기 클릭 5회 동작 ([03-manual-interaction](./03-manual-interaction.md))
5. [ ] 5분(또는 ForceEnd) 후 **생산 요약** → Settlement ([04-recipe-ui-summary](./04-recipe-ui-summary.md))

### 3-2. 스토리·세이브

6. [ ] `001E00001` Prepare 스토리 이벤트 Dev1 수신 ([06-story-hooks](./06-story-hooks.md))
7. [ ] 세이브 → 로드 후 **맵 배치·기계 포트·WIP** 복원 (Dev1 `IFactorySave` + Lead 구현)
8. [ ] 회수 시 인벤 반환 — `ReturnAllContentsToPlayerInventory` 회귀

### 3-3. 안정성

9. [ ] 튜토~1회 결산 플레이 **크래시 0**
10. [ ] Console Error 0 (경고는 문서화된 것만)

---

## 4. Lead 버그 triage

| 증상 | 확인 위치 |
|------|-----------|
| 배치 불가 | `GridManager`, `PlacementController`, footprint |
| 틱 안 돎 | `TickManager`, `IFactoryProduction`, `GamePhase` |
| 레시피 null | `RecipePool`, [05-recipe-data-lock](./05-recipe-data-lock.md) |
| 요약 팝업 안 뜸 | `GameSessionState` Production 종료 훅 |
| 스토리 미발행 | `FactoryStoryHooks`, `StoryEventBus` |

---

## 5. 전달물 확인

| 제공자 | Lead가 받는 것 |
|--------|----------------|
| Dev1 | `IFactorySave` 호출 시점, `SaveData.factory` 필드 |
| Dev2 | 의뢰 SO lock, 결산 UI |
| Art | 주인공 시트, 초상·표정 (Dev1 경유) |

Lead가 제공:

| 수신 | 내용 |
|------|------|
| Dev1 | `StoryEventBus`, `IFactorySave` 구현 |
| Dev2 | 생산 가능 itemId·기계 id |

---

## 6. 완료 기준

- [ ] §3 체크리스트 **전항목** 통과 또는 미통과 항목 이슈 티켓화
- [ ] develop 브랜치에서 통합 데모 1회 녹화/시연 가능
- [ ] [week3.md](../week3.md) 통합 데모 표 전체 체크

---

## 7. 관련 문서

- [week3.md](../week3.md)
- [week3-dev1/06-team-integration](../week3-dev1/06-team-integration.md)
- [dev-contract.md](../../dev-contract.md)
