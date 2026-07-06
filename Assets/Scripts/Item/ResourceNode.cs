using System;
using System.Collections.Generic;
using UnityEngine;

// 맵에 배치된 자원 노드(32×32px = 그리드 1칸)의 시각·메타데이터. 채굴 로직은 없다.
public class ResourceNode : MonoBehaviour
{
    // 채취 출력 아이템 ID (예: iron_ore)
    [SerializeField] private string itemId = "iron_ore";

    // UI·기획용 표시 이름
    [SerializeField] private string displayName = "철광석";

    // GridManager가 배치한 그리드 좌표 (좌하단 anchor, 1칸 점유)
    private Vector2Int gridAnchor;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Vector2Int GridAnchor => gridAnchor;

    // GridManager가 배치 직후 그리드 좌표를 주입한다.
    public void Initialize(Vector2Int anchor)
    {
        gridAnchor = anchor;
    }
}
