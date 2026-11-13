// Tests a callback that throws a type outside the parameter's [Throws] set.
// Expected behavior: report EXC010.
using System;
using System.IO;
using Exceptionable;
class C
{
    void Register([Throws("System.IO.IOException")] Action callback) { }
    void M() { Register(() => throw new InvalidOperationException()); }
}
