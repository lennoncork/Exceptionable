// Tests a caller that catches the constructor's implicit ArgumentNullException.
// Expected behavior: do not report EXC003.
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
    void M()
    {
        try
        {
            _ = new Resource("a");
        }
        catch (ArgumentNullException)
        {
        }
    }
}
