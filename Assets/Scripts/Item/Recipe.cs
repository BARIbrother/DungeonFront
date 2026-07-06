using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "DungeonFront/Recipe")]
public class Recipe : ScriptableObject
{
    // JSON·조회에 쓰는 고정 키
    public string id;

    public ItemEntryList inputEntryList;
    public ItemEntryList outputEntryList;
    public int durationByTick;
}
