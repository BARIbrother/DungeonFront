## 주인공 모션 (idle/walk)

### 이 작업물

Factory 맵에서 **WASD 이동** 시 보이는 **탑다운 캐릭터 스프라이트**.  
**idle**(정지) + **walk**(보행), 각 **4방향** (상·하·좌·우).

Lead [02-player-movement](../week1-lead/02-player-movement.md)의 `SpriteRenderer` / `Animator`에 넣는다.

### 방향 정의 (탑다운)

| 방향 | 키 | 스프라이트 |
|------|-----|------------|
| 위 | W | `walk_up` 등 |
| 아래 | S | `walk_down` |
| 왼쪽 | A | `walk_left` |
| 오른쪽 | D | `walk_right` |

- [ ] **idle** — 방향당 1프레임 (또는 공통 1프레임 + flip)
- [ ] **walk** — 방향당 **2~4프레임** 루프 (Week 1 최소 2프레임)

### 파일 형식 (택 1)

**A. 스프라이트 시트 1장**

- [ ] PNG 시트 + **피벗**·**프레임 크기** 메모 (예: 32×32 per frame)
- [ ] Unity Sprite Editor로 slice 가능한 **균일 그리드**

**B. 개별 PNG**

- [ ] `protagonist_idle_down.png` … 파일명 규칙 표를 Lead에 전달

### 피벗·스케일

- [ ] 캐릭터 **발** 또는 **타일 중심**이 그리드 칸 중앙에 오도록 피벗 (Lead와 합의 후 가이드에 기록)
- [ ] [01-style-guide](./01-style-guide.md) **1칸 높이 px**와 실제 스프라이트 높이 일치

### Lead 연동 체크

- [ ] `Assets/Art/Player/` 등에 import
- [ ] Lead Player Animator Controller에 **4방향 blend** 또는 단순 방향별 상태
- [ ] Play Mode: WASD 시 해당 방향 walk 재생, 키 뗴면 idle

### 없어도 되는 것 (Week 1)

- 수리·작업 클릭·운반 모션
- Unity Animator Controller **세팅** — Lead가 할 수 있음 (Art는 시트 전달만으로도 OK)

### 완료 기준

- [ ] 4방향 idle + 4방향 walk 프레임 존재
- [ ] PNG(시트)가 프로젝트에 import됨
- [ ] Lead 씬에서 이동 시 walk, 정지 시 idle가 보임 (연동 후)
