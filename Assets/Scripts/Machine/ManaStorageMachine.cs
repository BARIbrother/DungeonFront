// 마나 저장소. 버퍼만 두고 클릭 이관은 없다. 인벤 이관은 창고만 한다.
public class ManaStorageMachine : StorageMachine
{
    protected override bool AllowManualWithdraw => false;
}