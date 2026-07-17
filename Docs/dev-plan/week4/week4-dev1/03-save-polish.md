# 세이브 안정화

> **역할**: Dev1 · **Week**: 4 · **Issue**: 03  
> **선행**: W3 [04-save-load](../../week3/week3-dev1/04-save-load.md)  
> **연동**: Lead `IFactorySave` · Dev2 `IQuestSaveProvider`  
> **계약**: [dev-contract.md](../../dev-contract.md)  
> **비범위**: 멀티 던전 SaveData

---

## 1. 이 작업물

뒷산 **단일 던전** 세이브·로드를 튜토~엔딩 완주 기준으로 안정화한다.  
슬롯·autosave·게임오버 재도전 경로를 막는다.

**코드**: `SaveData`, `SaveLoadService`

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 일차·인벤·골드·명성·factory·quests round-trip | `factories` Dictionary·던전 id |
| autosave 시점 정리 (결산 후 등) | 클라우드 |
| 구세이브 호환 (version 유지/마이너 bump) | 슬롯 수 변경 |

---

## 3. 체크리스트

- [ ] 배치·WIP·구역 해금 복원 (Lead)
- [ ] 진행 중 의뢰·멀티데이 복원 (Dev2)
- [ ] Title 로드 → 동일 페이즈/일차
- [ ] 게임오버 후 **다른 슬롯** 재도전
- [ ] 중간 세이브 → 엔딩 직전 로드 회귀

---

## 4. 완료 기준

- [ ] 튜토 직후 세이브 → 로드 → 진행 계속
- [ ] 클리어 직전 세이브 → 로드 → 엔딩 가능
- [ ] Corrupt/부분 null 시 크래시 없이 안내

---

## 5. 관련 문서

- [week3-dev1/04-save-load](../../week3/week3-dev1/04-save-load.md)
- [05-team-integration](./05-team-integration.md)
