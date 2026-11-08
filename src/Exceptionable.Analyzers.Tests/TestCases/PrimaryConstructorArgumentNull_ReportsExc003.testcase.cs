// Tests a primary constructor initializer that throws ArgumentNullException without nullable annotations.
// Expected behavior: report EXC003 and do not report EXC007.
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
