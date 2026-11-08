// Tests a generic Throws attribute that does not cover the thrown exception.
// Expected behavior: report EXC002 for InvalidOperationException.
using System;
using System.IO;
using Exceptionable;
class C
{
    [Throws<IOException>]
    void M()
    {
        throw new InvalidOperationException();
    }
}
