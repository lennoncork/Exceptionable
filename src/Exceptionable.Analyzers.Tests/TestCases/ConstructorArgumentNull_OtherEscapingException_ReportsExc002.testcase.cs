// Tests a constructor whose only implicit contract is ArgumentNullException.
// Expected behavior: report EXC002 for the other escaping exception and EXC003 for ArgumentNullException.
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
