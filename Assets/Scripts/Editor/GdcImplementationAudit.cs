using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GDC Week 1~5 요구사항을 파일/에셋/씬 연결 기준으로 반복 검사한다.
/// 실행: Unity 메뉴 DungeonFront > Audit > Generate GDC Static Audit
/// 이 도구는 실행 중 조작 결과까지 보장하지 않는다. 결과 문서의 수동 PlayMode 체크와 함께 사용한다.
/// </summary>
public static class GdcImplementationAudit
{
    private const string QuestLinePath = "Assets/Data/Quest/questline.json";
    private const string QuestSystemPrefabPath = "Assets/Prefabs/Quest/QuestSystemRoot.prefab";

    [MenuItem("DungeonFront/Audit/Generate GDC Static Audit")]
    public static void Generate()
    {
        var report = new StringBuilder();
        report.AppendLine("# GDC 정적 구현 검사 결과");
        report.AppendLine();
        report.AppendLine($"> 생성 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine("> 범위: 소스 코드·에셋·프리팹·Build Settings. 실제 플레이 입력/화면 동작은 수동 PlayMode 검사 필요.");
        report.AppendLine();

        AddBuildSettingsSection(report);
        AddNewGameSection(report);
        AddQuestDataSection(report);
        AddQuestUiSection(report);
        AddMissingFeatureSection(report);
        AddIconSection(report);

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "GDC-static-audit-report.md"));
        File.WriteAllText(fullPath, report.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Debug.Log($"[GDC Audit] 정적 검사 보고서를 생성했습니다: {fullPath}");
    }

    // CI/배치 모드에서도 같은 검사를 실행할 수 있는 진입점.
    public static void GenerateFromCommandLine()
    {
        Generate();
    }

    private static void AddBuildSettingsSection(StringBuilder report)
    {
        report.AppendLine("## Build 및 시작 흐름");
        var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        string firstScene = enabledScenes.Length > 0 ? enabledScenes[0].path : "(없음)";
        bool hasTitle = enabledScenes.Any(scene =>
            string.Equals(Path.GetFileNameWithoutExtension(scene.path), "Title", StringComparison.OrdinalIgnoreCase));

        Result(report, enabledScenes.Length > 0, "Build Settings에 활성 씬이 있다", firstScene);
        Result(report, hasTitle, "게임오버용 Title 씬이 Build Settings에 있다", hasTitle ? "발견" : "발견하지 못함");
        Result(report, string.Equals(firstScene, "Assets/Scenes/Factory.unity", StringComparison.Ordinal),
            "Build 첫 씬이 Factory 시작점이다", firstScene);
        report.AppendLine();
    }

    private static void AddNewGameSection(StringBuilder report)
    {
        report.AppendLine("## W1 NewGame·인벤토리 정적 검사");
        string sessionSource = ReadAssetText("Assets/Scripts/GameFlow/GameSessionState.cs");
        bool startsAtZero = sessionSource.Contains("gold = 0;") && sessionSource.Contains("reputation = 0;");
        bool startsAtHundredTen = sessionSource.Contains("gold = 100;") && sessionSource.Contains("reputation = 10;");
        bool grantsWarehouse = sessionSource.Contains("Warehouse_1");
        bool writesToPlayerInventory = sessionSource.Contains("PlayerInventory") && sessionSource.Contains("AddMachine(");

        Result(report, startsAtZero && !startsAtHundredTen,
            "NewGame이 골드 0·명성 0으로 시작한다",
            startsAtHundredTen ? "현재 코드에 gold = 100; reputation = 10;이 있음" : "소스 확인 필요");
        Result(report, grantsWarehouse,
            "NewGame이 최신 정본의 시작 창고를 지급한다", grantsWarehouse ? "Warehouse_1 발견" : "Warehouse_1 없음");
        Result(report, writesToPlayerInventory,
            "NewGame이 실제 PlayerInventory를 초기화한다",
            writesToPlayerInventory ? "PlayerInventory.AddMachine 호출 발견" : "InventoryState만 채우는 구조");
        report.AppendLine();
    }

