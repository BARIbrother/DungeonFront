using System.Collections.Generic;

// 협업 계약인 Quest.cs를 확장하지 않고 Week3·4 전용 상태를 보관한다.
public enum QuestKind
{
    Standard,
    Story,
    Perpetual
}

public sealed class QuestRuntimeInfo
{
    public string questId;
    public string sourceQuestId;
    public int acceptedDay;
    public int rewardReputation;
    public int minReputation;
    public QuestKind questKind;
    public string unlockAfterQuestId;
    public bool isMandatory;
    public bool isMainStoryQuest;
    public bool triggersBackCaveEnding;

    public bool IsPerpetual => questKind == QuestKind.Perpetual;

    public QuestRuntimeInfo CloneForAcceptedDay(int day)
    {
        return new QuestRuntimeInfo
        {
            questId = questId,
            sourceQuestId = string.IsNullOrWhiteSpace(sourceQuestId)
                ? questId
                : sourceQuestId,
            acceptedDay = day,
            rewardReputation = rewardReputation,
            minReputation = minReputation,
            questKind = questKind,
            unlockAfterQuestId = unlockAfterQuestId,
            isMandatory = isMandatory,
            isMainStoryQuest = isMainStoryQuest,
            triggersBackCaveEnding = triggersBackCaveEnding
        };
    }
}

public static class QuestRuntimeRegistry
{
    // Quest 객체 자체를 키로 사용해 Unity 6.5에서 폐기된 GetInstanceID 호출을 피한다.
    private static readonly Dictionary<Quest, QuestRuntimeInfo> byInstance = new();

    public static void Register(Quest quest, QuestRuntimeInfo info)
    {
        if (quest != null && info != null)
        {
            byInstance[quest] = info;
        }
    }

    public static QuestRuntimeInfo Get(Quest quest)
    {
        if (quest == null)
        {
            return null;
        }

        byInstance.TryGetValue(quest, out QuestRuntimeInfo info);
        return info;
    }

    public static QuestRuntimeInfo GetOrCreate(Quest quest)
    {
        QuestRuntimeInfo info = Get(quest);
        if (info != null || quest == null)
        {
            return info;
        }

        string fallbackId = !string.IsNullOrWhiteSpace(quest.id)
            ? quest.id
            : quest.name;
        info = new QuestRuntimeInfo
        {
            questId = fallbackId,
            sourceQuestId = fallbackId,
            questKind = QuestKind.Standard
        };
        Register(quest, info);
        return info;
    }

    public static string GetStableId(Quest quest)
    {
        QuestRuntimeInfo info = GetOrCreate(quest);
        return info == null
            ? null
            : string.IsNullOrWhiteSpace(info.sourceQuestId)
                ? info.questId
                : info.sourceQuestId;
    }

    public static void Forget(Quest quest)
    {
        if (quest != null)
        {
            byInstance.Remove(quest);
        }
    }
}
