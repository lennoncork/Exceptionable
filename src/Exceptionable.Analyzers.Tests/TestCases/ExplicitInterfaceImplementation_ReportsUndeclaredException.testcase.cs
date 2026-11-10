// Tests contract validation of an explicit interface implementation.
// Expected behavior: report EXC002 once.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [Throws("System.IO.IOException")]
    void IApi.Execute()
    {
        throw new System.InvalidOperationException();
    }
}
