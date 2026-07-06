using UnityEngine;

// 아이템 한 종류의 정적 정의. 에디터 SO 또는 ItemManager가 런타임에 생성한다.
[CreateAssetMenu(fileName = "Item", menuName = "DungeonFront/Item")]
public class ItemDefinition : ScriptableObject
{
    // JSON·세이브·인벤토리 조회에 쓰는 고정 키
    public string id;

    // UI·기획용 표시 이름
    public string displayName;

    public Sprite icon;

    // UI·사용 규칙 구분용 분류 (Material: 가방 품목, Currency: 골드·명성 등)
    public ItemCategory category;
}
