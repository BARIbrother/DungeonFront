# 기계 정보·피드백

> **역할**: Lead · **Week**: 4 · **Issue**: 04  
> **선행**: `MachineRecipeUI` · [01-mvp-factory-polish](./01-mvp-factory-polish.md)  
> **기획**: [dev-gaps.md](../../dev-gaps.md) §7 · [01-core-loop.md](../../../01-core-loop.md)  
> **계약**: [dev-contract.md](../../dev-contract.md)

---

## 1. 이 작업물

기계 **선택·정보 패널**을 최소 완성한다.  
플레이어가 레시피·WIP·고장을 한눈에 보게 해 뒷산 클리어 중 헤매지 않게 한다.

**코드**: `MachineRecipeUI` 확장 또는 `MachineInfoPanel.cs`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 선택 시 이름·레시피·진행도·고장 | 풀 인스펙터·스탯 트리 |
| 고장 알림 (아이콘/tint 회귀) | Dev1 HUD 전면 개편 |
| 우클릭 레시피 UI 폴리시 | 신규 레시피 수치 (W3 lock 유지·미세 조정만) |

---

## 3. UI 스펙 (최소)

| 요소 | 내용 |
|------|------|
| 이름 | `displayName` |
| 레시피 | 현재 `Recipe` 표시명 · 입·출력 |
| WIP | 진행 바 또는 tick/click 잔여 |
| 상태 | Idle / Working / Broken / Frozen(해당 없음) |

---

## 4. 구현 단계

- [ ] 클릭/호버 선택 → 패널 갱신
- [ ] 고장 시 패널·월드 둘 다 표시
- [ ] Production 종료·회수 후 패널 정리

---

## 5. 완료 기준

- [ ] 가동 중 기계에서 WIP/레시피 확인 가능
- [ ] 고장 기계 선택 시 Broken 표시
- [ ] 우클릭 레시피 전환 회귀

---

## 6. 관련 문서

- [week3-lead/04-recipe-ui-summary](../../week3/week3-lead/04-recipe-ui-summary.md)
- [05-team-integration](./05-team-integration.md)
