## 주인공 수리·작업 모션

### 이 작업물

Factory **고장 수리**·**수작업(제작기 클릭)** 시 탑다운 모션.  
[week1 03-protagonist-motions](../../week1/week1-art/03-protagonist-motions.md) idle/walk에 **추가**.

### 모션 종류

| 이름 | 용도 | 방향 |
|------|------|------|
| **repair** | 고장 기계 근접 수리 클릭 | 4방향 또는 하향 1종 + flip |
| **work** | 수작업 기계 클릭 가공 | 4방향 또는 하향 1종 + flip |

- [ ] 루프 **2~4프레임** (Week 2 최소 2)
- [ ] [01-style-guide](../../week1/week1-art/01-style-guide.md) 팔레트·1칸 높이 유지

### Lead 연동

- [ ] Animator 파라미터명 전달 (예: `Repair`, `Work`, `Facing`)
- [ ] `Assets/Art/Characters/Protagonist/` import

### 완료 기준

- [ ] repair·work 스프라이트(시트) 존재
- [ ] Lead에 시트 + 파라미터 메모 전달
