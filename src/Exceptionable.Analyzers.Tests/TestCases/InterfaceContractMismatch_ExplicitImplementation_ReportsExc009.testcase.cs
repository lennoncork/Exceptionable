// Tests an explicit interface implementation that omits the interface contract.
// Expected behavior: report EXC009 on Execute.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    void IApi.Execute()
    {
    }
}
