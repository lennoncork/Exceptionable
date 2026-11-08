// Tests a nullable-aware constructor that both throws ArgumentNullException and declares it.
// Expected behavior: report EXC007 for the throw. The explicit contract still reports EXC003.
#nullable enable
using System;
using Exceptionable;
class Resource
{
    [Throws("System.ArgumentNullException")]
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
