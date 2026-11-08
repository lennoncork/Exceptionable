// Tests a constructor that chains to one with an implicit ArgumentNullException contract.
// Expected behavior: report EXC003 at this(). Do not mark the chaining constructor itself.
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

    public Resource()
        : this("ok")
    {
    }
}
class Caller
{
    void M()
    {
        _ = new Resource();
    }
}
