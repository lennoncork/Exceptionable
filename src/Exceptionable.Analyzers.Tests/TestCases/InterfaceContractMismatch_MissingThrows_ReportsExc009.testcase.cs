// Tests an implementation that omits the interface [Throws] contract.
// Expected behavior: report EXC009 on Execute. Inferred throws do not count as
// the same attribution.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    public void Execute()
    {
        throw new System.IO.IOException();
    }
}
