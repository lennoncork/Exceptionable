// Tests a constructor that throws InvalidOperationException and has no explicit attribute.
// Expected behavior: infer that exception and report EXC003 at the caller.
using System;
class Resource
{
    public Resource()
    {
        throw new InvalidOperationException();
    }
}
class Caller
{
    void M()
    {
        _ = new Resource();
    }
}
