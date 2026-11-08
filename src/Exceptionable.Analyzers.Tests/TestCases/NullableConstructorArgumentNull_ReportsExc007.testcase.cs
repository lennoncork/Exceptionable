// Tests ArgumentNullException for a non-nullable reference parameter in a nullable-aware constructor.
// Expected behavior: report EXC007 and do not treat the throw as an implicit [Throws] contract.
#nullable enable
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