    private static void AddQuestDataSection(StringBuilder report)
    {
        report.AppendLine("## W2~W4 의뢰 데이터 정적 검사");
        QuestLineFile questLine = LoadQuestLine();
        QuestLineQuest[] quests = questLine?.quests ?? Array.Empty<QuestLineQuest>();
        int uniqueIdCount = quests.Where(quest => quest != null)
            .Select(quest => quest.id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .Count();
        int mainCount = quests.Count(quest => quest != null && quest.y < 320f);
        int perpetualCount = quests.Count(quest => quest != null
            && (quest.deadlineDays <= 0 || (quest.title ?? string.Empty).StartsWith("상시", StringComparison.Ordinal)));

        var questItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (QuestLineQuest quest in quests.Where(quest => quest != null))
        {
            AddItemIds(quest.requiredItems, questItemIds);
            AddItemIds(quest.reward, questItemIds);
        }

        HashSet<string> definedItemIds = GetDefinedItemIds();
        string[] missingItemIds = questItemIds.Where(id => !definedItemIds.Contains(id)).OrderBy(id => id).ToArray();

        Result(report, quests.Length == 47, "questline.json 의뢰가 47개다", $"현재 {quests.Length}개");
        Result(report, uniqueIdCount == quests.Length, "questline.json 의뢰 ID가 중복되지 않는다", $"고유 ID {uniqueIdCount}개");
        Result(report, mainCount == 10, "메인 진행선이 10개다", $"현재 {mainCount}개");
        Result(report, perpetualCount == 7, "상시 의뢰가 7개다", $"현재 {perpetualCount}개");
        Result(report, missingItemIds.Length == 0,
            "의뢰의 모든 itemId가 실제 ItemDefinition에 있다",
            missingItemIds.Length == 0
                ? $"{questItemIds.Count}종 모두 연결됨"
                : $"{questItemIds.Count}종 중 {missingItemIds.Length}종 누락. 예: {string.Join(", ", missingItemIds.Take(8))}");
        report.AppendLine();
    }

    private static void AddQuestUiSection(StringBuilder report)
    {
        report.AppendLine("## W3~W5 퀘스트 UI·프리팹 연결 검사");
        GameObject root = PrefabUtility.LoadPrefabContents(QuestSystemPrefabPath);
        try
        {
            var gameOver = root.GetComponentInChildren<GameOverController>(true);
            var shop = root.GetComponentInChildren<ShopUI>(true);
            var debugPanel = root.GetComponentInChildren<QuestSystemDebugPanel>(true);

            Result(report, gameOver != null, "QuestSystemRoot에 GameOverController가 있다", gameOver == null ? "컴포넌트 없음" : "발견");
            Result(report, GetObjectReference(gameOver, "gameOverPanel") != null,
                "GameOverController에 실제 게임오버 패널이 연결되어 있다", "gameOverPanel 확인");
            Result(report, GetObjectReference(shop, "listRoot") != null
                           && GetObjectReference(shop, "rowPrefab") != null,
                "ShopUI에 목록 루트와 행 프리팹이 연결되어 있다", "listRoot/rowPrefab 확인");
            Result(report, !GetBool(debugPanel, "enableDebugPanel"),
                "정식 프리팹에서 F8 QA 패널이 기본 비활성이다", "enableDebugPanel 확인");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Result(report, CountReferencesInScenes("Assets/Scripts/Quest/Week4/PerpetualQuestPanel.cs") > 0,
            "정식 씬에 PerpetualQuestPanel이 배치되어 있다",
            $"씬 참조 {CountReferencesInScenes("Assets/Scripts/Quest/Week4/PerpetualQuestPanel.cs")}개");
        Result(report, CountReferencesInScenes("Assets/Scripts/Quest/Week3/UnlockUI.cs") > 0,
            "정식 씬에 UnlockUI가 배치되어 있다",
            $"씬 참조 {CountReferencesInScenes("Assets/Scripts/Quest/Week3/UnlockUI.cs")}개");
        report.AppendLine();
    }

    private static void AddMissingFeatureSection(StringBuilder report)
    {
        report.AppendLine("## W3 Dev1·W5 기능 증거 검사");
        // 파일명은 바뀔 수 있으므로 클래스명 대신, 이 기능에 반드시 필요한 런타임 동작을 찾는다.
        // Editor 폴더와 이 감사기 자신은 제외해 검색어가 보고서 문구에만 있어 PASS 되는 일을 막는다.
        string runtimeSource = ReadRuntimeScriptsText();
        bool subscribesToStoryEvents = runtimeSource.Contains("OnStoryEvent +=", StringComparison.Ordinal);
        bool pausesGame = runtimeSource.Contains("Time.timeScale", StringComparison.Ordinal);
        bool usesPersistentSavePath = runtimeSource.Contains("Application.persistentDataPath", StringComparison.Ordinal);
        bool writesJson = runtimeSource.Contains("JsonUtility.ToJson", StringComparison.Ordinal)
            && runtimeSource.Contains("File.WriteAllText", StringComparison.Ordinal);

        Result(report, subscribesToStoryEvents && pausesGame,
            "이름과 무관하게 스토리 이벤트를 받아 게임을 멈추는 대화/튜토리얼 소비자가 있다",
            "OnStoryEvent 구독 + Time.timeScale 사용을 함께 검사");
        Result(report, subscribesToStoryEvents,
            "StoryEventBus를 실제 UI가 구독한다", "OnStoryEvent += 검색");
        Result(report, usesPersistentSavePath && writesJson,
            "슬롯 파일 세이브/로드의 최소 증거가 있다",
            "persistentDataPath + JsonUtility.ToJson + File.WriteAllText 검사");

        Result(report, runtimeSource.Contains("Screen.SetResolution", StringComparison.Ordinal),
            "해상도 설정 코드가 있다", "런타임 Scripts만 검사");
        Result(report, runtimeSource.Contains("FullScreenMode", StringComparison.Ordinal)
                           || runtimeSource.Contains("Screen.fullScreen", StringComparison.Ordinal),
            "창 모드 설정 코드가 있다", "런타임 Scripts만 검사");
        Result(report, runtimeSource.Contains("AudioMixer", StringComparison.Ordinal)
                       || runtimeSource.Contains("AudioListener.volume", StringComparison.Ordinal),
            "볼륨 설정 코드가 있다", "런타임 Scripts만 검사");
        Result(report, runtimeSource.Contains("escapeKey", StringComparison.Ordinal)
                           && pausesGame,
            "ESC 일시정지 코드가 있다", "escapeKey + Time.timeScale 검사");
        report.AppendLine();
    }

    private static void AddIconSection(StringBuilder report)
    {
        report.AppendLine("## W5 아이콘 정적 검사");
        string[] iconPaths = Directory.Exists("Assets/Art/Items")
            ? Directory.GetFiles("Assets/Art/Items", "*.png", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();
        int native16Count = 0;
        foreach (string absolutePath in iconPaths)
        {
            string assetPath = absolutePath.Replace('\\', '/');
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null && texture.width == 16 && texture.height == 16)
            {
                native16Count++;
            }
        }

        Result(report, iconPaths.Length >= 21, "Dev2 담당 아이콘 21개 이상이 프로젝트에 있다", $"현재 {iconPaths.Length}개");
        Result(report, native16Count >= 21, "16×16 아이콘 21개 이상이 있다", $"현재 {native16Count}개");
        report.AppendLine();
    }

    private static void Result(StringBuilder report, bool passed, string requirement, string detail)
    {
        report.AppendLine($"- [{(passed ? "PASS" : "FAIL")}] {requirement} — {detail}");
    }

    private static QuestLineFile LoadQuestLine()
    {
        if (!File.Exists(QuestLinePath))
        {
            return null;
        }

        return JsonUtility.FromJson<QuestLineFile>(File.ReadAllText(QuestLinePath));
    }

    private static void AddItemIds(IEnumerable<QuestLineItem> entries, ISet<string> itemIds)
    {
        foreach (QuestLineItem entry in entries ?? Array.Empty<QuestLineItem>())
        {
            if (!string.IsNullOrWhiteSpace(entry?.itemcode))
            {
                itemIds.Add(entry.itemcode);
            }
        }
    }

    private static HashSet<string> GetDefinedItemIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && !string.IsNullOrWhiteSpace(item.id))
            {
                result.Add(item.id);
            }
        }

