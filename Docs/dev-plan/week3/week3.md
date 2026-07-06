# Week 3 — 개발 계획 (개요)

> [dev-plan.md](../dev-plan.md) · [parallel-roadmap.md](../parallel-roadmap.md) · [dev-contract.md](../dev-contract.md)

## 공통 목표

**튜토리얼 패널** + HUD 보강 + Lead 생산 연동 + Dev2 **납품·보상** + 아트(레이 초상화, 모션 보완).

## 데모 체크리스트

- [ ] Prepare — **튜토리얼 패널** (Dev1)
- [ ] HUD — 수락 의뢰·튜토 진행 (Dev1)
- [ ] Lead **철 체인 틱** + Dev1 `IFactoryProduction` 연동
- [ ] 결산 — **납품·보상** (Dev2)
- [ ] **레이** 초상화 · 주인공 모션 보완 (Art)

## 역할

| 역할 | 범위 | 문서 |
|------|------|------|
| **Lead** | 10종+·벨트·수작업·레시피 UI | [week3-lead/](./week3-lead/) |
| **Dev1** | 튜토 패널·HUD·틱 연동 | [week3-dev/](./week3-dev/) *(W3 분화 예정: dev1)* |
| **Dev2** | 납품·보상·멀티데이 | *(W3 분화 예정: week3-dev2)* |
| **Art** | 레이 초상화, 모션 보완 | [week3-art/](./week3-art/) |

## 병렬 원칙

- Dev1 틱 연동: `IFactoryProduction` **인터페이스만** — Lead 미완 시 `NullFactoryProduction`
- Dev2 납품: `PlayerInventory.GetCount`만 — Lead 생산과 무관
- 튜토 일시정지: Dev1이 `StopTick()` 호출 ([dev-contract.md](../dev-contract.md))

## Issue

| Lead | [week3-lead/](./week3-lead/) |
| Dev | [week3-dev/](./week3-dev/) |
| Art | [week3-art/](./week3-art/) |
