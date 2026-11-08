// Tests a [DoesNotThrow] constructor that throws ArgumentNullException.
// Expected behavior: report EXC001. Do not replace [DoesNotThrow] with an implicit [Throws] contract.
using System;
using Exceptionable;
class Resource
{
    [DoesNotThrow]
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
