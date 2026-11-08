// Tests an explicit [Throws] contract on a constructor that also throws ArgumentNullException.
// Expected behavior: the explicit attribute replaces inference. Report EXC002 for ArgumentNullException and EXC003 for IOException.
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
