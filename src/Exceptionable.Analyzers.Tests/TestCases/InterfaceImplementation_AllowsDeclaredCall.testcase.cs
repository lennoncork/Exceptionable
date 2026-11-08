// Tests a call inside an implementation whose interface contract permits the exception.
// Expected behavior: do not report EXC002 or EXC003. The interface Throws contract
// allows IOException to escape, including from an annotated call.
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
        Helpers.Helper();
    }
}
class Helpers
{
    [Throws("System.IO.IOException")]
    public static void Helper() => throw new System.IO.IOException();
}
