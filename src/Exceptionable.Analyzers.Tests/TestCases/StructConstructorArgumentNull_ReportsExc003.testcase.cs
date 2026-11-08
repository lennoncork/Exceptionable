// Tests a struct constructor that throws ArgumentNullException.
// Expected behavior: report EXC003 at the caller.
using System;
struct Resource
{
    public Resource(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
