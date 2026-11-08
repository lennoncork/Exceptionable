// Tests a nullable-aware constructor that guards only its non-nullable parameter.
// Expected behavior: report EXC007 and do not report EXC003.
#nullable enable
using System;
class Resource
{
    public Resource(string name, string? nickname)
    {
        _ = nickname;
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
        _ = new Resource("a", null);
    }
}
