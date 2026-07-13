# 캐릭터별 표정

> **역할**: Art · **Week**: 3 · **Issue**: 03  
> **선행**: W1 주인공 초상 · [01-eve-portrait](./01-eve-portrait.md) · [02-ray-portrait](./02-ray-portrait.md)  
> **후속**: [05-team-integration](./05-team-integration.md)  
> **수신자**: Dev1 `DialogueUI`

---

## 1. 이 작업물

대화 UI에서 **캐릭터·표정 id**로 교체할 **표정 variation PNG** 세트.  
전신 초상 신규 제작 대신, 기존 초상(또는 레이 기본)과 **머리 위치·크기를 맞춘** 표정만 추가한다.

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| 아래 표의 최소 표정 PNG | 신규 NPC 전신 초상 |
| 파일명·경로 규격 | 애니메이션·립싱크 · C# 로더 구현 |

---

## 3. 산출물 — 전체 파일 목록

**경로**: `Assets/Art/UI/Portraits/`

### 3-1. 기본 초상 (이미 있거나 이번 주 제작)

| characterId | expressionId | 파일명 | 출처 |
|-------------|--------------|--------|------|
| `protagonist` | `neutral` | `protagonist_portrait.png` | W1 |
| `eve` | `neutral` | `eve_portrait.png` | [01-eve-portrait](./01-eve-portrait.md) |
| `ray` | `neutral` | `ray_portrait.png` | [02-ray-portrait](./02-ray-portrait.md) |

### 3-2. 추가 표정 (이번 Issue)

| characterId | expressionId | 파일명 | 용도 예 |
|-------------|--------------|--------|---------|
| `protagonist` | `happy` | `protagonist_happy.png` | 면허 취득·칭찬 |
| `protagonist` | `serious` | `protagonist_serious.png` | 결심·튜토 |
| `eve` | `happy` | `eve_happy.png` | 의뢰 완료 |
| `eve` | `serious` | `eve_serious.png` | 평가·경고 |
| `ray` | `serious` | `ray_serious.png` | 마법 테크 설명 |

**최소 산출**: 위 **5 PNG** (기본 초상 3종 제외).

---

## 4. 제작 규칙

| 규칙 | 설명 |
|------|------|
| 캔버스 | style guide 초상 크기 **고정** (예: 128×128) |
| 정렬 | 동일 characterId 표정 간 **눈·코·입 위치**만 변경, 머리 외곽·어깨 라인 **고정** |
| 팔레트 | [01-style-guide](../../week1/week1-art/01-style-guide.md) 준수 |

### 레이어 작업 팁

1. 기본 초상 PSD에서 표정 레이어만 복제
2. 머리 윤곽 locked → 눈·입만 수정
3. export 시 **동일 캔버스 크기** 유지

---

## 5. Dev1 연동 규격

[week3-dev1/05-dialogue-story](../week3-dev1/05-dialogue-story.md)에서 기대하는 API:

```csharp
// 스토리 데이터 예 (Lead/Dev1)
// eventId 001E00002 → npcId=eve, expression=serious
DialogueUI.Show("eve", "serious", "이브", "첫 의뢰를 확인하세요.");
```

**로드 규칙** (Dev1 구현 예시):

```
경로: Assets/Art/UI/Portraits/{characterId}_{expressionId}.png
예외: neutral이고 protagonist/eve/ray면 {characterId}_portrait.png 허용
```

Art는 위 규칙에 맞는 **파일명**만 맞추면 됨.

### 스토리 이벤트 ↔ 표정 매핑 (참고)

| eventId (예) | npcId | expression |
|--------------|-------|------------|
| `001E00001` | `protagonist` | `serious` |
| `001E00002` | `eve` | `serious` |
| `001E00006` | `ray` | `serious` |
| 엔딩 | `protagonist` | `happy` |

상세 대사: `Docs/Story/` — Art는 **표정만** 맞추면 됨.

---

## 6. 작업 단계

- [ ] 기본 초상 3종 존재 확인 (주인공·이브·레이)
- [ ] 주인공 happy/serious — `protagonist_portrait` 정렬 기준
- [ ] 이브 happy/serious — `eve_portrait` 정렬 기준
- [ ] 레이 serious — `ray_portrait` 정렬 기준
- [ ] 전체를 Dialogue 슬롯 mock(256×256)에 나란히 배치해 **깜빡임 없이** 교체 가능한지 확인
- [ ] [05-team-integration](./05-team-integration.md) 전달

---

## 7. 완료 기준

- [ ] §3 표의 **추가 표정 PNG 전부** 존재
- [ ] 동일 캐릭터 표정 간 머리 위치·크기 통일
- [ ] style guide 팔레트 일치
- [ ] Dev1이 `characterId` + `expressionId`로 로드 성공 (통합 데모)

---

## 8. 관련 문서

- [01-style-guide](../../week1/week1-art/01-style-guide.md)
- [04-story.md](../../../04-story.md) — npcId
- [week3-dev1/05-dialogue-story](../week3-dev1/05-dialogue-story.md)
