using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
#endif

// Dev Mode 패널 (F8). 에디터·개발 빌드에서만 동작한다.
public class QuestSystemDebugPanel : MonoBehaviour
{
    [Header("Dev Mode (F8)")]
    [FormerlySerializedAs("enableDebugPanel")]
    [SerializeField] private bool enableDevMode = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static QuestSystemDebugPanel instance;

    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestPool questPool;
    [SerializeField] private Week3EconomyService economy;
    [SerializeField] private QuestDeadlineController deadlineController;
    [SerializeField] private QuestSaveProvider saveProvider;
    [SerializeField] private PerpetualQuestService perpetualService;
    [SerializeField] private QuestUnlockManager unlockManager;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private QuestDatabase questDatabase;

    private enum DevTab
    {
        Session,
        Quests,
        Inventory,
        Story,
        Smoke
    }

    private readonly List<Quest> perpetualQuests = new();
    private AcceptedQuestSave[] savedQuests;
    private Quest[] catalogQuests = System.Array.Empty<Quest>();
    private Vector2 scroll;
    private Vector2 catalogScroll;
    private bool visible;
    private DevTab tab = DevTab.Session;

    private string dayInput = "1";
    private string goldInput = "100";
    private string repInput = "10";
    private string grantItemId = "iron_ore";
    private string grantCountInput = "10";
    private string grantLevelInput = "1";
    private string grantAllMachinesCountInput = "1";
    private string storyIdInput = "001E00001";
    private string jumpMainId = "00100002";
    private string lastStatus = string.Empty;
    private string smokeSummary = string.Empty;

