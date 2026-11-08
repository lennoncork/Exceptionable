// Tests a nullable-aware [DoesNotThrow] constructor that throws ArgumentNullException.
// Expected behavior: report EXC001 and EXC007. Do not report EXC003.
#nullable enable
using System;
using Exceptionable;
[DoesNotThrow]
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
