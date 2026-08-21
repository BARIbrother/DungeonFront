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

    // 마나 제작기·마법 부여대가 소모하는 마나량. 0이면 입력 정수의 함량 합을 쓴다.
    public int manaCost;

    public int GetManaCost()
    {
        if (manaCost > 0)
        {
            return manaCost;
        }

        return ManaEssence.SumFromInputs(inputEntryList);
    }
}
