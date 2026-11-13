// Tests a catch around an invocation of a [DoesNotThrow] callback parameter.
// Expected behavior: report EXC006. The parameter contract proves the call does not throw.
using System;
using Exceptionable;
class C
{
    void Run([DoesNotThrow] Action callback)
    {
        try { callback(); }
        catch (System.Exception) { }
    }
}
