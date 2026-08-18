#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 임시 통합 QA. F10으로 열고 닫는다.
/// 기능 확인 후 Assets/Scripts/Temporary 폴더를 통째로 삭제하면 된다.
/// </summary>
public sealed class GdcFeatureTestPanel : MonoBehaviour
{
    private static GdcFeatureTestPanel instance;
    private bool visible;
    private bool testPause;
    private Vector2 scroll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance == null) new GameObject("TEMP_GdcFeatureTestPanel").AddComponent<GdcFeatureTestPanel>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) visible = !visible;
    }

    private void OnDestroy()
    {
        if (testPause) GamePauseService.ReleasePause("TEMP_QA");
    }

    private void OnGUI()
    {
        if (!visible) return;
        GUILayout.BeginArea(new Rect(12,12,430,Mathf.Min(Screen.height-24,760)),"TEMP GDC 통합 테스트 (F10)",GUI.skin.window);
        scroll=GUILayout.BeginScrollView(scroll);
        GameSessionState session=GameSessionState.Instance;
        QuestManager manager=QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
        PlayerInventory inventory=PlayerInventory.Instance ?? FindAnyObjectByType<PlayerInventory>();
        GUILayout.Label($"Day {session?.day ?? 0} / {session?.Phase.ToString() ?? "없음"}");
        GUILayout.Label($"Gold {session?.gold ?? 0} / Rep {session?.reputation ?? 0}");
        GUILayout.Label($"수락 의뢰 {manager?.currentQuests.Count ?? 0}/3 / 기계 {inventory?.Machines.Count ?? 0}");

        GUILayout.Space(8); GUILayout.Label("1. 대사 UI");
        if(GUILayout.Button("오프닝 대사 001E00001")) StoryEventBus.RaiseMock("001E00001");
        if(GUILayout.Button("첫 의뢰 안내 001E00002")) StoryEventBus.RaiseMock("001E00002");
        if(GUILayout.Button("결산 안내 001E00005")) StoryEventBus.RaiseMock("001E00005");

        GUILayout.Space(8); GUILayout.Label("2. 일시정지");
        if(GUILayout.Button(testPause ? "QA 일시정지 해제" : "QA 일시정지 요청"))
        {
            testPause=!testPause;
            if(testPause) GamePauseService.RequestPause("TEMP_QA"); else GamePauseService.ReleasePause("TEMP_QA");
        }
        GUILayout.Label($"Time.timeScale={Time.timeScale} / 서비스 Pause={GamePauseService.IsPaused}");

        GUILayout.Space(8); GUILayout.Label("3. 퀘스트 카드·납품");
        if(GUILayout.Button("첫 수락 의뢰의 요구 재료 지급")) GrantFirstQuestRequirements(manager,inventory);
        if(GUILayout.Button("Gold +500 / Rep +500")){session?.AddGold(500);session?.AddReputation(500);}

        GUILayout.Space(8); GUILayout.Label("4. 페이즈 이동");
        if(GUILayout.Button("Prepare → Production")) session?.StartProduction();
        if(GUILayout.Button("Production → Settlement")) session?.SetPhase(GamePhase.Settlement);
        if(GUILayout.Button("Settlement → 다음 날 Prepare")) session?.AdvanceDay();

        GUILayout.Space(8); GUILayout.Label("5. 저장");
        if(GUILayout.Button("슬롯 0 저장")) GameSaveService.Save(0);
        if(GUILayout.Button("슬롯 0 불러오기")) GameSaveService.Load(0);

        GUILayout.Space(8); GUILayout.Label("6. 게임오버");
        if(GUILayout.Button("게임오버 화면 강제 표시")) GameOverController.Instance?.TriggerGameOver("필수 의뢰를 완료하지 못했습니다");
        GUILayout.EndScrollView(); GUILayout.EndArea();
    }

    private static void GrantFirstQuestRequirements(QuestManager manager,PlayerInventory inventory)
    {
        if(manager==null||inventory==null||manager.currentQuests.Count==0){Debug.LogWarning("[TEMP QA] 먼저 의뢰를 수락하세요.");return;}
        Quest quest=manager.currentQuests[0];
        foreach(ItemEntry entry in quest.requiredItems?.entries ?? System.Array.Empty<ItemEntry>())
        {
            if(entry?.item!=null&&entry.count>0) inventory.Add(new ItemEntry{item=entry.item,count=entry.count});
        }
    }
}
#endif
