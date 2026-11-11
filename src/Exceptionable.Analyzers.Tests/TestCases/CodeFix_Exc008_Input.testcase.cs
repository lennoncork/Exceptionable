// Declares two exceptions where the body only throws one of them.
// Expected behavior: the EXC008 fix removes the stale IOException declaration and keeps the other.
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
