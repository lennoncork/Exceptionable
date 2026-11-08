// Tests a nullable-aware constructor that guards its nullable parameter.
// Expected behavior: keep the implicit [Throws] contract and report EXC003.
#nullable enable
using System;
class Resource
{
    public Resource(string name, string? nickname)
    {
        _ = name;
        if (nickname is null)
        {
            throw new ArgumentNullException(nameof(nickname));
        }
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a", "b");
    }
}
