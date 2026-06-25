## Lead·Dev 연동

### 이 작업물

Week 1 아트 산출물을 **Lead Factory 씬**·**Dev**에 넘기고, 금요일 데모에서 **주인공 비주얼**이 보이게 한다.

### 전달물 체크리스트

| 수신 | 파일·정보 | 경로 예 |
|------|-----------|---------|
| **Lead** | idle/walk 스프라이트 시트 또는 PNG 묶음 | `Assets/Art/Player/` |
| **Lead** | 피벗·프레임 크기·1칸 높이(px) **텍스트 메모** | `Docs/` 또는 채널 |
| **Lead** | [01-style-guide](./01-style-guide.md) — 맵 타일 placeholder 톤 참고용 | |
| **Dev** | [02-protagonist-portrait](./02-protagonist-portrait.md) PNG | `Assets/Art/UI/` |
| **Dev** | (선택) HUD용 **임시 폰트** 후보 | |

### Lead 쪽 확인 (Art가 요청)

- [ ] `Player` 오브젝트에 Sprite/Animator 교체 후 **Play** 가능
- [ ] WASD 4방향 walk / idle 전환
- [ ] 맵 그리드 칸과 캐릭터 크기 비율 자연스러움

### Dev 쪽 확인

- [ ] 초상화 PNG가 프로젝트에 있음 (UI 연결은 후속)
- [ ] 글로벌 HUD와 시각적 충돌 없음

### 금요일 데모 (Art 관점)

1. [ ] 스타일 가이드 팀 공유 완료
2. [ ] Factory에서 **주인공 walk** 보임
3. [ ] 초상화 파일 Dev·기획 폴더에 있음

### 완료 기준

- [ ] Lead에 모션 에셋 + 피벗 메모 전달
- [ ] Dev에 초상화 PNG 전달
- [ ] 가이드 1p 공유
- [ ] (권장) Lead Factory Play에서 Art 스프라이트 적용 확인
