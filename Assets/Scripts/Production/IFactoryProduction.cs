// 생산 틱이 있는 기계가 구현한다. TickManager가 페이즈별로 호출한다.
public interface IFactoryProduction
{
    void TickCompleteProduction();
    void TickStartProduction();
}
