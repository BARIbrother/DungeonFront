# 결산·의뢰 폴리시

> **역할**: Dev2 · **Week**: 4 · **Issue**: 05  
> **선행**: W3 결산·게임오버 · [02](./02-backcave-quest-fill.md)~[04](./04-quest-content-fill.md)  
> **계약**: [dev-contract.md](../../dev-contract.md)  
> **던전**: 뒷산만

---

## 1. 이 작업물

결산·미납·게임오버·멀티데이가 **뒷산 클리어 플로우**에서 깨지지 않게 폴리시한다.

---

## 2. 체크리스트

- [ ] 결산 n/m · 부분 납품 불가 · 버튼 상태
- [ ] 멀티데이 D-n 표시·일차 감소
- [ ] 필수 미납 → 게임오버 → 다른 슬롯
- [ ] 엔딩 직전 마지막 필수 납품 → 클리어 훅 ([03](./03-clear-line-lock.md))

---

## 3. 완료 기준

- [ ] §2 통과
- [ ] W3 결산 시나리오 회귀
- [ ] 뒷산 필수 1줄(축약) 납품~엔딩 훅 1회

---

## 4. 관련 문서

- [week3-dev2/03-gameover-penalty](../../week3/week3-dev2/03-gameover-penalty.md)
- [week4-dev1/05-team-integration](../week4-dev1/05-team-integration.md)
