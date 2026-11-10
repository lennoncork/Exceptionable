// Tests an implementation that replaces interface [Throws] with [DoesNotThrow].
// Expected behavior: report EXC009. The published contracts are not the same.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [DoesNotThrow]
    public void Execute()
    {
    }
}
