// Tests a constructor that throws an exception other than ArgumentNullException.
// Expected behavior: do not infer a [Throws] contract.
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
