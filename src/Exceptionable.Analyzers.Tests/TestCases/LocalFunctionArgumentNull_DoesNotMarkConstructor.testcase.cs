// Tests ArgumentNullException thrown from a local function inside a constructor.
// Expected behavior: do not treat the local function as part of the constructor contract.
using System;
class Resource
{
    public Resource(string name)
    {
        void Local()
        {
            throw new ArgumentNullException(nameof(name));
        }

        Local();
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
