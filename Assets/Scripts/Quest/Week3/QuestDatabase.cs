using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "DungeonFront/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [SerializeField] private Quest[] quests = Array.Empty<Quest>();

    private Dictionary<string, Quest> byId;

    public Quest Get(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        EnsureIndex();
        byId.TryGetValue(questId, out Quest quest);
        return quest;
    }

    private void OnEnable()
    {
        byId = null;
    }

    private void EnsureIndex()
    {
        if (byId != null)
        {
            return;
        }

        byId = new Dictionary<string, Quest>(StringComparer.Ordinal);
        AddToIndex(quests);
        AddToIndex(Resources.LoadAll<Quest>("Data/Quests"));
    }

    private void AddToIndex(IEnumerable<Quest> source)
    {
        if (source == null)
        {
            return;
        }

        foreach (Quest quest in source)
        {
            string questId = QuestRuntimeRegistry.GetStableId(quest);
            if (quest == null || string.IsNullOrWhiteSpace(questId))
            {
                continue;
            }

            if (!byId.TryAdd(questId, quest))
            {
                Debug.LogWarning($"Duplicate quest id ignored: {questId}", quest);
            }
        }
    }
}
