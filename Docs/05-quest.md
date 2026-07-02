# Quest (의뢰)

의뢰 정적 데이터를 담는 ScriptableObject. `Recipe`와 동일하게 `ItemEntryList`로 요구 품목을 표현한다.

## 필드

| 필드 | 타입 | 설명 |
|------|------|------|
| `title` | `string` | 의뢰 제목 |
| `clientName` | `string` | 의뢰인 이름 |
| `content` | `string` | 의뢰 내용 (본문) |
| `requiredItems` | `ItemEntryList` | 납품해야 할 품목 목록 |

## 에셋 생성

1. Project 창에서 우클릭
2. **Create → DungeonFront → Quest**
3. Inspector에서 필드 입력
4. `requiredItems.length` 설정 후 **Resize**, 각 `entries`에 `Item`과 `count` 지정

## JSON 로더 (예정)

의뢰 수가 늘어나면 `StreamingAssets/Quests/` 등에 JSON을 두고, 게임 시작 시 로드하는 방식으로 확장한다.
SO 클래스(`Quest`)는 그대로 두고, 로더가 JSON을 읽어 런타임 `Quest` 인스턴스를 만들거나 SO를 갱신한다.

### JSON 스키마

```json
{
  "title": "철광석 납품",
  "clientName": "대장장이",
  "content": "제련소에 철광석이 부족합니다. 10개만 구해 주세요.",
  "requiredItems": [
    { "itemId": "iron_ore", "count": 10 }
  ]
}
```

| JSON 필드 | SO 필드 | 비고 |
|-----------|---------|------|
| `title` | `title` | |
| `clientName` | `clientName` | |
| `content` | `content` | |
| `requiredItems[].itemId` | `entries[].item.id` | 문자열 ID로 `Item` 조회 필요 |
| `requiredItems[].count` | `entries[].count` | |

### 로더 구현 시 참고

- `Item`은 plain class이므로 JSON에서는 `itemId` 문자열로 참조하고, 로더에서 아이템 DB(레지스트리)로 `Item`을 해석한다.
- `ItemEntryList.length`와 `entries` 배열 길이를 맞춘 뒤 `Resize()` 없이 직접 채운다.
- 로더 스크립트는 `Assets/Scripts/Quest/QuestLoader.cs` 등으로 추가한다.
- JSON 파일 위치·로드 시점(초기화 씬, 메뉴 진입 등)은 구현 단계에서 정한다.
