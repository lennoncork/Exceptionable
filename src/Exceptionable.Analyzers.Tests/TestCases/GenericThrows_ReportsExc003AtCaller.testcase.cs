// Tests a call to a method annotated with a generic Throws attribute.
// Expected behavior: report EXC003 for IOException.
using System.IO;
using Exceptionable;
class C
{
    [Throws<IOException>]
    void M()
    {
    }

    void Caller()
    {
        M();
    }
}
