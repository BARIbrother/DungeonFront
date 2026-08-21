#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Dev Mode 치트·스모크 로직. 패널과 공유한다. 기존 세션/퀘스트/인벤 API만 호출한다.
public static class DevModeCommands
{
    public static readonly string[] KnownStoryEventIds =
    {
        "001E00001",
        "001E00002",
        "001E00004",
        "001E00005",
        "001E00006",
        "001E99999",
    };

    // 메인 라인 (문서 README 순서). 선행 해금 점프용.
    public static readonly string[] MainQuestIds =
    {
        "00100001", // Q001
        "00100002", // Q002
        "00100003", // Q003
        "00100005", // Q005
        "00100010", // Q010
        "00100006", // Q006
        "00100007", // Q007
        "00100011", // Q011
        "00100008", // Q008
        "00100009", // Q009
    };

    public struct SmokeResult
    {
        public bool passed;
        public string summary;
    }

    public static QuestDatabase LoadQuestDatabase()
    {
        QuestDatabase database = Resources.Load<QuestDatabase>("Data/QuestDatabase");
        if (database != null)
        {
            return database;
        }

#if UNITY_EDITOR
        database = AssetDatabase.LoadAssetAtPath<QuestDatabase>(
            "Assets/Resources/Data/QuestDatabase.asset");
#endif
        return database;
    }

    public static Quest[] ListQuests(QuestDatabase database)
    {
        if (database != null)
        {
            Quest[] fromDb = database.GetAll();
            if (fromDb != null && fromDb.Length > 0)
            {
                return fromDb;
            }
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Quest", new[] { "Assets/Quest" });
        var list = new List<Quest>(guids.Length);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Quest quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
            if (quest != null)
            {
                list.Add(quest);
            }
        }

        list.Sort((left, right) =>
            string.Compare(ResolveId(left), ResolveId(right), StringComparison.Ordinal));
        return list.ToArray();
#else
        return Array.Empty<Quest>();
#endif
    }

    public static string ResolveId(Quest quest)
    {
        if (quest == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(quest.id))
        {
            return quest.id;
        }

        return QuestRuntimeRegistry.GetStableId(quest) ?? quest.name ?? string.Empty;
    }

    public static string FormatRewardPreview(Quest quest)
    {
        if (quest?.rewards?.entries == null || quest.rewards.entries.Length == 0)
        {
            return "(보상 없음)";
        }

        var builder = new StringBuilder();
        foreach (ItemEntry entry in quest.rewards.entries)
        {
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(entry.item.DisplayName ?? entry.item.Id);
            builder.Append('×');
            builder.Append(entry.count);
        }

        return builder.Length > 0 ? builder.ToString() : "(보상 없음)";
    }

    public static void NewGame()
    {
        GameSessionState.Instance?.NewGame();
        QuestManager.Instance?.ClearAllQuestState();
    }

    public static void SetDay(int day)
    {
        GameSessionState.Instance?.SetDay(day);
    }

    public static void ForcePhase(GamePhase phase)
    {
        GameSessionState session = GameSessionState.Instance;
        if (session == null)
        {
            return;
        }

        switch (phase)
        {
            case GamePhase.Prepare:
                session.ForcePhase(GamePhase.Prepare);
                break;
            case GamePhase.Production:
                if (session.Phase == GamePhase.Prepare)
                {
                    session.StartProduction();
                }
                else
                {
                    session.ForcePhase(GamePhase.Production);
                }

                break;
            case GamePhase.Settlement:
                if (session.Phase == GamePhase.Production)
                {
                    session.ForceEndProduction();
                }
                else
                {
                    session.ForcePhase(GamePhase.Settlement);
                }

                break;
        }
    }

    public static void SetEconomy(Week3EconomyService economy, int gold, int reputation)
    {
        if (economy != null)
        {
            economy.Restore(gold, reputation);
            return;
        }

        GameSessionState session = GameSessionState.Instance;
        if (session == null)
        {
            return;
        }

        session.AddGold(gold - session.gold);
        session.AddReputation(reputation - session.reputation);
    }

