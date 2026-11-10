// Tests an inherited interface member's contract at a call through the derived interface.
// Expected behavior: System.Exception in the caller contract covers IOException.
using Exceptionable;
interface IBaseApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
interface IApi : IBaseApi
{
}
class Implementation : IApi
{
    [Throws("System.IO.IOException")]
    public void Execute()
    {
    }
}
class Caller
{
    [Throws("System.Exception")]
    void Call(IApi api)
    {
        api.Execute();
    }
}
