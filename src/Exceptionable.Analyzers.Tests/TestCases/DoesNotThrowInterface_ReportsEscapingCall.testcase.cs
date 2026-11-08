// Tests a call that escapes a DoesNotThrow interface through a wider method contract.
// Expected behavior: report EXC003. The interface contract still forbids the exception.
using Exceptionable;
interface IApi
{
    [DoesNotThrow]
    void Execute();
}
class Implementation : IApi
{
    [Throws("System.IO.IOException")]
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
