# 주인공 모션 보완

> **역할**: Art · **Week**: 3 · **Issue**: 04  
> **선행**: [week2 repair/work](../../week2/week2-art/01-protagonist-repair-work.md) · [01-style-guide](../../week1/week1-art/01-style-guide.md)  
> **후속**: [05-team-integration](./05-team-integration.md)  
> **수신자**: Lead (`PlayerMovement` Animator 연동)

---

## 1. 이 작업물

Factory 탑다운 **주인공 스프라이트 시트**를 W2 피드백 기준으로 다듬는다.  
Lead가 `Assets/Art/Characters/Protagonist/`에 import하면 **이동·수리·작업** 모션이 끊김 없이 재생되어야 한다.

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| idle / walk / repair / work 스프라이트 수정 | 신규 모션 종류 (attack, carry 등) |
| 피벗·스케일·프레임 타이밍 통일 | Animator Controller 코드 작성 (Lead) |
| 4방향 또는 하향+flip 규격 유지 | 초상화·표정 ([03-character-expressions](./03-character-expressions.md)) |

---

## 3. 현재 상태 (W2 산출)

| 항목 | 경로·내용 |
|------|-----------|
| 시트 폴더 | `Assets/Art/Characters/Protagonist/` |
| Lead 연동 | `PlayerMovement.cs` — `IsMoving`, `MoveX`, `MoveY` 파라미터 |
| 수리·작업 | W2에 repair/work 시트 전달됨. **피벗·루프 품질 이슈** 남음 |
| 그리드 | 1 unit = 1칸, 캐릭터 **1칸 높이** = style guide px (예: 32px) |

---

## 4. 산출물

| 파일 | 설명 |
|------|------|
| `protagonist_idle.png` (또는 통합 시트) | 4방향 idle, 최소 1프레임/방향 |
| `protagonist_walk.png` | 4방향 walk, **2~4프레임/방향** |
| `protagonist_repair.png` | 수리 루프 2~4프레임 |
| `protagonist_work.png` | 수작업 클릭 루프 2~4프레임 |

**권장**: 단일 **Sprite Sheet** 1장 + Unity Slice 메타는 Lead가 설정.  
파일명은 팀 합의 유지; 교체 시 **동일 경로 덮어쓰기**.

### Lead 전달 메모 (텍스트로 채널 공유)

```
Animator 파라미터 (현재):
- IsMoving (bool)
- MoveX, MoveY (float, -1~1)

추가 트리거 (Lead 요청 시):
- Repair (trigger)
- Work (trigger)

피벗: 발이 타일 하단 중앙 (0.5, 0) 기준
Pixels Per Unit: GridManager 기본 32
```

---

## 5. 스펙 (style guide 기준)

| 항목 | 값 |
|------|-----|
| 캔버스 높이 | style guide **1칸 높이 px**와 동일 (예: 32px) |
| 팔레트 | [01-style-guide](../../week1/week1-art/01-style-guide.md) 주색 8~16색 **엄수** |
| 윤곽선 | 가이드 규칙 따름 (보통 1px) |
| walk 프레임 간격 | **균등** — 시각적으로 끊김 없는 루프 |
| repair/work | 하향 1종 + 좌우 flip 허용 (4방향 미제작 시) |

### 피벗 규칙

- 캐릭터 **발 위치** = 스프라이트 하단 중앙
- 그리드 칸 중앙에 서 있을 때 타일과 겹치지 않게 **Y 오프셋 0**
- Lead 검증: Factory 씬에서 칸 경계에 발이 맞는지 확인

### 스케일 통일

- idle / walk / repair / work **동일 신체 크기** (머리~발 높이 px 동일)
- W2 대비 walk만 작거나 repair만 큰 경우 → 이번 Issue에서 통일

---

## 6. 작업 단계

### 6-1. 피벗

- [ ] 기존 시트를 style guide 1칸 높이에 맞춤
- [ ] Unity에서 import 후 **Pivot = Bottom Center** (Lead 확인용 스크린샷 1장)
- [ ] 4방향 idle 기준으로 walk/repair/work 정렬

### 6-2. walk 루프

- [ ] 프레임 타이밍 균등화 (깜빡임·끊김 제거)
- [ ] (선택) 2프레임 → **4프레임** 업그레이드 — 시간 여유 있을 때만
- [ ] 좌우 이동 시 flip 없이도 자연스러운지 확인

### 6-3. idle / repair / work

- [ ] idle과 walk **머리 위치** 수평 정렬
- [ ] repair·work 루프가 0.5초 내외로 반복 가능한 프레임 수
- [ ] 팔레트 drift 없음 (모션 간 색상 통일)

### 6-4. 반영

- [ ] `Assets/Art/Characters/Protagonist/`에 PNG export
- [ ] Lead에게 **변경 요약 + Animator 파라미터 메모** 전달

---

## 7. Lead 연동 (참고 — Art는 코드 수정 안 함)

Lead `PlayerMovement` / Animator에서 기대하는 동작:

| 상황 | 모션 |
|------|------|
| 입력 없음 | idle |
| WASD 이동 | walk + `MoveX`/`MoveY` |
| 고장 기계 근접 + 수리 클릭 | repair 트리거 |
| `requiresManualWork` 기계 클릭 | work 트리거 |

상세: [week3-lead/03-manual-interaction](../week3-lead/03-manual-interaction.md)

---

## 8. 완료 기준 (검증)

Art 단독:

- [ ] PNG가 `Assets/Art/Characters/Protagonist/`에 존재
- [ ] style guide 팔레트·1칸 높이 준수
- [ ] idle/walk/repair/work **동일 스케일**

Lead 통합 후 (금요일):

- [ ] Factory에서 이동 시 walk 루프 끊김 없음
- [ ] 그리드 칸 중앙에 발 위치 정확
- [ ] 수리·작업 클릭 시 해당 모션 재생

---

## 9. 관련 문서

- [01-style-guide](../../week1/week1-art/01-style-guide.md)
- [week2 protagonist repair/work](../../week2/week2-art/01-protagonist-repair-work.md)
- [05-team-integration](./05-team-integration.md)
