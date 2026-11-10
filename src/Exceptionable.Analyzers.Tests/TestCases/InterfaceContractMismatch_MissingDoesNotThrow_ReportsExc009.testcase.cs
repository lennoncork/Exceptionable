// Tests an implementation that omits the interface [DoesNotThrow] contract.
// Expected behavior: report EXC009 on Execute.
using Exceptionable;
interface IApi
{
    [DoesNotThrow]
    void Execute();
}
class Implementation : IApi
{
    public void Execute()
    {
    }
}
