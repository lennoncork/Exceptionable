// Tests an expression-bodied constructor that throws ArgumentNullException.
// Expected behavior: report EXC003 at the caller.
using System;
class Resource
{
    public Resource(string name) => throw new ArgumentNullException(nameof(name));
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
