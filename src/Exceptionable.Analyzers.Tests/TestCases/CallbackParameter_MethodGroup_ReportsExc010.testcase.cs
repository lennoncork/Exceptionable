// Tests a method group passed to a [DoesNotThrow] callback parameter.
// Expected behavior: report EXC010 for the method's known exception.
using System;
using Exceptionable;
class C
{
    void Register([DoesNotThrow] Action callback) { }
    void Throws() { throw new System.IO.IOException(); }
    void M() { Register(Throws); }
}
