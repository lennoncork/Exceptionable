// Tests Dispose inside a catch that handles its declared exception.
// Expected behavior: do not report EXC003.
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
        try
        {
            using var resource = new Resource();
            _ = resource;
        }
        catch (System.IO.IOException)
        {
        }
    }
}
