# GDC 정적 구현 검사 결과

> 생성 시각: 2026-08-07 07:32:49
> 범위: 소스 코드·에셋·프리팹·Build Settings. 실제 플레이 입력/화면 동작은 수동 PlayMode 검사 필요.

## Build 및 시작 흐름
- [PASS] Build Settings에 활성 씬이 있다 — Assets/DungeonFront.unity
- [FAIL] 게임오버용 Title 씬이 Build Settings에 있다 — 발견하지 못함
- [FAIL] Build 첫 씬이 Factory 시작점이다 — Assets/DungeonFront.unity

## W1 NewGame·인벤토리 정적 검사
- [FAIL] NewGame이 골드 0·명성 0으로 시작한다 — 현재 코드에 gold = 100; reputation = 10;이 있음
- [FAIL] NewGame이 최신 정본의 시작 창고를 지급한다 — Warehouse_1 없음
- [FAIL] NewGame이 실제 PlayerInventory를 초기화한다 — InventoryState만 채우는 구조

## W2~W4 의뢰 데이터 정적 검사
- [PASS] questline.json 의뢰가 47개다 — 현재 47개
- [PASS] questline.json 의뢰 ID가 중복되지 않는다 — 고유 ID 47개
- [PASS] 메인 진행선이 10개다 — 현재 10개
- [PASS] 상시 의뢰가 7개다 — 현재 7개
- [FAIL] 의뢰의 모든 itemId가 실제 ItemDefinition에 있다 — 62종 중 57종 누락. 예: bright_mana_wand, brightsteel_boots, brightsteel_chestplate, brightsteel_helmet, brightsteel_ingot, brightsteel_leggings, brightsteel_sword, concrete

## W3~W5 퀘스트 UI·프리팹 연결 검사
- [PASS] QuestSystemRoot에 GameOverController가 있다 — 발견
- [FAIL] GameOverController에 실제 게임오버 패널이 연결되어 있다 — gameOverPanel 확인
- [FAIL] ShopUI에 목록 루트와 행 프리팹이 연결되어 있다 — listRoot/rowPrefab 확인
- [FAIL] 정식 프리팹에서 F8 QA 패널이 기본 비활성이다 — enableDebugPanel 확인
- [FAIL] 정식 씬에 PerpetualQuestPanel이 배치되어 있다 — 씬 참조 0개
- [FAIL] 정식 씬에 UnlockUI가 배치되어 있다 — 씬 참조 0개

## W3 Dev1·W5 기능 증거 검사
- [PASS] 이름과 무관하게 스토리 이벤트를 받아 게임을 멈추는 대화/튜토리얼 소비자가 있다 — OnStoryEvent 구독 + Time.timeScale 사용을 함께 검사
- [PASS] StoryEventBus를 실제 UI가 구독한다 — OnStoryEvent += 검색
- [PASS] 슬롯 파일 세이브/로드의 최소 증거가 있다 — persistentDataPath + JsonUtility.ToJson + File.WriteAllText 검사
- [PASS] 해상도 설정 코드가 있다 — 런타임 Scripts만 검사
- [PASS] 창 모드 설정 코드가 있다 — 런타임 Scripts만 검사
- [PASS] 볼륨 설정 코드가 있다 — 런타임 Scripts만 검사
- [PASS] ESC 일시정지 코드가 있다 — escapeKey + Time.timeScale 검사

## W5 아이콘 정적 검사
- [FAIL] Dev2 담당 아이콘 21개 이상이 프로젝트에 있다 — 현재 3개
- [FAIL] 16×16 아이콘 21개 이상이 있다 — 현재 3개

