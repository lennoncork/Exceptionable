// Tests a callback parameter that allows IOException.
// Expected behavior: a lambda that throws only IOException is accepted.
using System;
using System.IO;
using Exceptionable;
class C
{
    void Register([Throws("System.IO.IOException")] Action callback) { }
    void M() { Register(() => throw new IOException()); }
}
