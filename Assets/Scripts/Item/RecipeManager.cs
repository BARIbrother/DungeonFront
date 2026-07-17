using System.Collections.Generic;
using UnityEngine;

// id로 Recipe를 조회하는 중앙 레지스트리. 씬의 매니저 오브젝트에 붙인다.
//
// recipeJsonFiles JSON 양식:
// {
//   "recipes": [
//     {
//       "id": "iron_plate_smelt",
//       "recipeTime": 10,
//       "inputs": [
//         { "itemId": "iron_ore", "count": 2 }
//       ],
//       "outputs": [
//         { "itemId": "iron_plate", "count": 1 }
//       ]
//     }
//   ]
// }
// - id: 필수. 조회 키
// - recipeTime: 필수. 완성 진행도 목표 (기계 workSpeed 누적)
// - inputs / outputs: 필수. itemId는 ItemManager에 등록된 id
public class RecipeManager : MonoBehaviour
{
    [SerializeField] private ItemManager itemManager;

    // 에디터에서 미리 만든 레시피 SO 목록
    [SerializeField] private Recipe[] editorRecipes;

    // 런타임에 등록할 JSON 레시피 정의 파일
    [SerializeField] private TextAsset[] recipeJsonFiles;

    private readonly Dictionary<string, Recipe> lookup = new();

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        LoadJsonRecipes();
    }

    // 에디터 등록 레시피로 lookup을 초기화한다.
    private void Initialize()
    {
        lookup.Clear();

        if (editorRecipes == null)
        {
            return;
        }

        foreach (Recipe recipe in editorRecipes)
        {
            Register(recipe);
        }
    }

    // 레시피를 id 키로 등록한다. 같은 id가 있으면 덮어쓴다.
    public void Register(Recipe recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.id))
        {
            return;
        }

        lookup[recipe.id] = recipe;
    }

    public Recipe Get(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        lookup.TryGetValue(id, out Recipe recipe);
        return recipe;
    }

    private void LoadJsonRecipes()
    {
        if (recipeJsonFiles == null)
        {
            return;
        }

        foreach (TextAsset jsonFile in recipeJsonFiles)
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
        RecipeJsonFile data = JsonUtility.FromJson<RecipeJsonFile>(json);
        if (data?.recipes == null)
        {
            return;
        }

        ItemManager items = GetItemManager();
        if (items == null)
        {
            return;
        }

        foreach (RecipeJsonRecord record in data.recipes)
        {
            if (record == null || string.IsNullOrEmpty(record.id))
            {
                continue;
            }

            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.id = record.id;
            recipe.recipeTime = record.recipeTime > 0 ? record.recipeTime : record.durationByTick;
            recipe.inputEntryList = BuildItemEntryList(items, record.inputs);
            recipe.outputEntryList = BuildItemEntryList(items, record.outputs);
            Register(recipe);
        }
    }

    private static ItemEntryList BuildItemEntryList(ItemManager itemManager, RecipeItemJsonRecord[] records)
    {
        if (records == null || records.Length == 0)
        {
            return new ItemEntryList
            {
                length = 0,
                entries = System.Array.Empty<ItemEntry>()
            };
        }

        var list = new ItemEntryList
        {
            length = records.Length,
            entries = new ItemEntry[records.Length]
        };

        for (int i = 0; i < records.Length; i++)
        {
            RecipeItemJsonRecord record = records[i];
            if (record == null)
            {
                continue;
            }

            list.entries[i] = new ItemEntry
            {
                item = itemManager.Get(record.itemId),
                count = record.count
            };
        }

        return list;
    }

    private ItemManager GetItemManager()
    {
        return itemManager != null
            ? itemManager
            : FindAnyObjectByType<ItemManager>();
    }
}

[System.Serializable]
public class RecipeJsonRecord
{
    public string id;
    public int recipeTime;
    // 구 JSON 호환. recipeTime이 비어 있을 때만 사용한다.
    public int durationByTick;
    public RecipeItemJsonRecord[] inputs;
    public RecipeItemJsonRecord[] outputs;
}

[System.Serializable]
public class RecipeItemJsonRecord
{
    public string itemId;
    public int count;
}

[System.Serializable]
public class RecipeJsonFile
{
    public RecipeJsonRecord[] recipes;
}
