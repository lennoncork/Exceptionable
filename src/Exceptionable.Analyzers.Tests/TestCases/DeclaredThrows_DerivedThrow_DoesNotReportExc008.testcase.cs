// Tests a derived throw against a declared base exception.
// Expected behavior: the derived throw satisfies the declared type, so do not report EXC008 or EXC002.
using System.IO;
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    void M()
    {
        throw new FileNotFoundException();
    }
}
