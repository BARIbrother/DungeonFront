## 플레이어 WASD 이동·스폰

### 이 작업물

생산 단계 5분 동안 **탑다운으로 캐릭터를 직접 조작**한다. Week 1은 **이동 + 스폰**만 구현한다 (클릭·수리·운반은 후속).

**코드**: `Assets/Scripts/PlayerMovement.cs` (기존 스켈레톤 확장)  
**씬**: `Factory` — [01-grid-map](./01-grid-map.md)과 동일 씬

### 플레이어 오브젝트 구조

```
Player (GameObject)
├── SpriteRenderer   ← Art 모션 시트로 교체
├── Animator         ← idle/walk 4방향 (Art 연동)
└── PlayerMovement   ← WASD 입력
```

- [ ] `SpriteRenderer` + `Animator` — 아트 **idle/walk** 스프라이트 시트만 갈아끼우면 동작하게
- [ ] placeholder: 단색 사각형·Capsule 등으로 먼저 구현 가능

### 이동 규격

| 항목 | 내용 |
|------|------|
| 입력 | **WASD** (방향키 대체 가능) |
| 방식 | 탑다운 **4방향** (대각선 없음 또는 있음 — 팀 선택 후 고정) |
| 물리 | `Rigidbody2D` **또는** `Transform` 직접 이동 중 하나 |
| 경계 | [01-grid-map](./01-grid-map.md) 맵 범위 **밖으로 나가지 않음** (클램프) |
| Y축 | 2D면 Y 고정 / 3D 탑다운이면 Y 또는 Z 고정 |

### NewGame 스폰

- [ ] Dev `NewGame()` 완료 시 호출할 **public 메서드** 또는 이벤트 구독  
  예: `PlayerSpawner.OnNewGame()` → `GridMap` 중앙 좌표로 `transform.position` 설정
- [ ] 스폰 위치 = [01-grid-map](./01-grid-map.md)에서 정한 **맵 중앙** 월드 좌표
- [ ] 매 `NewGame`마다 같은 위치로 리셋

### 카메라 (선택)

- [ ] **A**: 카메라 고정 — 맵 전체가 보임  
- [ ] **B**: 플레이어 follow  
→ Week 1은 A 또는 B 중 하나만 구현해도 됨

### 완료 기준

- [ ] Play Mode에서 WASD로 맵 안을 돌아다닐 수 있다
- [ ] 맵 밖으로 나가지 않는다
- [ ] Dev에서 `NewGame` 트리거 시 플레이어가 **맵 중앙**에 나타난다
- [ ] SpriteRenderer에 다른 Sprite를 넣으면 비주얼만 바뀌고 이동은 유지된다
