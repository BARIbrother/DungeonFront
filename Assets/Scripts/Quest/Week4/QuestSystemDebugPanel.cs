using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 학습·QA 전용 패널. 별도 Canvas 없이 Game 뷰에서 바로 기능을 눌러 볼 수 있다.
// 실제 게임 UI가 완성되면 프리팹에서 이 컴포넌트만 제거하면 된다.
public class QuestSystemDebugPanel : MonoBehaviour
{
    [Header("개발 중 기능 확인용 - 정식 퀘스트 UI가 아닙니다")]
    [SerializeField] private bool enableDebugPanel;

    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestPool questPool;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private QuestDeadlineController deadlineController;
    [SerializeField] private QuestSaveProvider saveProvider;
    [SerializeField] private PerpetualQuestService perpetualService;
    [SerializeField] private UnlockManager unlockManager;
    [SerializeField] private ShopUI shopUI;

    private readonly List<Quest> perpetualQuests = new();
    private AcceptedQuestSave[] savedQuests;
    private Vector2 scroll;
    private bool visible;

    private void Start()
    {
        if (!enableDebugPanel)
        {
            return;
        }

        ResolveReferences();
        RefreshQuestLists();
    }

    private void OnDestroy()
    {
        ClearPerpetualCopies();
    }

    private void Update()
    {
        if (enableDebugPanel
            && Keyboard.current != null
            && Keyboard.current.f8Key.wasPressedThisFrame)
        {
            visible = !visible;
        }
    }

