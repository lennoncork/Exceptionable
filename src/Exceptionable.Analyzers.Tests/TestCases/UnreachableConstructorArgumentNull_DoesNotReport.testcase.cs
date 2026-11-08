// Tests an unreachable ArgumentNullException in a constructor.
// Expected behavior: do not add an implicit [Throws] contract.
using System;
class Resource
{
    public Resource(string name)
    {
        return;
        throw new ArgumentNullException(nameof(name));
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
