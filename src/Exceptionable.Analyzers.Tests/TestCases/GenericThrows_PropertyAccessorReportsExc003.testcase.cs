// Tests a generic Throws attribute on a property.
// Expected behavior: report EXC003 for the getter.
using System.IO;
using Exceptionable;
class C
{
    [Throws<IOException>]
    int Value
    {
        get
        {
            throw new IOException();
        }
    }

    void Caller()
    {
        _ = this.Value;
    }
}
