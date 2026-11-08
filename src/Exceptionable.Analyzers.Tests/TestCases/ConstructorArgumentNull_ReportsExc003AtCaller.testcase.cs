// Tests a constructor that throws ArgumentNullException without [Throws].
// Expected behavior: treat the constructor as [Throws(ArgumentNullException)] and report EXC003 at the caller.
using System;
class Resource
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
