// Tests a thrown type that derives from the generic Throws argument.
// Expected behavior: report no diagnostics.
using System.IO;
using Exceptionable;
class Derived : IOException
{
}
class C
{
    [Throws<IOException>]
    void M()
    {
        throw new Derived();
    }
}
