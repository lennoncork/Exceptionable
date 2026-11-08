// Tests that an interface contract narrows a wider method Throws list at a call.
// Expected behavior: report EXC003 for System.Exception, which the interface does not permit.
using Exceptionable;
interface IApi
{
    [Throws("System.IO.IOException")]
    void Execute();
}
class Implementation : IApi
{
    [Throws("System.Exception")]
    public void Execute()
    {
        Helpers.Helper();
    }
}
class Helpers
{
    [Throws("System.Exception")]
    public static void Helper() => throw new System.Exception();
}
