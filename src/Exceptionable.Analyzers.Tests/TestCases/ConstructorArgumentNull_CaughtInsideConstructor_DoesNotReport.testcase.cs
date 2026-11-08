// Tests an ArgumentNullException that the constructor catches.
// Expected behavior: do not add an implicit [Throws] contract.
using System;
class Resource
{
    public Resource(string name)
    {
        try
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }
        catch (ArgumentNullException)
        {
        }
    }
}
class Caller
{
    void M()
    {
        _ = new Resource("a");
    }
}
