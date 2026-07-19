// 마나 저장소. 버퍼만 두고 클릭 이관은 없다 (벨트 입출·생산 종료 수집).
public class ManaStorageMachine : StorageMachine
{
    protected override bool AllowManualWithdraw => false;
}