    private void OnGUI()
    {
        if (!enableDebugPanel || !visible)
        {
            return;
        }

        GUILayout.BeginArea(
            new Rect(15, 15, 470, Screen.height - 30),
            "DungeonFront Quest QA (F8)",
            GUI.skin.window);
        scroll = GUILayout.BeginScrollView(scroll);

        DrawSessionControls();
        DrawAvailableQuests();
        DrawActiveQuests();
        DrawPerpetualQuests();
        DrawShopControls();
        DrawSaveControls();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawSessionControls()
    {
        GUILayout.Label("<b>1. 세션과 풀</b>", RichLabel());
        GameSessionState session = GameSessionState.Instance;
        string phase = session != null ? session.Phase.ToString() : "독립 테스트";
        int day = session != null ? session.day : 1;
        GUILayout.Label(
            $"Day {day} / {phase} / Gold {economy?.Gold ?? 0} / Rep {economy?.Reputation ?? 0}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("NewGame"))
        {
            session?.NewGame();
            RefreshQuestLists();
        }
        if (GUILayout.Button("Gold +500")) economy?.AddGold(500);
        if (GUILayout.Button("Rep +500"))
        {
            economy?.AddReputation(500);
            RefreshQuestLists();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("풀 새로고침")) RefreshQuestLists();
        if (GUILayout.Button("D-day 1 감소")) questManager?.OnDayAdvanced();
        if (GUILayout.Button("D-0 미납 판정")) deadlineController?.EvaluateExpiredQuests();
        GUILayout.EndHorizontal();
    }

    private void DrawAvailableQuests()
    {
        GUILayout.Space(8);
        GUILayout.Label("<b>2. 수락 가능 의뢰</b>", RichLabel());
        if (questManager == null || questManager.availableQuestsToday.Count == 0)
        {
            GUILayout.Label("표시할 의뢰가 없습니다.");
            return;
        }

        foreach (Quest quest in new List<Quest>(questManager.availableQuestsToday))
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{QuestRuntimeRegistry.GetStableId(quest)} / {quest.title} / {QuestCard.FormatDeadline(quest)}");
            GUI.enabled = questManager.CanAcceptQuest(quest);
            if (GUILayout.Button("수락", GUILayout.Width(60)))
            {
                questManager.acceptQuest(quest);
                RefreshQuestLists();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }

    private void DrawActiveQuests()
    {
        GUILayout.Space(8);
        GUILayout.Label("<b>3. 진행 중 의뢰</b>", RichLabel());
        if (questManager == null || questManager.currentQuests.Count == 0)
        {
            GUILayout.Label("수락한 의뢰가 없습니다.");
            return;
        }

        foreach (Quest quest in new List<Quest>(questManager.currentQuests))
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{QuestRuntimeRegistry.GetStableId(quest)} / {quest.title} / {QuestCard.FormatDeadline(quest)}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("요구 재료 지급")) GrantRequirements(quest, 1);
            GUI.enabled = questManager.CanCompleteQuest(quest);
            if (GUILayout.Button("납품"))
            {
                questManager.progressQuest(quest);
                RefreshQuestLists();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }

    private void DrawPerpetualQuests()
    {
        GUILayout.Space(8);
        GUILayout.Label("<b>4. 상시 의뢰</b>", RichLabel());
        if (perpetualQuests.Count == 0)
        {
            GUILayout.Label("상시 의뢰가 없습니다.");
            return;
        }

        foreach (Quest quest in perpetualQuests)
        {
            int maximum = perpetualService != null
                ? perpetualService.GetMaxMultiplier(quest)
                : 0;
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{quest.title} / 최대 x{maximum}");
            if (GUILayout.Button("재료 x2 지급", GUILayout.Width(100)))
            {
                GrantRequirements(quest, 2);
            }
            GUI.enabled = maximum > 0;
            if (GUILayout.Button("x1 납품", GUILayout.Width(70)))
            {
                perpetualService.TryDeliver(quest, 1);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
    }

    private void DrawSaveControls()
    {
        GUILayout.Space(8);
        GUILayout.Label("<b>6. 진행 중 의뢰 저장/복원</b>", RichLabel());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("메모리 저장"))
        {
            savedQuests = saveProvider?.Export();
        }
        if (GUILayout.Button("진행 목록 비우기"))
        {
            questManager?.ClearActive();
        }
        GUI.enabled = savedQuests != null;
        if (GUILayout.Button("메모리 복원"))
        {
            saveProvider?.Import(savedQuests);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.Label("※ 파일 SaveData 연동 전에도 DTO Export/Import를 시험하는 버튼입니다.");
    }

    private void DrawShopControls()
    {
        GUILayout.Space(8);
        GUILayout.Label("<b>5. 상점과 명성 해금</b>", RichLabel());

        PlayerInventory inventory = PlayerInventory.Instance;
        int ironOreCount = inventory != null ? inventory.GetCount("iron_ore") : 0;
        int machineCount = inventory != null ? inventory.Machines.Count : 0;
        GUILayout.Label(
            $"철광석 {ironOreCount}개 / 기계 {machineCount}대 / 제단 해금: {(unlockManager != null && unlockManager.IsUnlocked("Altar_1") ? "완료" : "잠김")}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("철광석 10G 구매"))
        {
            shopUI?.TryPurchase("iron_ore_single");
        }
        if (GUILayout.Button("제단 해금(명성 350)"))
        {
            unlockManager?.TryUnlock("Altar_1");
        }
        if (GUILayout.Button("제단 400G 구매"))
        {
            shopUI?.TryPurchase("altar_1");
        }
        GUILayout.EndHorizontal();
    }

    private void RefreshQuestLists()
    {
        ResolveReferences();
        int reputation = economy != null ? economy.Reputation : 0;
        questPool?.MakeAvailableQuestsToday(reputation);

        ClearPerpetualCopies();
        if (questPool != null)
        {
            perpetualQuests.AddRange(questPool.CreatePerpetualQuestList(reputation));
        }
    }

    private void GrantRequirements(Quest quest, int multiplier)
    {
        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("PlayerInventory가 없어 재료를 지급할 수 없습니다.");
            return;
        }

        foreach (ItemEntry entry
            in quest.requiredItems?.entries ?? System.Array.Empty<ItemEntry>())
        {
            if (entry?.item != null && entry.count > 0)
            {
                inventory.Add(new ItemEntry
                {
                    item = entry.item,
                    count = entry.count * multiplier
                });
            }
        }
    }

    private void ResolveReferences()
    {
        questManager ??= QuestManager.Instance;
        questManager ??= FindAnyObjectByType<QuestManager>();
        questPool ??= FindAnyObjectByType<QuestPool>();
        economy ??= FindAnyObjectByType<Week3EconomyService>();
        deadlineController ??= FindAnyObjectByType<QuestDeadlineController>();
        saveProvider ??= FindAnyObjectByType<QuestSaveProvider>();
        perpetualService ??= FindAnyObjectByType<PerpetualQuestService>();
        unlockManager ??= FindAnyObjectByType<UnlockManager>();
        shopUI ??= FindAnyObjectByType<ShopUI>();
    }

    private void ClearPerpetualCopies()
    {
        foreach (Quest quest in perpetualQuests)
        {
            if (quest != null)
            {
                QuestRuntimeRegistry.Forget(quest);
                Destroy(quest);
            }
        }
        perpetualQuests.Clear();
    }

    private static GUIStyle RichLabel()
    {
        var style = new GUIStyle(GUI.skin.label) { richText = true };
        return style;
    }
}
