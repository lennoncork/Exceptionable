// Tests a constructor with two escaping throws and no explicit attribute.
// Expected behavior: infer both exception types and report EXC003 for each. Do not report EXC002.
using System;
class Resource
{
    public Resource(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        throw new InvalidOperationException();
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
