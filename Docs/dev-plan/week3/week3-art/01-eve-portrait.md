# 이브 기본 초상화

> **역할**: Art · **Week**: 3 · **Issue**: 01  
> **선행**: [01-style-guide](../../week1/week1-art/01-style-guide.md) · W1 주인공 초상  
> **참고**: W2 [02-eve-portrait](../../week2/week2-art/02-eve-portrait.md) (placeholder 있을 수 있음)  
> **후속**: [03-character-expressions](./03-character-expressions.md) · [05-team-integration](./05-team-integration.md)  
> **수신자**: Dev1 (`DialogueUI` 초상 슬롯)

---

## 1. 이 작업물

NPC **이브(Eve)** 의 대화 UI용 **기본(neutral) 초상** PNG 1종을 **MVP 품질**로 확정한다.  
W2에 placeholder가 있으면 **폴리시·교체**, 없으면 **신규 제작**.

협회 자격 평가원 — [04-story.md](../../../04-story.md) `npcId: eve`, **1일차** 첫 의뢰 안내 (`001E00002`).

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| neutral 표정 1종 (`eve_portrait.png`) | happy/serious → [03-character-expressions](./03-character-expressions.md) |
| 주인공·레이와 동일 화풍·캔버스 | 전신 일러스트 |
| Dev1 로드 가능한 파일명·경로 | DialogueUI 코드 |

---

## 3. 캐릭터 설정 (기획)

| 항목 | 내용 |
|------|------|
| 이름 | 이브 |
| 역할 | 대장간 협회 자격 평가원. 튜토·엔딩 필수 의뢰 전달 |
| 성격·비주얼 | 공식적·신뢰감. 주인공 placeholder 모션과 **동일 화풍** |
| 최초 대화 | 1일차 Prepare — `001E00001` 오프닝 직후 `001E00002` |
| 튜토 | 첫 의뢰 수락 방법 안내 ([01-core-loop.md](../../../01-core-loop.md)) |

---

## 4. 산출물

| 항목 | 값 |
|------|-----|
| **경로** | `Assets/Art/UI/Portraits/eve_portrait.png` |
| **형식** | PNG, 알파 채널 |
| **캔버스** | style guide **초상 캔버스** (예: 128×128 또는 96×96) |
| **프레이밍** | 머리~가슴 (주인공·레이와 동일) |
| **표정 id** | `neutral` (기본) |

### Dev1 로드 규칙 (합의)

```csharp
string characterId = "eve";
string expressionId = "neutral";
// → Portraits/eve_portrait.png
// 표정 variation: eve_happy.png, eve_serious.png — 03-character-expressions
```

파일명은 [03-character-expressions](./03-character-expressions.md) 표와 **일치**시킬 것.

---

## 5. W2 대비 (이번 주 할 일)

| W2 상태 | W3 작업 |
|---------|---------|
| `eve_portrait.png` 없음 | 신규 제작 |
| placeholder·초안만 있음 | 캔버스·팔레트·머리 위치 **주인공 초상과 정렬** 후 교체 |
| 이미 MVP 수준 | 레이 초상([02-ray-portrait](./02-ray-portrait.md)) 제작 시 **레퍼런스 기준**으로 유지 |

---

## 6. 스펙 체크리스트

### 화풍

- [ ] [01-style-guide](../../week1/week1-art/01-style-guide.md) **팔레트 8~16색** 사용
- [ ] 윤곽선 규칙 (1px 등) 주인공 초상과 동일
- [ ] 해상도·캔버스 = `protagonist_portrait.png`와 동일

### 프레이밍

- [ ] 대화 UI 슬롯에 넣었을 때 **머리 위치**가 주인공·레이와 수평 정렬
- [ ] 배경 투명

### 기술

- [ ] Import: **Sprite (2D and UI)**, PPU = 다른 초상과 동일
- [ ] Filter Mode: Point (pixel art)

---

## 7. 작업 단계

1. [ ] W2 산출물·style guide·주인공 초상 레퍼런스 확인
2. [ ] 스케치 → 도트 — 협회 평가원 느낌 (과장 없이)
3. [ ] neutral 표정으로 완성
4. [ ] 주인공 초상과 **나란히** 놓고 크기·머리 위치 비교
5. [ ] `Assets/Art/UI/Portraits/eve_portrait.png` export (기존 파일 **덮어쓰기**)
6. [ ] Dev1에게 **characterId=`eve`**, 경로 공지

---

## 8. 완료 기준

- [ ] `eve_portrait.png`가 지정 경로에 존재
- [ ] 팔레트·캔버스 규격이 style guide·주인공 초상과 일치
- [ ] `001E00002` 대화 UI에서 이브 초상 표시 (금요일 통합)
- [ ] [03-character-expressions](./03-character-expressions.md) 표정 제작 시 **머리 정렬 기준**으로 사용 가능

---

## 9. 관련 문서

- [week2 02-eve-portrait](../../week2/week2-art/02-eve-portrait.md)
- [02-ray-portrait](./02-ray-portrait.md)
- [week3-dev1/05-dialogue-story](../week3-dev1/05-dialogue-story.md)
- [04-story.md](../../../04-story.md) — NPC 표
