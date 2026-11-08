// Verifies Generator Explicit Throws Overrides Recorded Contract.
using Exceptionable;
namespace Library;
public class Resource
{
    [Throws("System.IO.IOException")]
    public void Execute(bool fail)
    {
        if (fail)
        {
            throw new System.IO.IOException();
        }

        throw new System.InvalidOperationException();
    }
}


