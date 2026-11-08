// Tests a caller that declares the exception produced by a known call.
// Expected behavior: the call satisfies the declared type, so do not report EXC008.
using System.IO;
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    void MayThrow()
    {
        throw new IOException();
    }

    [Throws("System.IO.IOException")]
    void Caller()
    {
        this.MayThrow();
    }
}
