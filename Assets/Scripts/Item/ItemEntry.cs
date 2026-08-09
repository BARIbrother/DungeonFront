using System;
using UnityEngine;
using UnityEngine.Serialization;

// 아이템 개체와 수량을 묶은 엔트리. 인벤·포트·레시피·퀘스트에서 공유한다.
[Serializable]
public class ItemEntry : ISerializationCallbackReceiver
{
    [SerializeField]
    private Item _item;

    public int count;

    // 예전 ItemEntry.item(ItemDefinition) SO 직렬화를 승격하기 위한 레거시 필드.
    // 필드 이름을 item으로 두면 신규 Item 필드와 키가 충돌하므로 FormerlySerializedAs만 사용한다.
    [FormerlySerializedAs("item")]
    [SerializeField]
    [HideInInspector]
    private ItemDefinition _legacyDefinition;

    public Item item
    {
        get
        {
            EnsureItemFromLegacy();
            return _item;
        }
        set
        {
            _item = value;
            if (_item?.definition != null)
            {
                _legacyDefinition = _item.definition;
            }
        }
    }

    public void OnBeforeSerialize()
    {
        if (_item?.definition != null)
        {
            _legacyDefinition = _item.definition;
        }
    }

    public void OnAfterDeserialize()
    {
        EnsureItemFromLegacy();
    }

    private void EnsureItemFromLegacy()
    {
        if (_item == null && _legacyDefinition != null)
        {
            _item = Item.FromDefinition(_legacyDefinition);
            return;
        }

        if (_item != null && _item.definition == null && _legacyDefinition != null)
        {
            _item.definition = _legacyDefinition;
        }
    }
}
