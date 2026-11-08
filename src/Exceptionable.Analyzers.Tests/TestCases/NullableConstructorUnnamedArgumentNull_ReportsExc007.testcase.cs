// Tests an ArgumentNullException that does not name a parameter when every reference parameter is non-nullable.
// Expected behavior: report EXC007.
#nullable enable
using System;
class Resource
{
    public Resource(string name)
    {
        _ = name;
        throw new ArgumentNullException();
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
