// Tests a generic Throws attribute for the exception that is thrown.
// Expected behavior: report neither EXC002 nor EXC004.
using System.IO;
using Exceptionable;
class C
{
    [Throws<IOException>]
    void M()
    {
        throw new IOException();
    }
}
