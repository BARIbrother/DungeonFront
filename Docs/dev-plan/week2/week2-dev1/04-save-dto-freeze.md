## SaveData DTO 동결

### 이 작업물

W3 **JSON 세이브** 병렬을 위해 `SaveData` 클래스 시그니처를 **W2 종료 시 동결**.  
Dev2는 W3에 `quests[]` 직렬화만 추가한다.

**스키마**: [02-data-structure.md](../../../02-data-structure.md#세이브-데이터) · [dev-contract.md](../../dev-contract.md)

**코드**: `Assets/Scripts/GameFlow/SaveData.cs` (신규)

### 필드 (최소)

| 필드 | W2 담당 | 비고 |
|------|---------|------|
| `version` | Dev1 | 스키마 변경 시 +1 |
| `day`, `phase`, `gold`, `reputation` | Dev1 | |
| `inventoryItems[]` | Dev1 | |
| `machines[]` | Dev1 | 인벤 기계 목록 |
| `factory` | Dev1 | Lead `IFactorySave` — W2는 `null` 허용 |
| `quests[]` | **Dev2 W3** | W2는 빈 배열 |

### 독립 개발

- [ ] Lead `IFactorySave` 미구현 시 `factory = null`로 직렬화
- [ ] Dev2 미구현 시 `quests = []`
- [ ] W2에는 **저장 UI 없음** — DTO + 단위 테스트 또는 `Debug` 저장만

### 완료 기준

- [ ] `SaveData` public 필드·타입 확정, 팀 공지
- [ ] W3 시작 전 시그니처 변경 금지 합의
