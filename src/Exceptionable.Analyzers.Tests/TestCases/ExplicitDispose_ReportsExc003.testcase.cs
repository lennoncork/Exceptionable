// Tests an explicit IDisposable implementation with its own contract.
// Expected behavior: report EXC003 for the implementation Dispose.
using Exceptionable;
class Resource : System.IDisposable
{
    [Throws("System.IO.IOException")]
    void System.IDisposable.Dispose() => throw new System.IO.IOException();
}
class C
{
    void M()
    {
        using var resource = new Resource();
        _ = resource;
    }
}
