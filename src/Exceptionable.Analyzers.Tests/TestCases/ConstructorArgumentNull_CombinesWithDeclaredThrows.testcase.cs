// Tests a constructor that declares another exception and also throws ArgumentNullException.
// Expected behavior: report EXC003 for both exceptions and do not report EXC002.
using System;
using System.IO;
using Exceptionable;
class Resource
{
    [Throws("System.IO.IOException")]
    public Resource(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        throw new IOException();
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