    public static void GrantRequirements(Quest quest, int multiplier)
    {
        PlayerInventory inventory = PlayerInventory.GetOrFind();
        if (inventory == null || quest == null)
        {
            Debug.LogWarning("[DevMode] PlayerInventory 또는 Quest가 없어 지급할 수 없습니다.");
            return;
        }

        foreach (ItemEntry entry in quest.requiredItems?.entries ?? Array.Empty<ItemEntry>())
        {
            if (entry?.item == null || entry.count <= 0)
            {
                continue;
            }

            inventory.Add(new ItemEntry
            {
                item = entry.item.Clone(),
                count = entry.count * Mathf.Max(1, multiplier)
            });
        }
    }

    public static void GrantItem(string itemId, int count, int level)
    {
        PlayerInventory inventory = PlayerInventory.GetOrFind();
        if (inventory == null || string.IsNullOrWhiteSpace(itemId) || count <= 0)
        {
            return;
        }

        ItemDefinition definition = ResolveItemDefinition(itemId);
        if (definition == null)
        {
            Debug.LogWarning($"[DevMode] ItemDefinition을 찾지 못함: {itemId}");
            return;
        }

        inventory.Add(new ItemEntry
        {
            item = Item.FromDefinition(definition, level > 0 ? level : 1),
            count = count
        });
    }

    // MachineDatabase의 prefab 있는 기계를 종류마다 count대씩 인벤에 넣는다.
    public static int GrantAllMachines(int countPerMachine)
    {
        if (countPerMachine <= 0)
        {
            return 0;
        }

        PlayerInventory inventory = PlayerInventory.GetOrFind();
        MachineDatabase database = ResolveMachineDatabase();
        if (inventory == null || database == null)
        {
            Debug.LogWarning("[DevMode] PlayerInventory 또는 MachineDatabase가 없어 기계를 지급할 수 없습니다.");
            return 0;
        }

        database.RebuildLookup();
        IReadOnlyList<ItemDef_Machine> machines = database.All;
        if (machines == null)
        {
            return 0;
        }

        int granted = 0;
        for (int i = 0; i < machines.Count; i++)
        {
            ItemDef_Machine definition = machines[i];
            if (definition == null || definition.machinePrefab == null)
            {
                continue;
            }

            for (int n = 0; n < countPerMachine; n++)
            {
                inventory.AddMachine(definition);
                granted++;
            }
        }

        return granted;
    }

    public static void ClearInventoryItems()
    {
        PlayerInventory.GetOrFind()?.ClearItems();
    }

    public static void GrantActiveQuestRequirements(QuestManager questManager)
    {
        if (questManager == null)
        {
            return;
        }

        foreach (Quest quest in questManager.currentQuests)
        {
            GrantRequirements(quest, 1);
        }
    }

    public static bool OfferQuest(
        QuestManager questManager,
        Quest source,
        out Quest offered,
        out string error)
    {
        offered = null;
        error = null;
        if (questManager == null || source == null)
        {
            error = "QuestManager 또는 Quest SO가 없습니다.";
            return false;
        }

        if (questManager.currentQuests.Count >= QuestManager.MaxActiveQuestCount)
        {
            error = "활성 의뢰가 가득 찼습니다.";
            return false;
        }

        string questId = ResolveId(source);
        if (questManager.currentQuests.Exists(quest => ResolveId(quest) == questId)
            || questManager.availableQuestsToday.Exists(quest => ResolveId(quest) == questId))
        {
            error = $"이미 목록에 있습니다: {questId}";
            return false;
        }

        offered = CloneQuestForOffer(source);
        questManager.availableQuestsToday.Add(offered);
        questManager.NotifyQuestsChanged();
        return true;
    }

    public static bool OfferAndAccept(
        QuestManager questManager,
        Quest source,
        out string error)
    {
        if (!OfferQuest(questManager, source, out Quest offered, out error))
        {
            return false;
        }

        if (!questManager.acceptQuest(offered))
        {
            error = "수락에 실패했습니다.";
            questManager.availableQuestsToday.Remove(offered);
            QuestRuntimeRegistry.Forget(offered);
            UnityEngine.Object.Destroy(offered);
            return false;
        }

        return true;
    }

    public static bool GrantAndDeliver(QuestManager questManager, Quest quest, out string error)
    {
        error = null;
        if (questManager == null || quest == null)
        {
            error = "QuestManager 또는 Quest가 없습니다.";
            return false;
        }

        GrantRequirements(quest, 1);
        if (!questManager.progressQuest(quest))
        {
            error = "납품 실패 (요구 미충족 또는 목록에 없음).";
            return false;
        }

        return true;
    }

