# 대화 UI·스토리 연출

> **역할**: Dev1 · **Week**: 3 · **Issue**: 05  
> **선행**: W1 주인공 초상 · W3 Art [01-eve-portrait](../week3-art/01-eve-portrait.md) · [표정](../week3-art/03-character-expressions.md)  
> **이벤트**: Lead [06-story-hooks](../week3-lead/06-story-hooks.md)  
> **계약**: [dev-contract.md](../../dev-contract.md) — `StoryEventBus`

---

## 1. 이 작업물

**대화 UI** (초상·이름·본문·다음) + Lead **스토리 이벤트** 구독.  
재생 중 게임·틱 일시정지. 엔딩은 대화만.

**코드**: `Assets/Scripts/GameFlow/DialogueUI.cs`, `StoryEventListener.cs`, `StoryLineDatabase.cs` (선택)

---

## 2. 범위 / 비범위

| 포함 | 제외 |
|------|------|
| `StoryEventBus` 구독 | Lead `Raise` 구현 |
| npcId + expression → 초상 로드 | 대사 기획 작성 (Docs/Story/) |
| 1일차 오프닝 1회 이상 | 전 컷신 카메라 |
| 일시정지 | 튜토 패널 ([01-tutorial-panel](./01-tutorial-panel.md)와 순서 합의) |

---

## 3. UI 스펙

| 요소 | 설명 |
|------|------|
| 초상 슬롯 | Image — Art PNG |
| 이름 | Text — NPC 표시명 |
| 본문 | Text — 대사 (TextArea 여러 줄) |
| 다음 | 버튼 — 다음 줄 또는 닫기 |
| (선택) 자동 진행 | 없음 (MVP) |

### 표정 로드 규칙

[week3-art/03-character-expressions](../week3-art/03-character-expressions.md):

```
Assets/Art/UI/Portraits/{characterId}_portrait.png     → neutral (주인공/이브/레이)
Assets/Art/UI/Portraits/{characterId}_{expressionId}.png
예: eve_serious.png, ray_serious.png
```

```csharp
public void Show(string characterId, string expressionId, string displayName, string body);
```

---

## 4. 이벤트 → 대화 매핑

[04-story.md](../../../04-story.md) — `eventId`당 1행:

| eventId | npcId | expression (예) | 출처 |
|---------|-------|-----------------|------|
| `001E00001` | `protagonist` | `serious` | `Docs/Story/001E00001-Start-Tutorial.md` |
| `001E00002` | `eve` | `serious` | 동일 파일 |
| `001E00006` | `ray` | `serious` | *(3일차)* |

**StoryLineDatabase** (ScriptableObject 또는 JSON):

```csharp
[Serializable]
public class StoryLine {
    public string eventId;
    public string characterId;
    public string expressionId;
    public string displayName;
    [TextArea] public string text;
}
```

---

## 5. StoryEventListener

```csharp
public class StoryEventListener : MonoBehaviour {
    void OnEnable() => StoryEventBus.OnStoryEvent += Handle;
    void OnDisable() => StoryEventBus.OnStoryEvent -= Handle;

    void Handle(string eventId) {
        var line = database.Get(eventId);
        if (line == null) return;
        Time.timeScale = 0f; // 또는 TickManager 정지
        DialogueUI.Show(line);
    }

    public void OnDialogueClosed(string eventId) {
        Time.timeScale = 1f;
        // (선택) Lead에 다음 이벤트 알림
    }
}
```

**Lead만** `StoryEventBus.Raise` — Dev1은 구독만.

---

## 6. 구현 단계

- [ ] `DialogueUI` 프리팹 — Canvas 하위
- [ ] Sprite 로드 — Resources `Portraits/` 또는 직렬화 참조
- [ ] `StoryEventListener` — 씬 DontDestroyOnLoad
- [ ] 최소 `001E00001`, `001E00002` 데이터 입력
- [ ] 대화 중 이동·배치 입력 차단
- [ ] 엔딩 대화 — 결산 없이 UI만 (클리어 플로우)

---

## 7. Art 의존

| 파일 | 필수 시점 |
|------|-----------|
| `protagonist_portrait`, `eve_portrait` | W1/W2 — 즉시 |
| `ray_portrait`, 표정 세트 | W3 금요일 전 — placeholder 허용 |

Placeholder: 단색 PNG + npcId 텍스트.

---

## 8. 검증 시나리오

- [ ] `StoryEventBus.RaiseMock("001E00001")` → 대화 표시
- [ ] expression `serious` → 올바른 PNG
- [ ] 다음 → 패널 닫힘, timeScale 복구
- [ ] 1일차 실플레이 — Lead 훅 연동 후 오프닝 1회
- [ ] 대화 중 생산 틱 정지

---

## 9. 완료 기준

- [ ] §8 통과
- [ ] 초상·표정·이름·본문·다음 전부 동작
- [ ] [06-team-integration](./06-team-integration.md) 「대화 1회」항목

---

## 10. 관련 문서

- [04-story.md](../../../04-story.md)
- [week3-art/03-character-expressions](../week3-art/03-character-expressions.md)
- [01-tutorial-panel](./01-tutorial-panel.md)
