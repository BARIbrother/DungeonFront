## MVP 기계 10종+ 목록 초안

### 이 작업물

MVP 완료 조건 **「10종 이상의 기계」**를 채우기 위한 **기획 표 v0.1**.  
이번 주는 **이름·역할·수작업·해금 시점**만 확정한다. 레시피 틱·입출력 수치는 후속.

**산출물 (완료)**: [planning/03-machine-plan.md](../../planning/03-machine-plan.md)

### 표에 넣을 열

| 열 | 설명 |
|----|------|
| # | 1, 2, 3… |
| machineTypeId | `02-data-structure.md` type 문자열 |
| 표시명 (1티어) | UI용 한글명 |
| 역할 | 한 줄 (채취, 제련, 가공, 물류…) |
| 수작업 | O / X (클릭 가공 여부) |
| 해금 (초안) | `시작`, `명성 N`, `의뢰 ID` 등 |

### 반드시 결정할 것

- [ ] **출력기**·**벨트**를 10종 **안에 포함**하는지 표 하단에 명시 (`00-vision.md` MVP 포함) — [03-machine-plan](../../planning/03-machine-plan.md)에서 미정
- [x] 4~10+번째 기계 **이름·역할** 채우기
- [x] `machineDefId` 명명 = `{machineTypeId}_{tier}` 와 [03-machine-prefabs](./03-machine-prefabs.md) 일치

### 완료 기준

- [x] **10행 이상** (type 기준 서로 다른 기계)이 적힌 표 1장 → [03-machine-plan.md](../../planning/03-machine-plan.md)
- [ ] 팀 채널/회의에서 v0.1 **공유 완료** (날짜 기록 불필요)
