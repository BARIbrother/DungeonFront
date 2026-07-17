using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Recipe", menuName = "DungeonFront/Recipe")]
public class Recipe : ScriptableObject
{
    // JSON·조회에 쓰는 고정 키
    public string id;

    public ItemEntryList inputEntryList;
    public ItemEntryList outputEntryList;

    // 완성까지 필요한 진행도. 기계 workSpeed 누적이 이 값 이상이면 산출한다.
    [FormerlySerializedAs("durationByTick")]
    public int recipeTime;
}
