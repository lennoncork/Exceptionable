// Tests two declared exceptions after the body stops throwing one of them.
// Expected behavior: report EXC008 for IOException and do not report EXC002.
using System;
using System.IO;
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    [Throws("System.InvalidOperationException")]
    void M()
    {
        throw new InvalidOperationException();
    }
}