    public static void RestoreProgressionThroughMain(string mainQuestIdInclusive)
    {
        QuestProgressionService progression = QuestProgressionService.Instance
            ?? UnityEngine.Object.FindAnyObjectByType<QuestProgressionService>();
        if (progression == null)
        {
            Debug.LogWarning("[DevMode] QuestProgressionService가 없습니다.");
            return;
        }

        var completed = new List<string>();
        foreach (string mainId in MainQuestIds)
        {
            completed.Add(mainId);
            if (string.Equals(mainId, mainQuestIdInclusive, StringComparison.Ordinal))
            {
                break;
            }
        }

        progression.Restore(completed);
    }

    public static void ResetProgression()
    {
        QuestProgressionService progression = QuestProgressionService.Instance
            ?? UnityEngine.Object.FindAnyObjectByType<QuestProgressionService>();
        progression?.ResetProgression();
    }

    public static void RaiseStory(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        StoryEventBus.Raise(eventId.Trim());
        Debug.Log($"[DevMode] Story Raise {eventId}");
    }

    public static void ResetStoryFired()
    {
        FactoryStoryHooks hooks = UnityEngine.Object.FindAnyObjectByType<FactoryStoryHooks>();
        hooks?.ResetFiredStoryIds();
    }

    public static SmokeResult RunQ001Smoke(
        QuestManager questManager,
        QuestPool questPool,
        Week3EconomyService economy,
        QuestDatabase database)
    {
        var log = new StringBuilder();
        bool passed = true;

        void Fail(string message)
        {
            passed = false;
            log.AppendLine("FAIL: " + message);
            Debug.LogError("[DevMode Smoke] " + message);
        }

        void Ok(string message)
        {
            log.AppendLine("OK: " + message);
            Debug.Log("[DevMode Smoke] " + message);
        }

        if (questManager == null)
        {
            Fail("QuestManager 없음");
            return new SmokeResult { passed = false, summary = log.ToString() };
        }

        NewGame();
        economy ??= UnityEngine.Object.FindAnyObjectByType<Week3EconomyService>();
        int goldBefore = economy != null ? economy.Gold : GameSessionState.Instance?.gold ?? 0;
        int repBefore = economy != null
            ? economy.Reputation
            : GameSessionState.Instance?.reputation ?? 0;

        QuestDatabase db = database ?? LoadQuestDatabase();
        Quest q001 = db != null ? db.Get("00100001") : null;
        if (q001 == null)
        {
            foreach (Quest candidate in ListQuests(db))
            {
                if (ResolveId(candidate) == "00100001"
                    || string.Equals(candidate.name, "Q001", StringComparison.Ordinal))
                {
                    q001 = candidate;
                    break;
                }
            }
        }

        if (q001 == null)
        {
            Fail("Q001 SO를 찾지 못함 (00100001)");
            return new SmokeResult { passed = false, summary = log.ToString() };
        }

        Ok($"Q001 로드: {q001.title}");

        if (questPool != null)
        {
            int reputation = economy != null ? economy.Reputation : 0;
            questPool.MakeAvailableQuestsToday(reputation);
        }

        if (!OfferAndAccept(questManager, q001, out string offerError))
        {
            Fail(offerError ?? "Q001 수락 실패");
            return new SmokeResult { passed = false, summary = log.ToString() };
        }

        Ok("Q001 수락");

        Quest active = questManager.currentQuests.Find(quest =>
            ResolveId(quest) == "00100001"
            || string.Equals(quest.title, q001.title, StringComparison.Ordinal));
        if (active == null)
        {
            Fail("수락 후 활성 목록에 Q001 없음");
            return new SmokeResult { passed = false, summary = log.ToString() };
        }

        if (!GrantAndDeliver(questManager, active, out string deliverError))
        {
            Fail(deliverError ?? "Q001 납품 실패");
            return new SmokeResult { passed = false, summary = log.ToString() };
        }

        Ok("Q001 납품");

        int goldAfter = economy != null ? economy.Gold : GameSessionState.Instance?.gold ?? 0;
        int repAfter = economy != null
            ? economy.Reputation
            : GameSessionState.Instance?.reputation ?? 0;

        if (goldAfter <= goldBefore && repAfter <= repBefore)
        {
            Fail($"보상 미반영 Gold {goldBefore}->{goldAfter}, Rep {repBefore}->{repAfter}");
        }
        else
        {
            Ok($"보상 반영 Gold {goldBefore}->{goldAfter}, Rep {repBefore}->{repAfter}");
        }

        QuestProgressionService progression = QuestProgressionService.Instance
            ?? UnityEngine.Object.FindAnyObjectByType<QuestProgressionService>();
        if (progression != null && !progression.IsCompleted("00100001"))
        {
            Fail("진행도에 00100001 미기록");
        }
        else if (progression != null)
        {
            Ok("진행도 00100001 완료");
        }

        RestoreProgressionThroughMain("00100002");
        if (progression != null
            && progression.IsCompleted("00100001")
            && progression.IsCompleted("00100002"))
        {
            Ok("메인 선행 점프(Q001~Q002) 확인");
        }
        else if (progression != null)
        {
            Fail("메인 선행 Restore 실패");
        }

        log.AppendLine(passed ? "SMOKE PASSED" : "SMOKE FAILED");
        return new SmokeResult { passed = passed, summary = log.ToString().TrimEnd() };
    }

