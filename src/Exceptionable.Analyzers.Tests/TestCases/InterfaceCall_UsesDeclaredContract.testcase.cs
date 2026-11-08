// Tests an annotated interface method called through an interface-typed value.
// Expected behavior: report EXC003 at the call site.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Caller
{
    void Call(IApi api)
    {
        api.Execute();
    }
}
