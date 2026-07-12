# 레이 기본 초상화

> **역할**: Art · **Week**: 3 · **Issue**: 02  
> **선행**: [01-style-guide](../../week1/week1-art/01-style-guide.md) · [01-eve-portrait](./01-eve-portrait.md)  
> **후속**: [03-character-expressions](./03-character-expressions.md) · [05-team-integration](./05-team-integration.md)  
> **수신자**: Dev1 (`DialogueUI` 초상 슬롯)

---

## 1. 이 작업물

NPC **레이(Ray)** 의 대화 UI용 **기본(neutral) 초상** PNG 1종.  
마법 테크 담당 직원 — [04-story.md](../../../04-story.md) `npcId: ray`, 일반 플레이 3일차 등장.

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| neutral 표정 1종 | serious 등 추가 표정 → [03-character-expressions](./03-character-expressions.md) |
| 이브·주인공과 동일 화풍 | 전신 일러스트 |
| Dev1 로드 가능한 파일명·경로 | DialogueUI 코드 |

---

## 3. 캐릭터 설정 (기획)

| 항목 | 내용 |
|------|------|
| 이름 | 레이 |
| 역할 | 대장간 직원, 마법 테크 안내 |
| 성격·비주얼 | 기존 NPC(이브)와 톤 맞춤. 마법 테크 느낌 **소품만** 암시 (과하지 않게) |
| 최초 대화 | 3일차 Prepare (스토리 only: 1일차) — 이벤트 `001E00006` |

---

## 4. 산출물

| 항목 | 값 |
|------|-----|
| **경로** | `Assets/Art/UI/Portraits/ray_portrait.png` |
| **형식** | PNG, 알파 채널 |
| **캔버스** | style guide **초상 캔버스** (예: 128×128 또는 96×96) |
| **프레이밍** | 머리~가슴 (이브·주인공과 동일) |
| **표정 id** | `neutral` (기본) |

### Dev1 로드 규칙 (합의)

```csharp
// DialogueUI에서 사용할 id
string characterId = "ray";
string expressionId = "neutral"; // 기본 초상
// → Resources 또는 Addressable: Portraits/ray_portrait.png
// 표정 분리 시: ray_neutral.png — 02에서 serious 추가
```

파일명은 [03-character-expressions](./03-character-expressions.md) 표와 **일치**시킬 것.

---

## 5. 스펙 체크리스트

### 화풍

- [ ] [01-style-guide](../../week1/week1-art/01-style-guide.md) **팔레트 8~16색** 사용
- [ ] 윤곽선 규칙 (1px 등) 이브 초상과 동일
- [ ] 해상도·캔버스 크기 = `eve_portrait.png` / `protagonist_portrait.png`와 동일

### 프레이밍

- [ ] 대화 UI 슬롯에 넣었을 때 **머리 위치**가 다른 NPC와 수평 정렬
- [ ] 배경 투명 — UI 뒤 겹침 없음

### 기술

- [ ] Import Settings: **Sprite (2D and UI)**, PPU = 다른 초상과 동일
- [ ] Filter Mode: Point (pixel art)

---

## 6. 작업 단계

1. [ ] style guide·이브 초상 레퍼런스 열기
2. [ ] 스케치 → 도트 — 레이 식별 가능한 실루엣 (헤어·의상)
3. [ ] neutral 표정으로 완성
4. [ ] 이브 초상과 **나란히** 놓고 크기·머리 위치 비교
5. [ ] `Assets/Art/UI/Portraits/ray_portrait.png` export
6. [ ] Dev1에게 **characterId=`ray`**, 경로 공지

---

## 7. 완료 기준

- [ ] `ray_portrait.png`가 지정 경로에 존재
- [ ] 팔레트·캔버스 규격이 style guide·이브 초상과 일치
- [ ] Dev1 Dialogue UI 테스트 씬에서 슬롯에 정상 표시 (금요일 통합)

---

## 8. 관련 문서

- [03-character-expressions](./03-character-expressions.md) — `ray_serious.png`
- [week3-dev1/05-dialogue-story](../week3-dev1/05-dialogue-story.md)
- [04-story.md](../../../04-story.md) — NPC 표
