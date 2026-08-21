using UnityEngine;

// 수동 마나 제작기. 가로 2 × 세로 1. 수작업 진행은 HandmadeMachine과 동일하다.
public class ManaHandmadeMachine : HandmadeMachine
{
    public override Vector2Int GetFootprintSize() => new Vector2Int(2, 1);

    public override bool UsesMana() => true;
}
