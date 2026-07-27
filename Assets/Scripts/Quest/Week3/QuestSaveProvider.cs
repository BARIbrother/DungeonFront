using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AcceptedQuestSave
{
    public string questId;
    public int daysRemaining;
    public int acceptedDay;
}

public interface IQuestSaveProvider
{
    AcceptedQuestSave[] Export();
    void Import(AcceptedQuestSave[] data);
}

public class QuestSaveProvider : MonoBehaviour, IQuestSaveProvider
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestDatabase questDatabase;
    [SerializeField] private QuestPool questPool;

    public AcceptedQuestSave[] Export()
    {
        QuestManager manager = GetManager();
        if (manager == null)
        {
            return Array.Empty<AcceptedQuestSave>();
        }

        var result = new List<AcceptedQuestSave>(manager.currentQuests.Count);
        foreach (Quest quest in manager.currentQuests)
        {
            if (quest == null)
            {
                continue;
            }

            QuestRuntimeInfo info = QuestRuntimeRegistry.GetOrCreate(quest);
            string id = QuestRuntimeRegistry.GetStableId(quest);
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("Quest without a stable id was skipped while saving.", quest);
                continue;
            }

            result.Add(new AcceptedQuestSave
            {
                questId = id,
                daysRemaining = quest.currentleftDeadlineDays,
                acceptedDay = info.acceptedDay
            });
        }

        return result.ToArray();
    }

    public void Import(AcceptedQuestSave[] data)
    {
        QuestManager manager = GetManager();
        if (manager == null)
        {
            Debug.LogWarning("QuestSaveProvider could not find QuestManager.", this);
            return;
        }

        manager.ClearActive();
        if (data == null || data.Length == 0)
        {
            return;
        }

        QuestDatabase database = GetDatabase();
        QuestPool pool = GetPool();
        if (database == null && pool == null)
        {
            Debug.LogWarning("QuestSaveProvider has no QuestDatabase or QuestPool.", this);
            return;
        }

        foreach (AcceptedQuestSave saved in data)
        {
            if (saved == null || string.IsNullOrWhiteSpace(saved.questId))
            {
                continue;
            }

            Quest template = database != null
                ? database.Get(saved.questId)
                : null;
            bool isTemporaryTemplate = false;
            if (template == null && pool != null)
            {
                template = pool.CreateQuestById(saved.questId);
                isTemporaryTemplate = template != null;
            }

            if (template == null)
            {
                Debug.LogWarning($"Saved quest id was not found and will be skipped: {saved.questId}");
                continue;
            }

            Quest instance = manager.CreateQuestInstance(template, saved.acceptedDay);
            instance.currentleftDeadlineDays = Mathf.Max(0, saved.daysRemaining);
            manager.RestoreQuest(instance);

            if (isTemporaryTemplate)
            {
                QuestRuntimeRegistry.Forget(template);
                Destroy(template);
            }
        }
    }

    private QuestManager GetManager()
    {
        if (questManager == null)
        {
            questManager = FindAnyObjectByType<QuestManager>();
        }

        return questManager;
    }

    private QuestDatabase GetDatabase()
    {
        if (questDatabase == null)
        {
            questDatabase = Resources.Load<QuestDatabase>("Data/QuestDatabase");
        }

        return questDatabase;
    }

    private QuestPool GetPool()
    {
        if (questPool == null)
        {
            questPool = FindAnyObjectByType<QuestPool>();
        }

        return questPool;
    }
}
