// Tests a string Throws attribute and a generic Throws attribute on one method.
// Expected behavior: both declared exceptions may escape, with no EXC002 or EXC004.
using System;
using System.IO;
using Exceptionable;
class C
{
    [Throws("System.IO.IOException")]
    [Throws<InvalidOperationException>]
    void M(bool flag)
    {
        if (flag)
        {
            throw new IOException();
        }

        throw new InvalidOperationException();
    }
}
