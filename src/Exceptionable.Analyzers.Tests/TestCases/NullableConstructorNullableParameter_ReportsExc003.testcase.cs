// Tests ArgumentNullException for a nullable reference parameter.
// Expected behavior: keep the implicit [Throws] contract and report EXC003. Do not report EXC007.
#nullable enable
using System;
class Resource
{
    public Resource(string? name)
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