    private static Quest CloneQuestForOffer(Quest source)
    {
        Quest clone = ScriptableObject.CreateInstance<Quest>();
        clone.id = source.id;
        clone.name = source.name;
        clone.title = source.title;
        clone.clientName = source.clientName;
        clone.content = source.content;
        clone.deadlineDays = source.deadlineDays;
        clone.currentleftDeadlineDays = source.deadlineDays;
        clone.requiredItems = CloneItemEntryList(source.requiredItems);
        clone.rewards = CloneItemEntryList(source.rewards);

        string questId = ResolveId(source);
        QuestRuntimeInfo sourceInfo = QuestRuntimeRegistry.Get(source);
        QuestRuntimeRegistry.Register(clone, new QuestRuntimeInfo
        {
            questId = questId,
            sourceQuestId = questId,
            rewardReputation = sourceInfo?.rewardReputation ?? 0,
            minReputation = sourceInfo?.minReputation ?? 0,
            questKind = sourceInfo?.questKind ?? QuestKind.Standard,
            unlockAfterQuestId = sourceInfo?.unlockAfterQuestId,
            isMandatory = sourceInfo?.isMandatory ?? false,
            isMainStoryQuest = sourceInfo?.isMainStoryQuest ?? false,
            triggersBackCaveEnding = sourceInfo?.triggersBackCaveEnding ?? false
        });
        return clone;
    }

    private static ItemEntryList CloneItemEntryList(ItemEntryList source)
    {
        if (source?.entries == null)
        {
            return new ItemEntryList { length = 0, entries = Array.Empty<ItemEntry>() };
        }

        var entries = new ItemEntry[source.entries.Length];
        for (int i = 0; i < source.entries.Length; i++)
        {
            ItemEntry entry = source.entries[i];
            if (entry?.item == null)
            {
                continue;
            }

            entries[i] = new ItemEntry
            {
                item = entry.item.Clone(),
                count = entry.count
            };
        }

        return new ItemEntryList
        {
            length = entries.Length,
            entries = entries
        };
    }

    private static MachineDatabase ResolveMachineDatabase()
    {
        PlayerMovement movement = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (movement != null && movement.MachineDatabase != null)
        {
            return movement.MachineDatabase;
        }

        MachineDatabase[] loaded = Resources.FindObjectsOfTypeAll<MachineDatabase>();
        if (loaded != null && loaded.Length > 0)
        {
            return loaded[0];
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<MachineDatabase>(
            "Assets/ItemDefinition/MachineDef/MachineDatabase.asset");
#else
        return null;
#endif
    }

    private static ItemDefinition ResolveItemDefinition(string itemId)
    {
        ItemManager manager = UnityEngine.Object.FindAnyObjectByType<ItemManager>();
        ItemDefinition definition = manager != null ? manager.Get(itemId) : null;
        if (definition != null)
        {
            return definition;
        }

        PlayerInventory inventory = PlayerInventory.GetOrFind();
        definition = inventory != null ? inventory.GetDefinition(itemId) : null;
        if (definition != null)
        {
            return definition;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets($"{itemId} t:ItemDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition candidate = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (candidate != null
                && string.Equals(candidate.id, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }
#endif
        return null;
    }
}
#endif
