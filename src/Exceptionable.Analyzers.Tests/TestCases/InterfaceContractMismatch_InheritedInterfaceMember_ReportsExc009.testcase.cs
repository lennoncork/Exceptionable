// Tests an implementation of an inherited interface member that has a contract.
// Expected behavior: report EXC009 against IBaseApi.Execute.
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
    public void Execute()
    {
    }
}
