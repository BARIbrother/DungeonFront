# 대화 UI·스토리 연출

**역할**: Dev1 · **Week**: 3 · **Issue**: 05  
**선행**: W1 주인공 초상 · W3 이브·레이·표정 에셋

## 1. 이 작업물

대화 UI(초상·이름·본문·다음) + 스토리 이벤트 구독.  
재생 중 게임·틱 일시정지. 엔딩은 대화만.

**코드**: `DialogueUI.cs`, `StoryEventListener.cs`, `StoryLineDatabase.cs` (선택)

## 2. UI 스펙

| 요소 | 설명 |
|------|------|
| 초상 | Image |
| 이름 | Text |
| 본문 | Text (여러 줄) |
| 다음 | 버튼 — 다음 줄 또는 닫기 |

### 표정 경로

```
Assets/Art/UI/Portraits/{characterId}_portrait.png     → neutral
Assets/Art/UI/Portraits/{characterId}_{expressionId}.png
예: eve_serious.png, ray_serious.png
```

```csharp
public void Show(string characterId, string expressionId, string displayName, string body);
```

Placeholder: 단색 PNG + npcId 텍스트 허용.

## 3. 이벤트 매핑 (최소)

| eventId | npcId | expression (예) |
|---------|-------|-----------------|
| `001E00001` | `protagonist` | `serious` |
| `001E00002` | `eve` | `serious` |
| `001E00006` | `ray` | `serious` (3일차) |

`StoryEventBus`는 **구독만**. Raise는 Lead.

대화 중: `Time.timeScale=0` 또는 틱 정지 · 이동·배치 입력 차단.  
닫을 때 timeScale/틱 복구.

## 4. 완료 기준

- [ ] 초상·표정·이름·본문·다음 전부 동작
- [ ] `001E00001` · `001E00002` 최소 데이터 재생
- [ ] 대화 중 틱·입력 정지 · 종료 후 복구
- [ ] expression에 맞는 PNG (없으면 placeholder)
- [ ] RaiseMock 또는 실플레이로 오프닝 1회
