using System;
using UnityEngine;

// 맵 초기 레이아웃에 들어갈 자원 노드 한 칸.
[Serializable]
public struct MapNodeLayoutEntry
{
    public string zoneId;
    public Vector2Int gridCoord;
    public string itemId;
}
