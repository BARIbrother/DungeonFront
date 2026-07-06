using System.Collections.Generic;
using UnityEngine;

// id로 ItemDefinition을 조회하는 중앙 레지스트리. 씬의 매니저 오브젝트에 붙인다.
//
// itemJsonFiles JSON 양식:
// {
//   "items": [
//     {
//       "id": "iron_ore",
//       "displayName": "철광석",
//       "iconPath": "Icons/iron_ore",
//       "category": 0
//     }
//   ]
// }
// - id: 필수. 인벤토리·조회 키
// - displayName: 필수
// - iconPath: 선택. Resources 폴더 기준 경로
// - category: 선택. 0=Material, 1=Currency (생략 시 0)
public class ItemManager : MonoBehaviour
{
    // 에디터에서 미리 만든 아이템 SO 목록
    [SerializeField] private ItemDefinition[] editorItems;

    // 런타임에 등록할 JSON 아이템 정의 파일
    [SerializeField] private TextAsset[] itemJsonFiles;

    private readonly Dictionary<string, ItemDefinition> lookup = new();

    private void Awake()
    {
        Initialize();
        LoadJsonItems();
    }

    // 에디터 등록 아이템으로 lookup을 초기화한 뒤 JSON 아이템을 덮어쓴다.
    private void Initialize()
    {
        lookup.Clear();

        if (editorItems == null)
        {
            return;
        }

        foreach (ItemDefinition item in editorItems)
        {
            Register(item);
        }
    }

    // 아이템을 id 키로 등록한다. 같은 id가 있으면 덮어쓴다.
    public void Register(ItemDefinition item)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
        {
            return;
        }

        lookup[item.id] = item;
    }

    public ItemDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        lookup.TryGetValue(id, out ItemDefinition item);
        return item;
    }

    private void LoadJsonItems()
    {
        if (itemJsonFiles == null)
        {
            return;
        }

        foreach (TextAsset jsonFile in itemJsonFiles)
        {
            if (jsonFile == null)
            {
                continue;
            }

            LoadFromJson(jsonFile.text);
        }
    }

    private void LoadFromJson(string json)
    {
        ItemJsonFile data = JsonUtility.FromJson<ItemJsonFile>(json);
        if (data?.items == null)
        {
            return;
        }

        foreach (ItemJsonRecord record in data.items)
        {
            if (record == null || string.IsNullOrEmpty(record.id))
            {
                continue;
            }

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = record.id;
            item.displayName = record.displayName;
            item.category = record.category;

            if (!string.IsNullOrEmpty(record.iconPath))
            {
                item.icon = Resources.Load<Sprite>(record.iconPath);
            }

            Register(item);
        }
    }
}

[System.Serializable]
public class ItemJsonRecord
{
    public string id;
    public string displayName;
    public string iconPath;
    public ItemCategory category;
}

[System.Serializable]
public class ItemJsonFile
{
    public ItemJsonRecord[] items;
}
