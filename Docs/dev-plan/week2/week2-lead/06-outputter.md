## 출력기 (Outputter)

### 이 작업물

**출력기** 기계 — 인접 기계의 **입·출력 포트**에 **어느 방향에서든** 접근·중계.  
기존 「포트 방향 회전·뒤→앞 이송」 기획 **폐기**.

**코드**: `Assets/Scripts/Machine/` (예: `Outputter.cs`, `OccupantKind.Outputter` 이미 존재)  
**기획**: `00-vision.md` 물류 — 출력기는 **별도 기계 종류**

### 규격 (변경)

| 항목 | 내용 |
|------|------|
| 출력기 역할 | 그리드에 배치한 **물류 허브** — 옆 칸 기계의 `inputPort` / `outputPort`와 중계 |
| 방향 | 기계 본체 포트는 **방향 없음** (내부 버퍼). 출력기는 **4방향 인접** 어느 면이든 해당 기계 포트에 연결 |
| 이송 | 인접 기계 출력 → 출력기 → 인접 기계 입력 (또는 벨트·수동과 조합) |
| 없을 때 | 출력기·벨트 없으면 플레이어 **수동 운반** ([week3 manual](../../week3/week3-lead/03-manual-interaction.md)) |

### 구현

- [ ] `Outputter` : `Machine` 서브클래스, `GetOccupantKind()` → `Outputter`
- [ ] 배치·[01-placement-mode](./01-placement-mode.md) 지원
- [ ] 틱마다 **4방향 인접** `Machine` 스캔 → `outputPort` → 출력기 → 대상 `inputPort` (`PutintoInputPort`)
- [ ] Prefab·6종+ 목록에 **출력기** 포함 ([04-machines-6plus](./04-machines-6plus.md))
- [ ] [03-production-tick](./03-production-tick.md) 버퍼·틱과 연동

### Week 2 범위

출력기 **1종** 동작 + 철 체인 1구간 이상 연결. **벨트**는 Week 3 [02-logistics](../../week3/week3-lead/02-logistics.md).

### 완료 기준

- [ ] 출력기 배치 후 인접 용광로·채굴기 등과 **방향 무관** 아이템 중계
- [ ] 생산 틱 중 출력기가 1회 이상 이송
