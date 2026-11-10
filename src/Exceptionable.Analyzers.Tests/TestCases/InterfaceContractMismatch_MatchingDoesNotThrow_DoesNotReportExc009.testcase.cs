// Tests an implementation that repeats the interface [DoesNotThrow] contract.
// Expected behavior: do not report EXC009.
using Exceptionable;
interface IApi
{
    [DoesNotThrow]
    void Execute();
}
class Implementation : IApi
{
    [DoesNotThrow]
    public void Execute()
    {
    }
}