    // QuestSystemRoot 프리팹은 GDC용으로 enableDevMode가 꺼져 있어 F8이 막힌다.
    // 에디터·개발 빌드에서는 별도 패널을 띄워 F8을 연다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        var root = new GameObject("DevModePanel");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<QuestSystemDebugPanel>();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        ClearPerpetualCopies();
    }

    private void Start()
    {
        if (!IsEnabled)
        {
            return;
        }

        ResolveReferences();
        RefreshQuestLists();
        RefreshCatalog();
    }

    private void Update()
    {
        if (instance != this
            || !IsEnabled
            || Keyboard.current == null
            || !Keyboard.current.f8Key.wasPressedThisFrame)
        {
            return;
        }

        visible = !visible;
        if (visible)
        {
            ResolveReferences();
            SyncEconomyFields();
        }
    }

    private bool IsEnabled => enableDevMode;

    private void OnGUI()
    {
        if (instance != this || !IsEnabled || !visible)
        {
            return;
        }

        GUILayout.BeginArea(
            new Rect(15, 15, 520, Screen.height - 30),
            "DungeonFront Dev Mode (F8)",
            GUI.skin.window);
        scroll = GUILayout.BeginScrollView(scroll);

        DrawStatusBar();
        DrawTabs();
        GUILayout.Space(6);

        switch (tab)
        {
            case DevTab.Session:
                DrawSessionTab();
                break;
            case DevTab.Quests:
                DrawQuestsTab();
                break;
            case DevTab.Inventory:
                DrawInventoryTab();
                break;
            case DevTab.Story:
                DrawStoryTab();
                break;
            case DevTab.Smoke:
                DrawSmokeTab();
                break;
        }

        if (!string.IsNullOrEmpty(lastStatus))
        {
            GUILayout.Space(8);
            GUILayout.Label("<b>Status</b>", RichLabel());
            GUILayout.Label(lastStatus);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawStatusBar()
    {
        GameSessionState session = GameSessionState.Instance;
        string phase = session != null ? session.Phase.ToString() : "독립 테스트";
        int day = session != null ? session.day : 1;
        int active = questManager != null ? questManager.currentQuests.Count : 0;
        GUILayout.Label(
            $"Day {day} / {phase} / Gold {economy?.Gold ?? session?.gold ?? 0} / "
            + $"Rep {economy?.Reputation ?? session?.reputation ?? 0} / Active {active}"
            + (session != null ? $" / TestMode={(session.IsTestMode ? "ON" : "OFF")}" : string.Empty),
            RichLabel());
    }

    private void DrawTabs()
    {
        GUILayout.BeginHorizontal();
        DrawTabButton("Session", DevTab.Session);
        DrawTabButton("Quests", DevTab.Quests);
        DrawTabButton("Inventory", DevTab.Inventory);
        DrawTabButton("Story", DevTab.Story);
        DrawTabButton("Smoke", DevTab.Smoke);
        GUILayout.EndHorizontal();
    }

    private void DrawTabButton(string label, DevTab value)
    {
        GUI.backgroundColor = tab == value ? Color.cyan : Color.white;
        if (GUILayout.Button(label))
        {
            tab = value;
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawSessionTab()
    {
        GUILayout.Label("<b>Session</b>", RichLabel());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("NewGame"))
        {
            DevModeCommands.NewGame();
            RefreshQuestLists();
            SyncEconomyFields();
            SetStatus("NewGame");
        }

        if (GUILayout.Button("Prepare"))
        {
            DevModeCommands.ForcePhase(GamePhase.Prepare);
            SetStatus("ForcePhase Prepare");
        }

        if (GUILayout.Button("Production"))
        {
            DevModeCommands.ForcePhase(GamePhase.Production);
            SetStatus("ForcePhase Production");
        }

        if (GUILayout.Button("Settlement"))
        {
            DevModeCommands.ForcePhase(GamePhase.Settlement);
            SetStatus("ForcePhase Settlement");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Day", GUILayout.Width(40));
        dayInput = GUILayout.TextField(dayInput, GUILayout.Width(60));
        if (GUILayout.Button("Set Day", GUILayout.Width(80)))
        {
            if (int.TryParse(dayInput, out int day))
            {
                DevModeCommands.SetDay(day);
                SetStatus($"SetDay {day}");
            }
        }

        if (GUILayout.Button("Day +1", GUILayout.Width(60)))
        {
            GameSessionState session = GameSessionState.Instance;
            if (session != null)
            {
                DevModeCommands.SetDay(session.day + 1);
                dayInput = session.day.ToString();
            }
        }

        if (GUILayout.Button("Day -1", GUILayout.Width(60)))
        {
            GameSessionState session = GameSessionState.Instance;
            if (session != null)
            {
                DevModeCommands.SetDay(session.day - 1);
                dayInput = session.day.ToString();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Gold", GUILayout.Width(40));
        goldInput = GUILayout.TextField(goldInput, GUILayout.Width(80));
        GUILayout.Label("Rep", GUILayout.Width(30));
        repInput = GUILayout.TextField(repInput, GUILayout.Width(80));
        if (GUILayout.Button("Set", GUILayout.Width(50)))
        {
            if (int.TryParse(goldInput, out int gold)
                && int.TryParse(repInput, out int rep))
            {
                DevModeCommands.SetEconomy(economy, gold, rep);
                RefreshQuestLists();
                SetStatus($"Economy Gold={gold} Rep={rep}");
            }
        }

        if (GUILayout.Button("G+500", GUILayout.Width(60)))
        {
            economy?.AddGold(500);
            SyncEconomyFields();
        }

        if (GUILayout.Button("R+500", GUILayout.Width(60)))
        {
            economy?.AddReputation(500);
            SyncEconomyFields();
            RefreshQuestLists();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("<b>Shop / Unlock (legacy)</b>", RichLabel());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("철광석 10G"))
        {
            shopUI?.TryPurchase("iron_ore_single");
        }

        if (GUILayout.Button("제단 해금"))
        {
            unlockManager?.TryUnlock("Altar_1");
        }

        if (GUILayout.Button("제단 구매"))
        {
            shopUI?.TryPurchase("altar_1");
        }
        GUILayout.EndHorizontal();

        DrawAudioDiagnostics();
    }

    private const string AudioVolumePrefsKey = "Settings.MasterVolume";

    private void DrawAudioDiagnostics()
    {
        GUILayout.Space(8);
        GUILayout.Label("<b>Audio</b>", RichLabel());

        AudioManager audio = AudioManager.Instance;
        AudioCatalog resolved = audio != null ? audio.Catalog : null;
        bool hasManager = audio != null;
        bool hasCatalog = resolved != null;
        float prefsVolume = PlayerPrefs.GetFloat(AudioVolumePrefsKey, -1f);
        string prefsText = prefsVolume < 0f ? "(없음→기본 1)" : prefsVolume.ToString("0.###");

        GUILayout.Label(
            $"Listener={AudioListener.volume:0.###} / Prefs[{AudioVolumePrefsKey}]={prefsText}",
            RichLabel());
        GUILayout.Label(
            $"AudioManager={(hasManager ? "OK" : "NULL")} / Catalog={(hasCatalog ? "OK" : "NULL")}"
            + (hasCatalog
                ? $" / prepare={(resolved.prepare?.clip != null ? "clip" : "null")}"
                  + $" / uiClick={(resolved.uiClick?.clip != null ? "clip" : "null")}"
                : "  ← Manager가 NULL이면 씬 컴포넌트 Missing Script 가능"),
            RichLabel());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("강제 생성/복구"))
        {
            SetStatus(AudioManager.ForceRebuildCatalog());
        }

        if (GUILayout.Button("음량 Max(1)"))
        {
            AudioListener.volume = 1f;
            PlayerPrefs.SetFloat(AudioVolumePrefsKey, 1f);
            PlayerPrefs.Save();
            SetStatus("AudioListener.volume=1, Prefs 저장");
        }

        if (GUILayout.Button("Prefs 삭제"))
        {
            PlayerPrefs.DeleteKey(AudioVolumePrefsKey);
            PlayerPrefs.Save();
            AudioListener.volume = 1f;
            SetStatus("Settings.MasterVolume Prefs 삭제, Listener=1");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("uiClick 테스트"))
        {
            AudioManager.EnsureExists();
            audio = AudioManager.Instance;
            resolved = audio != null ? audio.Catalog : null;
            if (resolved?.uiClick?.clip == null)
            {
                SetStatus("uiClick 재생 실패: Catalog/clip 없음 → 강제 생성/복구 먼저");
            }
            else
            {
                audio.PlaySfx(resolved.uiClick);
                SetStatus(
                    $"uiClick PlaySfx 호출 (Listener={AudioListener.volume:0.###})");
            }
        }

        if (GUILayout.Button("Prepare BGM"))
        {
            AudioManager.EnsureExists();
            audio = AudioManager.Instance;
            resolved = audio != null ? audio.Catalog : null;
            if (resolved?.prepare?.clip == null)
            {
                SetStatus("Prepare BGM 실패: Catalog/clip 없음 → 강제 생성/복구 먼저");
            }
            else
            {
                audio.PlayBgm(resolved.prepare);
                SetStatus(
                    $"Prepare BGM Play 호출 (Listener={AudioListener.volume:0.###})");
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawQuestsTab()
    {
        GUILayout.Label("<b>Quests</b>", RichLabel());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("풀 새로고침"))
        {
            RefreshQuestLists();
            SetStatus("풀 새로고침");
        }

        if (GUILayout.Button("카탈로그 새로고침"))
        {
            RefreshCatalog();
            SetStatus($"카탈로그 {catalogQuests.Length}개");
        }

        if (GUILayout.Button("D-day -1"))
        {
            questManager?.OnDayAdvanced();
        }

        if (GUILayout.Button("D-0 미납"))
        {
            deadlineController?.EvaluateExpiredQuests();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label("<b>진행도</b>", RichLabel());
        QuestProgressionService progression = QuestProgressionService.Instance
            ?? FindAnyObjectByType<QuestProgressionService>();
        if (progression != null)
        {
            GUILayout.Label(
                "완료: "
                + string.Join(", ", progression.CompletedQuestIds));
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("메인까지 완료", GUILayout.Width(100));
        jumpMainId = GUILayout.TextField(jumpMainId, GUILayout.Width(100));
        if (GUILayout.Button("Restore", GUILayout.Width(70)))
        {
            DevModeCommands.RestoreProgressionThroughMain(jumpMainId.Trim());
            RefreshQuestLists();
            SetStatus($"Progression through {jumpMainId}");
        }

        if (GUILayout.Button("Reset", GUILayout.Width(60)))
        {
            DevModeCommands.ResetProgression();
            RefreshQuestLists();
            SetStatus("Progression reset");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label("<b>SO 카탈로그 (강제 오퍼/수락)</b>", RichLabel());
        catalogScroll = GUILayout.BeginScrollView(catalogScroll, GUILayout.Height(160));
        foreach (Quest quest in catalogQuests)
        {
            if (quest == null)
            {
                continue;
            }

            string id = DevModeCommands.ResolveId(quest);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"{id} / {quest.title}");
            GUILayout.Label($"보상: {DevModeCommands.FormatRewardPreview(quest)}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("오퍼", GUILayout.Width(60)))
            {
                if (DevModeCommands.OfferQuest(questManager, quest, out _, out string error))
                {
                    SetStatus($"오퍼 {id}");
                    RefreshQuestLists();
                }
                else
                {
                    SetStatus(error);
                }
            }

            if (GUILayout.Button("수락", GUILayout.Width(60)))
            {
                if (DevModeCommands.OfferAndAccept(questManager, quest, out string error))
                {
                    SetStatus($"수락 {id}");
                    RefreshQuestLists();
                }
                else
                {
                    SetStatus(error);
                }
            }

            if (GUILayout.Button("원클릭 납품", GUILayout.Width(90)))
            {
                if (DevModeCommands.OfferAndAccept(questManager, quest, out string acceptError))
                {
                    Quest active = questManager.currentQuests.Find(candidate =>
                        DevModeCommands.ResolveId(candidate) == id);
                    string deliverError = active == null
                        ? "수락 후 활성 목록에 없음"
                        : null;
                    bool delivered = active != null
                        && DevModeCommands.GrantAndDeliver(
                            questManager,
                            active,
                            out deliverError);
                    if (delivered)
                    {
                        SetStatus($"원클릭 완료 {id}");
                    }
                    else
                    {
                        SetStatus(deliverError ?? acceptError ?? "원클릭 실패");
                    }

                    RefreshQuestLists();
                }
                else
                {
                    SetStatus(acceptError);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();

        GUILayout.Space(4);
        GUILayout.Label("<b>오늘 수락 가능</b>", RichLabel());
        if (questManager == null || questManager.availableQuestsToday.Count == 0)
        {
            GUILayout.Label("없음");
        }
        else
        {
            foreach (Quest quest in new List<Quest>(questManager.availableQuestsToday))
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(
                    $"{DevModeCommands.ResolveId(quest)} / {quest.title} / {QuestCard.FormatDeadline(quest)}");
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

        GUILayout.Space(4);
        GUILayout.Label("<b>진행 중</b>", RichLabel());
        if (questManager == null || questManager.currentQuests.Count == 0)
        {
            GUILayout.Label("없음");
        }
        else
        {
            foreach (Quest quest in new List<Quest>(questManager.currentQuests))
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    $"{DevModeCommands.ResolveId(quest)} / {quest.title} / {QuestCard.FormatDeadline(quest)}");
                GUILayout.Label($"보상: {DevModeCommands.FormatRewardPreview(quest)}");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("요구 지급"))
                {
                    DevModeCommands.GrantRequirements(quest, 1);
                }

                GUI.enabled = questManager.CanCompleteQuest(quest);
                if (GUILayout.Button("납품"))
                {
                    questManager.progressQuest(quest);
                    RefreshQuestLists();
                }

                GUI.enabled = true;
                if (GUILayout.Button("원클릭"))
                {
                    if (DevModeCommands.GrantAndDeliver(questManager, quest, out string error))
                    {
                        SetStatus($"납품 완료 {DevModeCommands.ResolveId(quest)}");
                    }
                    else
                    {
                        SetStatus(error);
                    }

                    RefreshQuestLists();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        GUILayout.Space(4);
        GUILayout.Label("<b>상시</b>", RichLabel());
        if (perpetualQuests.Count == 0)
        {
            GUILayout.Label("없음");
        }
        else
        {
            foreach (Quest quest in perpetualQuests)
            {
                int maximum = perpetualService != null
                    ? perpetualService.GetMaxMultiplier(quest)
                    : 0;
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{quest.title} / max x{maximum}");
                if (GUILayout.Button("재료 x2", GUILayout.Width(70)))
                {
                    DevModeCommands.GrantRequirements(quest, 2);
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

        GUILayout.Space(4);
        GUILayout.Label("<b>세이브(메모리)</b>", RichLabel());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Export"))
        {
            savedQuests = saveProvider?.Export();
            SetStatus("Export");
        }

        if (GUILayout.Button("ClearActive"))
        {
            questManager?.ClearActive();
            RefreshQuestLists();
        }

        GUI.enabled = savedQuests != null;
        if (GUILayout.Button("Import"))
        {
            saveProvider?.Import(savedQuests);
            RefreshQuestLists();
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawInventoryTab()
    {
        GUILayout.Label("<b>Inventory</b>", RichLabel());

        GUILayout.BeginHorizontal();
        GUILayout.Label("itemId", GUILayout.Width(50));
        grantItemId = GUILayout.TextField(grantItemId);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("count", GUILayout.Width(50));
        grantCountInput = GUILayout.TextField(grantCountInput, GUILayout.Width(60));
        GUILayout.Label("level", GUILayout.Width(40));
        grantLevelInput = GUILayout.TextField(grantLevelInput, GUILayout.Width(40));
        if (GUILayout.Button("지급", GUILayout.Width(60)))
        {
            if (int.TryParse(grantCountInput, out int count)
                && int.TryParse(grantLevelInput, out int level))
            {
                DevModeCommands.GrantItem(grantItemId.Trim(), count, level);
                SetStatus($"Grant {grantItemId} x{count} lv{level}");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("활성 의뢰 요구 일괄 지급"))
        {
            DevModeCommands.GrantActiveQuestRequirements(questManager);
            SetStatus("활성 의뢰 요구 지급");
        }

        if (GUILayout.Button("아이템 비우기"))
        {
            DevModeCommands.ClearInventoryItems();
            SetStatus("인벤 아이템 Clear");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label("<b>기계</b>", RichLabel());
        GUILayout.BeginHorizontal();
        GUILayout.Label("count", GUILayout.Width(50));
        grantAllMachinesCountInput = GUILayout.TextField(grantAllMachinesCountInput, GUILayout.Width(60));
        if (GUILayout.Button("모든 기계 지급"))
        {
            if (int.TryParse(grantAllMachinesCountInput, out int machineCount) && machineCount > 0)
            {
                int granted = DevModeCommands.GrantAllMachines(machineCount);
                SetStatus($"모든 기계 지급 x{machineCount} → {granted}대");
            }
        }
        GUILayout.EndHorizontal();

        PlayerInventory inventory = PlayerInventory.GetOrFind();
        if (inventory == null)
        {
            GUILayout.Label("PlayerInventory 없음");
            return;
        }

        GUILayout.Space(4);
        GUILayout.Label("<b>보유</b>", RichLabel());
        foreach (ItemEntry entry in inventory.GetOwnedItemEntries())
        {
            if (entry?.item == null)
            {
                continue;
            }

            GUILayout.Label(
                $"{entry.item.Id} lv{entry.item.ResolvedLevel} ×{entry.count}"
                + (entry.item.Enchantments.Count > 0
                    ? $" enc={entry.item.Enchantments.Count}"
                    : string.Empty));
        }
    }

    private void DrawStoryTab()
    {
        GUILayout.Label("<b>Story</b>", RichLabel());

        GUILayout.BeginHorizontal();
        storyIdInput = GUILayout.TextField(storyIdInput);
        if (GUILayout.Button("Raise", GUILayout.Width(70)))
        {
            DevModeCommands.RaiseStory(storyIdInput);
            SetStatus($"Raise {storyIdInput}");
        }

        if (GUILayout.Button("Reset fired", GUILayout.Width(90)))
        {
            DevModeCommands.ResetStoryFired();
            SetStatus("firedStoryIds reset");
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("알려진 eventId", RichLabel());
        foreach (string eventId in DevModeCommands.KnownStoryEventIds)
        {
            if (GUILayout.Button(eventId))
            {
                storyIdInput = eventId;
                DevModeCommands.RaiseStory(eventId);
                SetStatus($"Raise {eventId}");
            }
        }
    }

    private void DrawSmokeTab()
    {
        GUILayout.Label("<b>Smoke</b>", RichLabel());
        GUILayout.Label(
            "NewGame → Q001 오퍼·수락 → 요구 지급 → 납품 → 보상/진행도 확인 + 메인 점프");

        if (GUILayout.Button("Run Q001 Smoke", GUILayout.Height(36)))
        {
            DevModeCommands.SmokeResult result = DevModeCommands.RunQ001Smoke(
                questManager,
                questPool,
                economy,
                questDatabase);
            smokeSummary = result.summary;
            SetStatus(result.passed ? "SMOKE PASSED" : "SMOKE FAILED");
            RefreshQuestLists();
            SyncEconomyFields();
        }

        if (!string.IsNullOrEmpty(smokeSummary))
        {
            GUILayout.Space(6);
            GUILayout.Label(smokeSummary);
        }
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

    private void RefreshCatalog()
    {
        ResolveReferences();
        questDatabase ??= DevModeCommands.LoadQuestDatabase();
        catalogQuests = DevModeCommands.ListQuests(questDatabase);
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
        unlockManager ??= FindAnyObjectByType<QuestUnlockManager>();
        shopUI ??= FindAnyObjectByType<ShopUI>();
        questDatabase ??= DevModeCommands.LoadQuestDatabase();
    }

    private void SyncEconomyFields()
    {
        GameSessionState session = GameSessionState.Instance;
        int day = session != null ? session.day : 1;
        dayInput = day.ToString();
        goldInput = (economy?.Gold ?? session?.gold ?? 0).ToString();
        repInput = (economy?.Reputation ?? session?.reputation ?? 0).ToString();
    }

    private void SetStatus(string message)
    {
        lastStatus = message;
        Debug.Log("[DevMode] " + message);
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
        return new GUIStyle(GUI.skin.label) { richText = true };
    }
#else
    private void Update()
    {
        // 릴리스 플레이어 빌드에서는 Dev Mode를 끈다.
    }
#endif
}
