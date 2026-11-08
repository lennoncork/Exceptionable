// Tests that a caller of a throwing constructor does not pass that contract to its own caller.
// Expected behavior: report EXC003 only at the constructor invocation.
using System;
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
    public void Parent()
    {
        _ = new Resource("a");
    }

    public void Grandparent()
    {
        this.Parent();
    }
}
