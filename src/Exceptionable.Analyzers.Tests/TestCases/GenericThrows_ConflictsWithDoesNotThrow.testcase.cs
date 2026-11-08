// Tests DoesNotThrow combined with a generic Throws attribute.
// Expected behavior: report EXC005.
using System.IO;
using Exceptionable;
class C
{
    [Throws<IOException>]
    [DoesNotThrow]
    void M()
    {
    }
}
