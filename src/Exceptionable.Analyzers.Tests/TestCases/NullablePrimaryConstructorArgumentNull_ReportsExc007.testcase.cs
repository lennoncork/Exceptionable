// Tests a nullable-aware primary constructor whose initializer throws ArgumentNullException.
// Expected behavior: report EXC007 and do not report EXC003.
#nullable enable
using System;
class Resource(string name)
{
    private readonly string value = name ?? throw new ArgumentNullException(nameof(name));
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
