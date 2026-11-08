// Tests a caller whose contract permits the constructor's implicit ArgumentNullException.
// Expected behavior: do not report EXC003.
using System;
using Exceptionable;
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
    [Throws("System.ArgumentNullException")]
    void M()
    {
        _ = new Resource("a");
    }
}