        return result;
    }

    private static UnityEngine.Object GetObjectReference(UnityEngine.Object target, string propertyName)
    {
        if (target == null)
        {
            return null;
        }

        return new SerializedObject(target).FindProperty(propertyName)?.objectReferenceValue;
    }

    private static bool GetBool(UnityEngine.Object target, string propertyName)
    {
        return target != null && new SerializedObject(target).FindProperty(propertyName)?.boolValue == true;
    }

    private static int CountReferencesInScenes(string scriptAssetPath)
    {
        string guid = AssetDatabase.AssetPathToGUID(scriptAssetPath);
        if (string.IsNullOrWhiteSpace(guid))
        {
            return 0;
        }

        return Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories)
            .Count(scene => File.ReadAllText(scene).Contains(guid, StringComparison.Ordinal));
    }

    private static string ReadRuntimeScriptsText()
    {
        return string.Join("\n", AssetDatabase.FindAssets("t:MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/Scripts/", StringComparison.Ordinal)
                && !path.Contains("/Editor/", StringComparison.Ordinal))
            .Select(ReadAssetText));
    }

    private static string ReadAssetText(string assetPath)
    {
        return File.Exists(assetPath) ? File.ReadAllText(assetPath) : string.Empty;
    }

    [Serializable]
    private class QuestLineFile
    {
        public QuestLineQuest[] quests;
    }

    [Serializable]
    private class QuestLineQuest
    {
        public string id;
        public string title;
        public int deadlineDays;
        public QuestLineItem[] requiredItems;
        public QuestLineItem[] reward;
        public float x;
        public float y;
    }

    [Serializable]
    private class QuestLineItem
    {
        public string itemcode;
    }
}
