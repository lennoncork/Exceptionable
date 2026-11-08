// Tests a [Throws] attribute left behind after the thrown exception changes.
// Expected behavior: report EXC002 for the new exception and EXC008 for the declared type that no longer escapes.
using System;
using System.IO;
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    void M()
    {
        throw new InvalidOperationException();
    }
}
