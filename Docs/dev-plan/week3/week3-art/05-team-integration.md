# Art — Week 3 팀 전달

> **역할**: Art · **Week**: 3 · **Issue**: 05  
> **시점**: 금요일 통합 전  
> **선행**: [01](./01-eve-portrait.md) · [02](./02-ray-portrait.md) · [03](./03-character-expressions.md) · [04](./04-protagonist-motions-polish.md)

---

## 1. 이 작업물

W3 Art 산출물을 **Lead·Dev1**에게 import·검증 가능한 상태로 넘기고, 통합 데모 체크리스트를 통과시킨다.

---

## 2. 전달물 표

| 수신 | 파일 | 경로 | 용도 |
|------|------|------|------|
| **Dev1** | `eve_portrait.png` | `Assets/Art/UI/Portraits/` | Dialogue — 이브 기본 |
| **Dev1** | `ray_portrait.png` | 동일 | Dialogue — 레이 기본 |
| **Dev1** | 표정 PNG 전체 (§2-1) | 동일 | `DialogueUI` expression 교체 |
| **Lead** | 주인공 시트 (idle/walk/repair/work) | `Assets/Art/Characters/Protagonist/` | `PlayerMovement` Animator |

### 2-1. Dev1 표정 파일 전체 목록

```
protagonist_portrait.png   (기존)
protagonist_happy.png
protagonist_serious.png
eve_portrait.png           (W3 [01-eve-portrait](./01-eve-portrait.md))
eve_happy.png
eve_serious.png
ray_portrait.png
ray_serious.png
```

---

## 3. 전달 절차

1. [ ] PNG를 **최종 경로**에 commit (임시 폴더 사용 금지)
2. [ ] 채널에 **변경 요약** 1메시지:
   - 추가/수정 파일 목록
   - 주인공 시트 PPU·피벗 (Bottom Center 등)
   - Animator 파라미터 메모 ([04-protagonist-motions-polish](./04-protagonist-motions-polish.md) §4)
3. [ ] Dev1: Dialogue 테스트 씬 또는 `001E00001`에서 초상 로드 확인 요청
4. [ ] Lead: Factory 씬 이동·수리·작업 모션 확인 요청

---

## 4. 통합 검증 (금요일)

### Dev1

- [ ] `001E00001` 오프닝 — 주인공/이브 초상 + 표정 표시
- [ ] 레이 이벤트 — `ray` + `serious` 또는 `neutral`
- [ ] 대화 중 일시정지 시 초상 유지

### Lead

- [ ] Factory 이동 — walk 루프 끊김 없음
- [ ] 그리드 칸 중앙 피벗
- [ ] 수리·작업 모션 트리거 정상

### 공통

- [ ] import 설정 Point filter, PPU 통일
- [ ] 깨진 참조(Sprite Missing) 0건

---

## 5. 블로커 시

| 문제 | 담당 | 조치 |
|------|------|------|
| 초상 슬롯 크기 안 맞음 | Art+Dev1 | style guide 캔버스 또는 UI Rect 합의 |
| walk 피벗 어긋남 | Art | 시트 pivot 재export → Lead 재import |
| 표정 파일명 불일치 | Art | [03-character-expressions](./03-character-expressions.md) 표 준수 |

---

## 6. 관련 문서

- [week3-dev1/06-team-integration](../week3-dev1/06-team-integration.md) — 전체 통합 데모
- [week3.md](../week3.md) — W3 목표
