// Tests that EXC009 is not reported when analysis is disabled.
// Expected behavior: no diagnostics even though the implementation omits the
// interface contract.
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
    }
}
