// Tests a using declaration. Dispose runs when the variable goes out of scope.
// Expected behavior: report EXC003 for Dispose.
using Exceptionable;
class Resource : System.IDisposable
{
    [Throws("System.IO.IOException")]
    public void Dispose() => throw new System.IO.IOException();
}
class C
{
    void M()
    {
        using var resource = new Resource();
        _ = resource;
    }
}
