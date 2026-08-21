// 마나 저장소. 창고와 같은 입고·추출 경로다. 정수만 받고 클릭 이관은 없다.
public class ManaStorageMachine : StorageMachine
{
    protected override bool AllowManualWithdraw => false;

    public override bool AcceptsManaEssence() => true;

    public override bool PutintoInputPort(ItemEntry IE)
    {
        if (IsBroken || IE == null || IE.item == null || IE.count <= 0)
        {
            return false;
        }

        if (!ManaEssence.IsEssence(IE.item))
        {
            return false;
        }

        return inputPort != null && inputPort.TryAdd(IE);
    }

    // 출력기가 뒤쪽에 붙었을 때 버퍼에서 정수를 빼 벨트로 보낸다.
    public bool TryExtractItem(Item item, int count)
    {
        if (item == null || count <= 0 || inputPort == null)
        {
            return false;
        }

        return inputPort.TryTake(new ItemEntry { item = item.Clone(), count = count });
    }
}
